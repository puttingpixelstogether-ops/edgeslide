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
        // HUD (marshals to UI thread itself).
        _overlay.ShowValue(u.Side, u.Action, u.Value);

        // Apply value off the UI thread.
        lock (_applyGate)
        {
            if (u.Action == StripAction.Brightness)
            {
                _pendingBrightness = u.Value;
                _hasPendingBrightness = true;
            }
            else if (u.Action == StripAction.Volume)
            {
                _pendingVolume = u.Value;
                _hasPendingVolume = true;
            }

            if (!_applierRunning)
            {
                _applierRunning = true;
                Task.Run(ApplyLoop);
            }
        }
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