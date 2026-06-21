using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EdgeSlide;

public enum StripSide { Left, Right }

/// <summary>An active gesture update delivered to the UI/controllers.</summary>
public readonly struct GestureUpdate
{
    public readonly StripSide Side;
    public readonly StripAction Action;
    public readonly double Value; // 0.0 .. 1.0

    public GestureUpdate(StripSide side, StripAction action, double value)
    {
        Side = side;
        Action = action;
        Value = value;
    }
}

/// <summary>
/// Turns a stream of <see cref="TouchContact"/>s into validated slider gestures.
/// Implements the entry-zone rule, vertical-direction check, debounce hold, and
/// typing suppression described in the spec.
/// </summary>
public sealed class StripGestureDetector
{
    private const int DirectionDecisionMs = 100; // window to judge primary direction
    private const int TypingSuppressMs = 800;    // suppress gestures this long after a keypress

    private readonly Func<Settings> _getSettings;
    private readonly Func<TouchpadGeometry?> _getGeometry;
    private readonly KeyboardActivityMonitor _keyboard;

    private readonly Dictionary<int, ContactSession> _sessions = new();

    // ContactIDs currently touching the pad, so we can enforce single-finger-only.
    private readonly HashSet<int> _downIds = new();
    // Once 2+ fingers are seen, stay disabled until every finger lifts.
    private bool _multiFingerLockout;

    public event Action<GestureUpdate>? GestureUpdated;
    public event Action? GestureEnded;

    public StripGestureDetector(
        Func<Settings> getSettings,
        Func<TouchpadGeometry?> getGeometry,
        KeyboardActivityMonitor keyboard)
    {
        _getSettings = getSettings;
        _getGeometry = getGeometry;
        _keyboard = keyboard;
    }

    public void OnContact(TouchContact c)
    {
        Settings settings = _getSettings();
        TouchpadGeometry? geo = _getGeometry();
        if (geo == null || !settings.Enabled)
            return;

        // --- Single-finger enforcement -------------------------------------
        // Track how many fingers are on the pad right now.
        if (c.TipSwitch) _downIds.Add(c.ContactId);
        else _downIds.Remove(c.ContactId);
        int fingerCount = _downIds.Count;

        // Two or more fingers (e.g. a two-finger scroll, or a second finger added
        // mid-slide): cancel any gesture and stay disabled until the pad is clear.
        if (fingerCount >= 2)
        {
            if (!_multiFingerLockout)
            {
                _multiFingerLockout = true;
                EndAllSessions(); // drop the in-progress gesture, hide the HUD
            }
            return;
        }

        if (_multiFingerLockout)
        {
            // Don't resume until EVERY finger has lifted, so going 2 -> 1 fingers
            // (lifting one finger of a scroll) doesn't suddenly start sliding.
            if (fingerCount == 0)
                _multiFingerLockout = false;
            return;
        }
        // -------------------------------------------------------------------

        // Finger lifted: clear this contact's session unconditionally.
        if (!c.TipSwitch)
        {
            if (_sessions.Remove(c.ContactId, out ContactSession? endedSession) && endedSession.Activated)
                GestureEnded?.Invoke();
            return;
        }

        long now = Stopwatch.GetTimestamp();

        if (!_sessions.TryGetValue(c.ContactId, out ContactSession? session))
        {
            session = CreateSession(c, geo, settings, now);
            _sessions[c.ContactId] = session;
            return; // first sample only establishes the origin
        }

        if (!session.Eligible)
            return; // entry-zone rule failed — ignore for the life of this contact

        UpdateSession(session, c, geo, settings, now);
    }

    /// <summary>End any in-progress gesture sessions (keeps finger-count tracking).</summary>
    private void EndAllSessions()
    {
        bool hadActive = false;
        foreach (ContactSession s in _sessions.Values)
            hadActive |= s.Activated;
        _sessions.Clear();
        if (hadActive)
            GestureEnded?.Invoke();
    }

    /// <summary>Drop everything (e.g. when disabled or settings changed mid-gesture).</summary>
    public void Reset()
    {
        EndAllSessions();
        _downIds.Clear();
        _multiFingerLockout = false;
    }

