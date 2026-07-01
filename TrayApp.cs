using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EdgeSlide;

/// <summary>
/// Owns the tray icon and the whole runtime graph. No main window: the only
/// persistent UI is the tray icon (plus the transient HUD during gestures).
/// </summary>
public sealed class TrayApp : ApplicationContext
{
    private Settings _settings;

    private readonly NotifyIcon _tray;
    private Icon? _trayIcon; // currently displayed icon; disposed when replaced
    private readonly ToolStripMenuItem _enabledItem;
    private readonly KeyboardActivityMonitor _keyboard;
    private readonly TouchpadListener _listener;
    private readonly StripGestureDetector _detector;
    private readonly BrightnessController _brightness;
    private readonly VolumeController _volume;
    private readonly OverlayForm _overlay;

    private SettingsForm? _settingsForm;
    private bool _disposed;

    // Background "latest value wins" applier so slow WMI/CoreAudio calls never
    // block the input/UI thread.
    private readonly object _applyGate = new();
    private bool _hasPendingBrightness, _hasPendingVolume;
    private double _pendingBrightness, _pendingVolume;
    private bool _applierRunning;

    // Relative-mode anchor for the current slide (single finger, so one at a time):
    // the value the control had when the slide started, and the finger position then.
    private double _relStartValue, _relAnchorFrac;

    public TrayApp()
    {
        _settings = Settings.Load();

        _keyboard = new KeyboardActivityMonitor();
        _keyboard.Start();

        _brightness = new BrightnessController();
        _volume = new VolumeController();
        _overlay = new OverlayForm();

        _listener = new TouchpadListener();
        _detector = new StripGestureDetector(() => _settings, () => _listener.Geometry, _keyboard);

        _listener.ContactReceived += _detector.OnContact;
        _listener.TouchpadDetected += () => Logger.Info("Touchpad detected and geometry resolved.");
        _detector.GestureUpdated += OnGestureUpdated;
        _detector.GestureEnded += _overlay.HideNow; // hide the HUD immediately on finger lift

        // Tray icon + menu.
        _enabledItem = new ToolStripMenuItem("Enabled", null, OnToggleEnabled) { Checked = _settings.Enabled, CheckOnClick = false };
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Settings", null, (_, _) => OpenSettings()));
        menu.Items.Add(_enabledItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApp()));

        _trayIcon = CreateTrayIcon(_settings.Enabled);
        _tray = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = "EdgeSlide",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.MouseClick += OnTrayClick;

