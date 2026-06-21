using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static EdgeSlide.NativeMethods;

namespace EdgeSlide;

/// <summary>
/// Holds the cached preparsed data and value-caps for one touchpad HID device
/// and knows how to turn a single HID report into <see cref="TouchContact"/>s.
/// </summary>
internal sealed class ParsedDevice : IDisposable
{
    private readonly IntPtr _preparsed;       // allocated buffer, must be freed
    private readonly ushort _xUsage = HID_USAGE_GENERIC_X;
    private readonly ushort _yUsage = HID_USAGE_GENERIC_Y;

    // Link collections that represent individual finger contacts.
    private readonly List<ushort> _contactCollections;

    public TouchpadGeometry Geometry { get; }

    private ParsedDevice(IntPtr preparsed, List<ushort> contactCollections, TouchpadGeometry geometry)
    {
        _preparsed = preparsed;
        _contactCollections = contactCollections;
        Geometry = geometry;
    }

    public static ParsedDevice? Create(IntPtr hDevice)
    {
        // 1. Fetch preparsed data size, then the data itself.
        uint size = 0;
        if (GetRawInputDeviceInfo(hDevice, RIDI_PREPARSEDDATA, IntPtr.Zero, ref size) != 0 || size == 0)
            return null;

        IntPtr preparsed = Marshal.AllocHGlobal((int)size);
        bool keep = false;
        try
        {
            if (GetRawInputDeviceInfo(hDevice, RIDI_PREPARSEDDATA, preparsed, ref size) == unchecked((uint)-1))
                return null;

            if (HidP_GetCaps(preparsed, out HIDP_CAPS caps) != HIDP_STATUS_SUCCESS)
                return null;

            ushort valueCapsLength = caps.NumberInputValueCaps;
            if (valueCapsLength == 0)
                return null;

            var valueCaps = new HIDP_VALUE_CAPS[valueCapsLength];
            if (HidP_GetValueCaps(HidP_Input, valueCaps, ref valueCapsLength, preparsed) != HIDP_STATUS_SUCCESS)
                return null;

            // 2. Identify finger-contact link collections (those reporting X on the generic page),
            //    and capture logical/physical ranges for X and Y.
            var contactCollections = new List<ushort>();
            int xMin = 0, xMax = 0, yMin = 0, yMax = 0;
            double physWidthMm = 0;
            bool haveX = false, haveY = false;

            for (int i = 0; i < valueCapsLength; i++)
            {
                HIDP_VALUE_CAPS vc = valueCaps[i];
                ushort usage = vc.UsageMin; // for non-range caps, Usage occupies this slot

                if (vc.UsagePage == HID_USAGE_PAGE_GENERIC && usage == HID_USAGE_GENERIC_X)
                {
                    if (!contactCollections.Contains(vc.LinkCollection))
                        contactCollections.Add(vc.LinkCollection);

                    if (!haveX)
                    {
                        xMin = vc.LogicalMin;
                        xMax = vc.LogicalMax;
                        haveX = true;
                        physWidthMm = TryPhysicalWidthMm(vc);
                    }
                }
                else if (vc.UsagePage == HID_USAGE_PAGE_GENERIC && usage == HID_USAGE_GENERIC_Y && !haveY)
                {
                    yMin = vc.LogicalMin;
                    yMax = vc.LogicalMax;
                    haveY = true;
                }
            }

            if (!haveX || !haveY || contactCollections.Count == 0)
            {
                Logger.Warn("Touchpad HID descriptor missing X/Y value caps; cannot parse this device.");
                return null;
            }

            var geometry = new TouchpadGeometry
            {
                XLogicalMin = xMin,
                XLogicalMax = xMax,
                YLogicalMin = yMin,
                YLogicalMax = yMax,
                PhysicalWidthMm = physWidthMm
            };

            Logger.Info($"Touchpad parsed: X[{xMin}..{xMax}] Y[{yMin}..{yMax}] " +
                        $"contacts={contactCollections.Count} physWidthMm={physWidthMm:0.##}");

            keep = true;
            return new ParsedDevice(preparsed, contactCollections, geometry);
        }
        finally
        {
            if (!keep)
                Marshal.FreeHGlobal(preparsed);
        }
    }

