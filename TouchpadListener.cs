using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static EdgeSlide.NativeMethods;

namespace EdgeSlide;

/// <summary>A single parsed contact point from a touchpad HID report.</summary>
public readonly struct TouchContact
{
    public readonly int ContactId;
    public readonly int X;
    public readonly int Y;
    public readonly bool TipSwitch; // true = finger down

    public TouchContact(int contactId, int x, int y, bool tipSwitch)
    {
        ContactId = contactId;
        X = x;
        Y = y;
        TipSwitch = tipSwitch;
    }
}

/// <summary>Logical and (best-effort) physical geometry of the touchpad surface.</summary>
public sealed class TouchpadGeometry
{
    public int XLogicalMin { get; init; }
    public int XLogicalMax { get; init; }
    public int YLogicalMin { get; init; }
    public int YLogicalMax { get; init; }

    /// <summary>Physical width in mm, or 0 if it could not be determined.</summary>
    public double PhysicalWidthMm { get; init; }

    public int XRange => Math.Max(1, XLogicalMax - XLogicalMin);
    public int YRange => Math.Max(1, YLogicalMax - YLogicalMin);

    public bool HasPhysicalWidth => PhysicalWidthMm > 0.1;

    /// <summary>
    /// Logical units that correspond to <paramref name="mm"/> millimetres along X.
    /// Falls back to a fraction of the logical range when physical width is unknown.
    /// </summary>
    public double LogicalUnitsForMm(double mm, double fallbackFractionPer5mm)
    {
        if (HasPhysicalWidth)
            return XRange / PhysicalWidthMm * mm;

        // Fallback: fraction-of-range represents ~5mm; scale linearly with mm.
        return XRange * fallbackFractionPer5mm * (mm / 5.0);
    }
}

/// <summary>
/// Registers for Raw Input from the Precision Touchpad digitizer and parses
/// incoming HID reports into <see cref="TouchContact"/> values. Handles both
/// per-finger reports and batched multi-contact reports.
/// </summary>
public sealed class TouchpadListener : IDisposable
{
    private readonly MessageWindow _window;
    private readonly Dictionary<IntPtr, ParsedDevice> _devices = new();
    private bool _registered;
    private bool _disposed;

    /// <summary>Raised for each contact parsed from an incoming report.</summary>
    public event Action<TouchContact>? ContactReceived;

    /// <summary>True once at least one compatible Precision Touchpad has reported.</summary>
    public bool TouchpadFound { get; private set; }

    /// <summary>Geometry of the most recently seen touchpad (null until first report).</summary>
    public TouchpadGeometry? Geometry { get; private set; }

    /// <summary>Raised the first time a touchpad is detected (after geometry is known).</summary>
    public event Action? TouchpadDetected;

    // Diagnostics: when an (empty) file named "diagnostics.on" exists in the config
    // folder, raw contacts are logged (throttled) so we can see live coordinates.
    private readonly bool _diagnostics;
    private long _lastDiagTicks;

    public TouchpadListener()
    {
        _window = new MessageWindow(OnInputMessage);
        try
        {
            // Tolerant of "diagnostics.on", "diagnostics.on.txt", etc.
            _diagnostics =
                System.IO.Directory.Exists(Settings.ConfigDirectory) &&
                System.IO.Directory.GetFiles(Settings.ConfigDirectory, "diagnostics.on*").Length > 0;
        }
        catch { _diagnostics = false; }
        if (_diagnostics)
            Logger.Info("Diagnostics mode ON: raw touch coordinates will be logged.");
    }

    /// <summary>Registers the digitizer for input-sink raw input.</summary>
    public bool Start()
    {
        if (_registered) return true;

        var rid = new RAWINPUTDEVICE[1];
        rid[0].usUsagePage = HID_USAGE_PAGE_DIGITIZER;
        rid[0].usUsage = HID_USAGE_DIGITIZER_TOUCH_PAD;
        rid[0].dwFlags = RIDEV_INPUTSINK;
        rid[0].hwndTarget = _window.Handle;

        _registered = RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        if (!_registered)
        {
            int err = Marshal.GetLastWin32Error();
            Logger.Error($"RegisterRawInputDevices failed (error {err}).");
        }
        else
        {
            Logger.Info("Raw Input registered for Precision Touchpad digitizer.");
        }
        return _registered;
    }

