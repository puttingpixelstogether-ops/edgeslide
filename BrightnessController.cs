using System;
using System.Management;

namespace EdgeSlide;

/// <summary>
/// Controls display brightness via the WMI WmiMonitorBrightnessMethods class.
/// Caches the controllable monitor object so a slide doesn't re-query WMI every tick.
/// Gracefully disables itself if no controllable monitor is present (e.g. desktops
/// or external-only setups).
/// </summary>
public sealed class BrightnessController : IDisposable
{
    private bool _available;
    private ManagementObject? _monitor;     // cached WmiMonitorBrightnessMethods instance
    private byte _lastApplied = 255;
    private DateTime _lastSet = DateTime.MinValue;
    private readonly object _gate = new();

    public bool Available => _available;

    public BrightnessController()
    {
        Probe();
    }

    /// <summary>Find and cache the brightness-method object. Safe to call again to re-acquire.</summary>
    private void Probe()
    {
        try
        {
            _monitor?.Dispose();
            _monitor = null;

            using var searcher = new ManagementObjectSearcher(
                "root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
            foreach (ManagementBaseObject o in searcher.Get())
            {
                if (o is ManagementObject mo)
                {
                    _monitor = mo;
                    break;
                }
            }

            _available = _monitor != null;
            if (!_available)
                Logger.Warn("No WMI brightness method found; brightness control disabled.");
        }
        catch (Exception ex)
        {
            _available = false;
            _monitor = null;
            Logger.Warn($"Brightness probe failed; brightness control disabled: {ex.Message}");
        }
    }

    /// <summary>Set brightness from a 0.0–1.0 value.</summary>
    public void SetScalar(double value)
    {
        if (!_available) return;
        byte target = (byte)Math.Clamp((int)Math.Round(value * 100.0), 0, 100);

        lock (_gate)
        {
            // Avoid spamming WMI with identical/too-frequent calls (it is slow).
            if (target == _lastApplied && (DateTime.UtcNow - _lastSet).TotalMilliseconds < 250)
                return;

            try
            {
                if (_monitor == null)
                {
                    Probe();
                    if (_monitor == null) return;
                }

                // WmiSetBrightness(timeout, brightness)
                _monitor.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, target });
                _lastApplied = target;
                _lastSet = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                // Monitor handle may have gone stale (display change/sleep); drop it and
                // re-acquire on the next call.
                Logger.Warn($"WmiSetBrightness failed, will re-probe: {ex.Message}");
                _monitor?.Dispose();
                _monitor = null;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _monitor?.Dispose();
            _monitor = null;
        }
    }
}