    /// <summary>
    /// Physical width of the X axis in millimetres, decoded from the HID Unit and
    /// Unit-Exponent fields per the USB HID spec. Returns 0 (caller falls back to a
    /// fraction of the logical range) when the device doesn't report usable units.
    ///
    /// HID encodes the physical extent as: value * 10^UnitExponent, in a base unit
    /// determined by the Unit "system" nibble — centimetres for SI Linear, inches for
    /// English Linear. The Unit's length nibble must be a non-zero exponent for the
    /// value to represent a length.
    /// </summary>
    private static double TryPhysicalWidthMm(HIDP_VALUE_CAPS vc)
    {
        if (vc.PhysicalMax <= vc.PhysicalMin)
            return 0;
        if (vc.Units == 0)
            return 0; // device declares no units

        // Unit nibbles: [0]=measurement system, [1]=length exponent.
        int system = (int)(vc.Units & 0xF);
        int lengthNibble = (int)((vc.Units >> 4) & 0xF);
        int lengthExp = lengthNibble < 8 ? lengthNibble : lengthNibble - 16;
        if (lengthExp == 0)
            return 0; // not a length dimension

        // Base unit per measurement system.
        double mmPerBaseUnit = system switch
        {
            1 => 10.0,   // SI Linear  -> centimetres
            3 => 25.4,   // English Linear -> inches
            _ => 0.0     // rotation / unknown
        };
        if (mmPerBaseUnit == 0.0)
            return 0;

        // Unit exponent is a signed nibble (8..15 => -8..-1).
        int rawExp = (int)(vc.UnitsExp & 0xF);
        int unitExp = rawExp < 8 ? rawExp : rawExp - 16;

        double spanBaseUnits = (vc.PhysicalMax - vc.PhysicalMin) * Math.Pow(10, unitExp);
        double widthMm = spanBaseUnits * mmPerBaseUnit;

        // Sanity-check against any plausible touchpad width before trusting it.
        if (widthMm is >= 20 and <= 400)
            return widthMm;

        return 0;
    }

    // ContactIDs that were reported finger-down in the previous frame, so we can
    // synthesize clean "lift" events when a finger disappears.
    private readonly HashSet<int> _activeIds = new();

    /// <summary>
    /// Parse one HID report and emit finger contacts.
    ///
    /// A Precision Touchpad reports a fixed set of contact slots every frame
    /// (this device has 5). Unused slots come through with TipSwitch = false and a
    /// stale/zero ContactID — often colliding with a real finger's ID. We therefore
    /// only treat TipSwitch = true slots as real fingers, and emit a lift event for a
    /// ContactID only once it stops being reported down. This prevents empty slots
    /// from tearing down an in-progress gesture.
    /// </summary>
    public void ParseReport(IntPtr report, uint reportLength, Action<TouchContact> emit)
    {
        var presentIds = new HashSet<int>();
        var presentContacts = new List<TouchContact>(_contactCollections.Count);

        foreach (ushort collection in _contactCollections)
        {
            // Only a slot whose TipSwitch is set represents a finger that is down.
            if (!IsTipDown(collection, report, reportLength))
                continue;

            if (HidP_GetUsageValue(HidP_Input, HID_USAGE_PAGE_GENERIC, collection, _xUsage,
                    out uint xVal, _preparsed, report, reportLength) != HIDP_STATUS_SUCCESS)
                continue;

            if (HidP_GetUsageValue(HidP_Input, HID_USAGE_PAGE_GENERIC, collection, _yUsage,
                    out uint yVal, _preparsed, report, reportLength) != HIDP_STATUS_SUCCESS)
                continue;

            int contactId = 0;
            if (HidP_GetUsageValue(HidP_Input, HID_USAGE_PAGE_DIGITIZER, collection,
                    HID_USAGE_DIGITIZER_CONTACT_ID, out uint cid, _preparsed, report, reportLength)
                == HIDP_STATUS_SUCCESS)
            {
                contactId = (int)cid;
            }

            if (presentIds.Add(contactId))
                presentContacts.Add(new TouchContact(contactId, (int)xVal, (int)yVal, true));
        }

        // Emit the fingers that are currently down.
        foreach (TouchContact c in presentContacts)
            emit(c);

        // Any ID that was down last frame but is gone now has lifted.
        foreach (int id in _activeIds)
        {
            if (!presentIds.Contains(id))
                emit(new TouchContact(id, 0, 0, false));
        }

        // Remember this frame's active set.
        _activeIds.Clear();
        foreach (int id in presentIds)
            _activeIds.Add(id);
    }

    private bool IsTipDown(ushort collection, IntPtr report, uint reportLength)
    {
        int max = HidP_MaxUsageListLength(HidP_Input, HID_USAGE_PAGE_DIGITIZER, _preparsed);
        if (max <= 0)
            return false;

        var usages = new ushort[max];
        uint length = (uint)max;
        if (HidP_GetUsages(HidP_Input, HID_USAGE_PAGE_DIGITIZER, collection, usages, ref length,
                _preparsed, report, reportLength) != HIDP_STATUS_SUCCESS)
            return false;

        for (int i = 0; i < length; i++)
        {
            if (usages[i] == HID_USAGE_DIGITIZER_TIP_SWITCH)
                return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_preparsed != IntPtr.Zero)
            Marshal.FreeHGlobal(_preparsed);
    }
}