    private ContactSession CreateSession(TouchContact c, TouchpadGeometry geo, Settings settings, long now)
    {
        double stripWidthLogical = geo.LogicalUnitsForMm(settings.StripWidthMm, settings.FallbackStripFractionOfRange);
        double activationZone = 2.0 * stripWidthLogical;

        double relX = c.X - geo.XLogicalMin; // 0 at left edge
        bool leftCandidate = relX <= activationZone;
        bool rightCandidate = relX >= geo.XRange - activationZone;

        StripSide side = leftCandidate ? StripSide.Left : StripSide.Right;
        StripAction action = side == StripSide.Left ? settings.LeftStripAction : settings.RightStripAction;

        bool eligible = (leftCandidate || rightCandidate) && action != StripAction.Disabled;

        return new ContactSession
        {
            Eligible = eligible,
            Side = side,
            Action = action,
            StartX = c.X,
            StartY = c.Y,
            StartTicks = now,
            InnerSinceTicks = 0,
            DirectionDecided = false,
            Activated = false,
            StripWidthLogical = stripWidthLogical
        };
    }

    private void UpdateSession(ContactSession s, TouchContact c, TouchpadGeometry geo, Settings settings, long now)
    {
        double elapsedMs = ElapsedMs(s.StartTicks, now);

        // Direction gate: once enough time has passed AND the finger has actually
        // moved a meaningful amount, require the motion to be primarily vertical.
        // We wait for real movement (not just placement jitter) before deciding, and
        // the horizontal threshold is scaled to the touchpad so tiny wobble near the
        // edge can't falsely cancel a slide.
        double horizCancelThreshold = Math.Max(40.0, geo.XRange * 0.02); // ~2% of width
        if (!s.DirectionDecided && elapsedMs >= DirectionDecisionMs)
        {
            double dx = Math.Abs(c.X - s.StartX);
            double dy = Math.Abs(c.Y - s.StartY);

            // Not enough movement yet to judge direction — keep waiting.
            if (dx + dy < horizCancelThreshold)
            {
                // don't set DirectionDecided; revisit on a later frame
            }
            else
            {
                s.DirectionDecided = true;
                if (dx > dy * 1.5 && dx > horizCancelThreshold)
                {
                    s.Eligible = false; // genuine horizontal swipe — release the surface
                    return;
                }
            }
        }

        // Inner-strip containment (1× strip width from the originating edge).
        double relX = c.X - geo.XLogicalMin;
        bool inInner = s.Side == StripSide.Left
            ? relX <= s.StripWidthLogical
            : relX >= geo.XRange - s.StripWidthLogical;

        if (!inInner)
        {
            s.InnerSinceTicks = 0; // left the strip; debounce restarts
            return;
        }

        if (s.InnerSinceTicks == 0)
            s.InnerSinceTicks = now;

        // Activation: continuous inner-strip hold for DebounceMs, and not typing.
        if (!s.Activated)
        {
            bool heldLongEnough = ElapsedMs(s.InnerSinceTicks, now) >= settings.DebounceMs;
            if (!heldLongEnough)
                return;
            if (_keyboard.TypedWithin(TypingSuppressMs))
                return; // suppress while typing
            s.Activated = true;
            Logger.Info($"Gesture activated on {s.Side} strip ({s.Action}).");
        }

        // Map Y → value. Default: top of the touchpad = 100% (maximum), bottom = 0%,
        // so brightness/volume rise as the finger slides up. Y increases downward in
        // touchpad coordinates, hence the 1.0 - ... .
        double frac = 1.0 - (double)(c.Y - geo.YLogicalMin) / geo.YRange;
        frac = Math.Clamp(frac, 0.0, 1.0);
        if (settings.InvertDirection)
            frac = 1.0 - frac; // inverted: top = 0%, bottom = 100%

        GestureUpdated?.Invoke(new GestureUpdate(s.Side, s.Action, frac));
    }

    private static double ElapsedMs(long fromTicks, long now)
        => (now - fromTicks) * 1000.0 / Stopwatch.Frequency;

    private sealed class ContactSession
    {
        public bool Eligible;
        public StripSide Side;
        public StripAction Action;
        public int StartX;
        public int StartY;
        public long StartTicks;
        public long InnerSinceTicks;
        public bool DirectionDecided;
        public bool Activated;
        public double StripWidthLogical;
    }
}