        StartUp();
    }

    private void StartUp()
    {
        bool present = TouchpadListener.HasCompatibleDevice();
        _listener.Start();

        if (!present)
        {
            Logger.Warn("No compatible Precision Touchpad detected at startup.");
            _tray.ShowBalloonTip(5000, "EdgeSlide",
                "No compatible touchpad found. Precision Touchpad required.",
                ToolTipIcon.Warning);
        }

        if (!_brightness.Available)
            Logger.Info("Brightness control unavailable on this system.");
        if (!_volume.Available)
            Logger.Info("Volume control unavailable on this system.");

        Logger.Info($"EdgeSlide started. Enabled={_settings.Enabled}.");
    }

    // ---------------------------------------------------------------------
    // Gesture → UI + controllers
    // ---------------------------------------------------------------------
    private void OnGestureUpdated(GestureUpdate u)
    {
        // At the start of a volume slide, re-acquire the current default output so a
        // switched device (e.g. Bluetooth just connected) is controlled, not the old one.
        if (u.SessionStart && u.Action == StripAction.Volume)
            _volume.RefreshDefaultDevice();

        double value = ComputeAppliedValue(u);

        // HUD (marshals to UI thread itself). Skipped per-control if the user turned that
        // overlay off, e.g. because their system already shows its own volume indicator.
        bool showOverlay = u.Action switch
        {
            StripAction.Brightness => _settings.ShowOverlayBrightness,
            StripAction.Volume => _settings.ShowOverlayVolume,
            _ => true
        };
        if (showOverlay)
            _overlay.ShowValue(u.Side, u.Action, value);

        // Apply value off the UI thread.
        lock (_applyGate)
        {
            if (u.Action == StripAction.Brightness)
            {
                _pendingBrightness = value;
                _hasPendingBrightness = true;
            }
            else if (u.Action == StripAction.Volume)
            {
                _pendingVolume = value;
                _hasPendingVolume = true;
            }

            if (!_applierRunning)
            {
                _applierRunning = true;
                Task.Run(ApplyLoop);
            }
        }
    }

    /// <summary>
    /// Map a gesture update to the value to apply, honouring the strip's slider mode.
    /// Absolute: the finger position is the value. Relative: start from the control's
    /// current level when the slide began and add how far the finger has moved since,
    /// so the value never jumps to where you first touch.
    /// </summary>
    private double ComputeAppliedValue(GestureUpdate u)
    {
        SliderMode mode = u.Side == StripSide.Left ? _settings.LeftStripMode : _settings.RightStripMode;
        if (mode != SliderMode.Relative)
            return u.Value;

        if (u.SessionStart)
        {
            double current = u.Action == StripAction.Brightness
                ? _brightness.GetScalar()
                : _volume.GetScalar();
            if (current < 0) current = u.Value; // couldn't read — fall back to absolute anchor
            _relStartValue = current;
            _relAnchorFrac = u.Value;
            return current; // no change at the moment of touch-down
        }

        return Math.Clamp(_relStartValue + (u.Value - _relAnchorFrac), 0.0, 1.0);
    }

    private void ApplyLoop()
    {
        while (true)
        {
            bool doBrightness, doVolume;
            double b, v;
            lock (_applyGate)
            {
                if (!_hasPendingBrightness && !_hasPendingVolume)
                {
                    _applierRunning = false;
                    return;
                }
                doBrightness = _hasPendingBrightness;
                doVolume = _hasPendingVolume;
                b = _pendingBrightness;
                v = _pendingVolume;
                _hasPendingBrightness = false;
                _hasPendingVolume = false;
            }

            if (doBrightness) _brightness.SetScalar(b);
            if (doVolume) _volume.SetScalar(v);
        }
    }

    // ---------------------------------------------------------------------
    // Tray interactions
    // ---------------------------------------------------------------------
    private void OnTrayClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            OpenSettings();
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            _settingsForm.BringToFront();
            return;
        }

        _settingsForm = new SettingsForm(_settings);
        _settingsForm.SettingsSaved += OnSettingsSaved;
        _settingsForm.FormClosed += (_, _) =>
        {
            _settingsForm?.Dispose();
            _settingsForm = null;
        };
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void OnSettingsSaved(Settings updated)
    {
        _settings = updated;
        _settings.Save();
        _detector.Reset();
        _enabledItem.Checked = _settings.Enabled;
        ApplyTrayIcon(_settings.Enabled);
        Logger.Info("Settings updated.");
    }

    private void OnToggleEnabled(object? sender, EventArgs e)
    {
        _settings.Enabled = !_settings.Enabled;
        _settings.Save();
        _enabledItem.Checked = _settings.Enabled;
        ApplyTrayIcon(_settings.Enabled);
        if (!_settings.Enabled)
            _detector.Reset();
        Logger.Info($"Enabled toggled to {_settings.Enabled}.");
    }

    private void ExitApp()
    {
        Dispose(true);
        ExitThread();
    }

    // ---------------------------------------------------------------------
    // Tray icon — loads the embedded multi-size EdgeSlide.ico so Windows picks
    // the crisp native render for the current DPI. The disabled state is a
    // dimmed, desaturated copy of the same icon.
    // ---------------------------------------------------------------------

    /// <summary>Swap the tray icon, disposing the previous one to avoid a GDI handle leak.</summary>
    private void ApplyTrayIcon(bool enabled)
    {
        Icon? previous = _trayIcon;
        _trayIcon = CreateTrayIcon(enabled);
        _tray.Icon = _trayIcon;
        previous?.Dispose();
    }

    private static Icon CreateTrayIcon(bool enabled)
    {
        Icon? loaded = LoadEmbeddedIcon(SystemInformation.SmallIconSize);
        if (loaded == null)
            return SystemIcons.Application;
        if (enabled)
            return loaded;

        using (loaded)
        using (Bitmap src = loaded.ToBitmap())
        using (var dim = new Bitmap(src.Width, src.Height))
        {
            for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                Color p = src.GetPixel(x, y);
                int gray = (int)(p.R * 0.30 + p.G * 0.59 + p.B * 0.11);
                int a = (int)(p.A * 0.55); // fade it out
                dim.SetPixel(x, y, Color.FromArgb(a, gray, gray, gray));
            }

            IntPtr h = dim.GetHicon();
            using var tmp = Icon.FromHandle(h);
            var icon = (Icon)tmp.Clone();
            NativeDestroyIcon(h);
            return icon;
        }
    }

    private static Icon? LoadEmbeddedIcon(Size size)
    {
        try
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            using System.IO.Stream? s = asm.GetManifestResourceStream("EdgeSlide.ico");
            return s == null ? null : new Icon(s, size);
        }
        catch
        {
            return null;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
    private static extern bool NativeDestroyIcon(IntPtr handle);

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }
        _disposed = true;

        if (disposing)
        {
            try { _tray.Visible = false; } catch { /* ignore */ }
            _tray.Dispose();
            _trayIcon?.Dispose();
            _settingsForm?.Dispose();
            _overlay.Dispose();
            _listener.Dispose();
            _keyboard.Dispose();
            _brightness.Dispose();
            _volume.Dispose();
        }
        base.Dispose(disposing);
    }
}