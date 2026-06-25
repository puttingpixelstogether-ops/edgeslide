using System;
using NAudio.CoreAudioApi;

namespace EdgeSlide;

/// <summary>
/// Controls the default render endpoint's master volume via NAudio CoreAudio.
/// Re-resolves the default endpoint on demand so it survives device changes.
/// </summary>
public sealed class VolumeController : IDisposable
{
    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _device;
    private bool _available;
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
                if (_device == null)
                    ResolveDevice();
                if (_device == null)
                    return -1.0;
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
        float target = (float)Math.Clamp(value, 0.0, 1.0);

        lock (_gate)
        {
            try
            {
                if (_device == null)
                    ResolveDevice();
                if (_device == null)
                    return;

                _device.AudioEndpointVolume.MasterVolumeLevelScalar = target;
                if (_device.AudioEndpointVolume.Mute && target > 0)
                    _device.AudioEndpointVolume.Mute = false;
            }
            catch (Exception ex)
            {
                // Endpoint may have gone away (device unplugged); try once more next time.
                Logger.Warn($"Set volume failed, will re-resolve endpoint: {ex.Message}");
                _device = null;
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
