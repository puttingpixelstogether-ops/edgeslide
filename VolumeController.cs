using System;
using NAudio.CoreAudioApi;

namespace EdgeSlide;

/// <summary>
/// Controls the default render endpoint's master volume via NAudio CoreAudio.
/// The default endpoint is re-resolved at the start of each slide (see
/// <see cref="RefreshDefaultDevice"/>), so switching output — e.g. connecting Bluetooth —
/// is picked up on our own thread. We deliberately do NOT use IMMNotificationClient:
/// its callbacks arrive on the audio system's thread, and touching COM/audio objects
/// from there can deadlock the whole app.
/// </summary>
public sealed class VolumeController : IDisposable
{
    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _device;
    private bool _available;
    private int _lastVolumePercent = -1; // last level we sent (0–100); -1 = unknown
    private readonly object _gate = new();

    public bool Available => _available;

    public VolumeController()
    {
        try
        {
            _enumerator = new MMDeviceEnumerator();
            ResolveDevice();
            _available = _device != null;
            if (!_available)
                Logger.Warn("No default audio render endpoint found; volume control disabled.");
        }
        catch (Exception ex)
        {
            _available = false;
            Logger.Warn($"Volume init failed; volume control disabled: {ex.Message}");
        }
    }

    private void ResolveDevice()
    {
        try
        {
            _device?.Dispose();
            _device = _enumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Could not resolve default audio endpoint: {ex.Message}");
            _device = null;
        }
    }

    /// <summary>
    /// Read the current master volume as a 0.0–1.0 value, or -1 if it can't be read.
    /// Used to anchor a relative slide so it starts from the present level.
    /// </summary>
    public double GetScalar()
    {
        if (!_available) return -1.0;
        lock (_gate)
        {
            try
            {
                if (_device == null) ResolveDevice();
                if (_device == null) return -1.0;
                return Math.Clamp(_device.AudioEndpointVolume.MasterVolumeLevelScalar, 0.0, 1.0);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not read current volume, will re-resolve endpoint: {ex.Message}");
                _device = null;
                return -1.0;
            }
        }
    }

    /// <summary>Set master volume from a 0.0–1.0 value.</summary>
    public void SetScalar(double value)
    {
        if (!_available) return;
        // Quantise to whole percent and skip if unchanged: a slide parks many samples on
        // the same level, and re-sending an identical value just spams the endpoint.
        int pct = (int)Math.Round(Math.Clamp(value, 0.0, 1.0) * 100.0);

        lock (_gate)
        {
            if (pct == _lastVolumePercent) return;
            try
            {
                if (_device == null) ResolveDevice();
                if (_device == null) return;

                _device.AudioEndpointVolume.MasterVolumeLevelScalar = pct / 100f;
                if (pct > 0 && _device.AudioEndpointVolume.Mute)
                    _device.AudioEndpointVolume.Mute = false;
                _lastVolumePercent = pct;
            }
            catch (Exception ex)
            {
                // Endpoint may have gone away (device unplugged); try once more next time.
                Logger.Warn($"Set volume failed, will re-resolve endpoint: {ex.Message}");
                _device = null;
                _lastVolumePercent = -1;
            }
        }
    }

    /// <summary>
    /// Re-acquire the current default playback device. Called at the start of a volume
    /// slide so switching output (e.g. connecting Bluetooth) is picked up, without relying
    /// on system change-notifications.
    /// </summary>
    public void RefreshDefaultDevice()
    {
        lock (_gate)
        {
            try
            {
                ResolveDevice();
                _available = _device != null;
                _lastVolumePercent = -1; // new device: force the next set to apply
            }
            catch (Exception ex)
            {
                Logger.Warn($"Volume device refresh failed: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _device?.Dispose();
            _device = null;
            _enumerator?.Dispose();
            _enumerator = null;
        }
    }
}