    /// <summary>
    /// Enumerates raw input devices and returns true if a Precision Touchpad
    /// digitizer (usage page 0x0D, usage 0x05) is present.
    /// </summary>
    public static bool HasCompatibleDevice()
    {
        uint count = 0;
        uint structSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();
        if (GetRawInputDeviceList(IntPtr.Zero, ref count, structSize) == unchecked((uint)-1) || count == 0)
            return false;

        IntPtr list = Marshal.AllocHGlobal((int)(structSize * count));
        try
        {
            uint got = GetRawInputDeviceList(list, ref count, structSize);
            if (got == unchecked((uint)-1))
                return false;

            bool foundTouchpad = false;
            int hidCount = 0;

            for (int i = 0; i < got; i++)
            {
                IntPtr entryPtr = IntPtr.Add(list, (int)(i * structSize));
                var entry = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(entryPtr);
                if (entry.dwType != RIM_TYPEHID)
                    continue;

                uint infoSize = (uint)Marshal.SizeOf<RID_DEVICE_INFO>();
                IntPtr infoPtr = Marshal.AllocHGlobal((int)infoSize);
                try
                {
                    Marshal.WriteInt32(infoPtr, (int)infoSize); // cbSize
                    uint res = GetRawInputDeviceInfo(entry.hDevice, RIDI_DEVICEINFO, infoPtr, ref infoSize);
                    if (res == unchecked((uint)-1) || res == 0)
                        continue;
                    var info = Marshal.PtrToStructure<RID_DEVICE_INFO>(infoPtr);
                    if (info.dwType != RIM_TYPEHID)
                        continue;

                    hidCount++;
                    ushort up = info.hid.usUsagePage;
                    ushort u = info.hid.usUsage;

                    // Log every digitizer-ish HID so we can see touchpad (0x0D/0x05)
                    // vs touchscreen (0x0D/0x04) vs other devices.
                    if (up == HID_USAGE_PAGE_DIGITIZER)
                    {
                        string kind = u switch
                        {
                            0x04 => "Touch Screen",
                            0x05 => "Touch Pad",
                            0x01 => "Digitizer",
                            0x02 => "Pen",
                            _ => "other digitizer"
                        };
                        Logger.Info($"HID digitizer found: usagePage=0x{up:X2} usage=0x{u:X2} ({kind}) " +
                                    $"VID=0x{info.hid.dwVendorId:X4} PID=0x{info.hid.dwProductId:X4}");
                    }

                    if (up == HID_USAGE_PAGE_DIGITIZER && u == HID_USAGE_DIGITIZER_TOUCH_PAD)
                        foundTouchpad = true;
                }
                finally
                {
                    Marshal.FreeHGlobal(infoPtr);
                }
            }

            Logger.Info($"Device scan complete: {hidCount} HID device(s), " +
                        $"Precision Touchpad present = {foundTouchpad}.");
            return foundTouchpad;
        }
        finally
        {
            Marshal.FreeHGlobal(list);
        }
    }

    private void OnInputMessage(IntPtr hRawInput)
    {
        try
        {
            ParseRawInput(hRawInput);
        }
        catch (Exception ex)
        {
            // Manufacturer report quirks must never crash the listener.
            Logger.Warn($"Raw input parse error (ignored): {ex.Message}");
        }
    }

    private void ParseRawInput(IntPtr hRawInput)
    {
        // 1. Get the size of the raw input block.
        uint size = 0;
        uint headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
        if (GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, headerSize) != 0 || size == 0)
            return;

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            uint read = GetRawInputData(hRawInput, RID_INPUT, buffer, ref size, headerSize);
            if (read != size) return;

            // 2. Read header.
            var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
            if (header.dwType != RIM_TYPEHID) return;

            // 3. Read the RAWHID part that follows the header.
            IntPtr hidPtr = IntPtr.Add(buffer, (int)headerSize);
            var hid = Marshal.PtrToStructure<RAWHID>(hidPtr);
            if (hid.dwSizeHid == 0 || hid.dwCount == 0) return;

            // 4. Resolve (and cache) the parsed device for this hDevice.
            ParsedDevice? device = GetOrCreateDevice(header.hDevice);
            if (device == null) return;

            if (!TouchpadFound)
            {
                TouchpadFound = true;
                Geometry = device.Geometry;
                TouchpadDetected?.Invoke();
            }
            else
            {
                Geometry = device.Geometry;
            }

            // 5. Each report is dwSizeHid bytes; bRawData begins after the RAWHID struct.
            IntPtr reportBase = IntPtr.Add(hidPtr, Marshal.SizeOf<RAWHID>());
            for (int i = 0; i < hid.dwCount; i++)
            {
                IntPtr report = IntPtr.Add(reportBase, (int)(i * hid.dwSizeHid));
                device.ParseReport(report, hid.dwSizeHid, EmitContact);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void EmitContact(TouchContact contact)
    {
        if (_diagnostics)
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double sinceMs = (now - _lastDiagTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            if (sinceMs >= 300) // throttle so the log stays readable
            {
                _lastDiagTicks = now;
                TouchpadGeometry? g = Geometry;
                string range = g != null ? $" | Xrange[{g.XLogicalMin}..{g.XLogicalMax}] Yrange[{g.YLogicalMin}..{g.YLogicalMax}]" : "";
                Logger.Info($"CONTACT id={contact.ContactId} X={contact.X} Y={contact.Y} tip={contact.TipSwitch}{range}");
            }
        }
        ContactReceived?.Invoke(contact);
    }

    private ParsedDevice? GetOrCreateDevice(IntPtr hDevice)
    {
        if (_devices.TryGetValue(hDevice, out ParsedDevice? existing))
            return existing;

        ParsedDevice? device = ParsedDevice.Create(hDevice);
        if (device != null)
            _devices[hDevice] = device;
        return device;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_registered)
        {
            // Unregister raw input.
            var rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = HID_USAGE_PAGE_DIGITIZER;
            rid[0].usUsage = HID_USAGE_DIGITIZER_TOUCH_PAD;
            rid[0].dwFlags = RIDEV_REMOVE;
            rid[0].hwndTarget = IntPtr.Zero;
            RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        }

        foreach (ParsedDevice d in _devices.Values)
            d.Dispose();
        _devices.Clear();

        _window.Dispose();
    }

    // -------------------------------------------------------------------------
    // Hidden message-only window that receives WM_INPUT.
    // -------------------------------------------------------------------------
    private sealed class MessageWindow : NativeWindow, IDisposable
    {
        private readonly Action<IntPtr> _onInput;

        public MessageWindow(Action<IntPtr> onInput)
        {
            _onInput = onInput;
            // A hidden top-level window (never shown, no WS_VISIBLE). This is the
            // most reliable RIDEV_INPUTSINK target across Windows versions.
            var cp = new CreateParams
            {
                Caption = "EdgeSlideRawInput",
                Style = unchecked((int)0x80000000) // WS_POPUP, not visible
            };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
                _onInput(m.LParam);
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                DestroyHandle();
        }
    }
}
