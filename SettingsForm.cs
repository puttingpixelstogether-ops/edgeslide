using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EdgeSlide;

/// <summary>
/// On-demand settings panel rendered as an HTML/CSS page inside a WebView2 control.
/// The look is defined entirely in SettingsUI.html; this class only hosts the view
/// and bridges settings to/from the page via JSON messages.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly Settings _working;
    private readonly WebView2 _web = new();
    private bool _sentInitial;

    /// <summary>Raised with the new settings when the user clicks Save.</summary>
    public event Action<Settings>? SettingsSaved;

    public SettingsForm(Settings current)
    {
        _working = current.Clone();

        Text = "EdgeSlide";
        FormBorderStyle = FormBorderStyle.Sizable; // resizable
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(10, 10, 11); // matches the dark settings UI (#0A0A0B)

        // Initial size; the real, DPI-correct size is applied in OnHandleCreated once
        // the monitor's DPI is known. Minimum is small because the UI is responsive.
        ClientSize = new Size(1040, 840);
        MinimumSize = new Size(520, 440);

        // Use the app icon for the window title bar / taskbar (not the default).
        try
        {
            using Stream? ico = Assembly.GetExecutingAssembly().GetManifestResourceStream("EdgeSlide.ico");
            if (ico != null) Icon = new Icon(ico);
        }
        catch { /* fall back to the default icon */ }

        _web.Dock = DockStyle.Fill;
        _web.DefaultBackgroundColor = Color.FromArgb(10, 10, 11); // avoid a white flash while loading
        Controls.Add(_web);

        Load += async (_, _) => await InitWebViewAsync();
    }

    private async Task InitWebViewAsync()
    {
        try
        {
            // Keep WebView2's user-data folder inside our own config dir so it works
            // even when the app is installed under Program Files.
            string userDataFolder = Path.Combine(Settings.ConfigDirectory, "WebView2");
            Directory.CreateDirectory(userDataFolder);

            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await _web.EnsureCoreWebView2Async(env);

            CoreWebView2 core = _web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;

            core.WebMessageReceived += OnWebMessageReceived;

            // Open external links (e.g. the GitHub link) in the user's real browser,
            // not inside the settings webview.
            core.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(args.Uri) { UseShellExecute = true });
                }
                catch (Exception ex) { Logger.Warn($"Could not open link {args.Uri}: {ex.Message}"); }
            };

            core.NavigationCompleted += (_, _) =>
            {
                if (_sentInitial) return;
                _sentInitial = true;
                core.PostWebMessageAsJson(BuildSettingsJson());
            };

            core.NavigateToString(LoadHtml());
        }
        catch (Exception ex)
        {
            Logger.Warn($"WebView2 init failed: {ex.Message}");
            MessageBox.Show(this,
                "The settings window needs the Microsoft Edge WebView2 runtime, which could not be started.\n\n" +
                "Install the Evergreen WebView2 Runtime from:\n" +
                "https://developer.microsoft.com/microsoft-edge/webview2/\n\nthen open Settings again.",
                "EdgeSlide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        UiMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<UiMessage>(e.TryGetWebMessageAsString(), JsonOpts);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Bad settings message from UI: {ex.Message}");
            return;
        }
        if (msg == null) return;

        if (msg.Action == "cancel")
        {
            Close();
            return;
        }

        if (msg.Action == "save")
        {
            _working.StripWidthMm = msg.StripWidthMm;
            _working.LeftStripAction = ParseAction(msg.LeftAction, StripAction.Brightness);
            _working.RightStripAction = ParseAction(msg.RightAction, StripAction.Volume);
            _working.LeftStripMode = ParseMode(msg.LeftMode, SliderMode.Relative);
            _working.RightStripMode = ParseMode(msg.RightMode, SliderMode.Relative);
            _working.InvertDirection = msg.Invert;
            _working.ShowOverlayBrightness = msg.ShowOsdBrightness;
            _working.ShowOverlayVolume = msg.ShowOsdVolume;
            _working.DebounceMs = msg.DebounceMs;
            _working.LaunchAtStartup = msg.LaunchAtStartup;
            _working.Clamp();

            StartupManager.Apply(_working.LaunchAtStartup);
            SettingsSaved?.Invoke(_working);
            Close();
        }
    }

    private string BuildSettingsJson()
    {
        var dto = new UiMessage
        {
            Action = "load",
            Version = AppVersion(),
            StripWidthMm = (int)Math.Round(_working.StripWidthMm),
            LeftAction = _working.LeftStripAction.ToString(),
            RightAction = _working.RightStripAction.ToString(),
            LeftMode = _working.LeftStripMode.ToString(),
            RightMode = _working.RightStripMode.ToString(),
            Invert = _working.InvertDirection,
            ShowOsdBrightness = _working.ShowOverlayBrightness,
            ShowOsdVolume = _working.ShowOverlayVolume,
            DebounceMs = _working.DebounceMs,
            LaunchAtStartup = _working.LaunchAtStartup || StartupManager.IsEnabled()
        };
        return JsonSerializer.Serialize(dto, JsonOpts);
    }

    private static string LoadHtml()
    {
        // SettingsUI.html is embedded; resource name is "<RootNamespace>.SettingsUI.html".
        Assembly asm = Assembly.GetExecutingAssembly();
        string resName = "EdgeSlide.SettingsUI.html";
        using Stream? s = asm.GetManifestResourceStream(resName);
        if (s == null)
            return "<html><body style='font-family:Segoe UI'>Settings UI resource missing.</body></html>";
        using var reader = new StreamReader(s);
        return reader.ReadToEnd();
    }

    private static StripAction ParseAction(string? s, StripAction fallback)
        => Enum.TryParse(s, ignoreCase: true, out StripAction a) ? a : fallback;

    private static SliderMode ParseMode(string? s, SliderMode fallback)
        => Enum.TryParse(s, ignoreCase: true, out SliderMode m) ? m : fallback;

    /// <summary>App version (from the assembly, i.e. the .csproj &lt;Version&gt;), formatted "vMAJOR.MINOR.PATCH".</summary>
    private static string AppVersion()
    {
        Version? v = Assembly.GetExecutingAssembly().GetName().Version;
        return v == null ? "" : $"v{v.Major}.{v.Minor}.{v.Build}";
    }

    // Theme the window title bar to match the settings UI: dark mode + caption painted
    // the exact background colour (Win11 22000+ for caption colour; dark mode on Win10 2004+).
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int useDark = 1;
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

            // COLORREF is 0x00BBGGRR. Background #0A0A0B -> R=0A G=0A B=0B.
            int caption = 0x000B0A0A;
            DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
            // Text #F0EEE8 -> R=F0 G=EE B=E8 -> 0x00E8EEF0.
            int captionText = 0x00E8EEF0;
            DwmSetWindowAttribute(Handle, DWMWA_TEXT_COLOR, ref captionText, sizeof(int));
        }
        catch { /* older Windows without these attributes — ignore */ }

        // DPI-correct sizing. WinForms window sizes are in physical pixels; WebView2
        // renders CSS px = physical / (DPI/96). To give the page a comfortable ~1040x840
        // CSS layout, scale the physical window by the DPI factor (clamped to the screen).
        try
        {
            double scale = DeviceDpi / 96.0;
            Rectangle wa = Screen.FromHandle(Handle).WorkingArea;
            int w = Math.Min((int)Math.Round(1040 * scale), wa.Width  - 40);
            int h = Math.Min((int)Math.Round(840  * scale), wa.Height - 40);
            ClientSize = new Size(w, h);
            Location = new Point(wa.X + (wa.Width - Width) / 2, wa.Y + (wa.Height - Height) / 2);
        }
        catch { /* keep the constructor size */ }
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Wire contract shared with SettingsUI.html (camelCase JSON).</summary>
    private sealed class UiMessage
    {
        [JsonPropertyName("action")] public string Action { get; set; } = "";
        [JsonPropertyName("version")] public string Version { get; set; } = "";
        [JsonPropertyName("stripWidthMm")] public int StripWidthMm { get; set; }
        [JsonPropertyName("leftAction")] public string LeftAction { get; set; } = "";
        [JsonPropertyName("rightAction")] public string RightAction { get; set; } = "";
        [JsonPropertyName("leftMode")] public string LeftMode { get; set; } = "";
        [JsonPropertyName("rightMode")] public string RightMode { get; set; } = "";
        [JsonPropertyName("invert")] public bool Invert { get; set; }
        [JsonPropertyName("showOsdBrightness")] public bool ShowOsdBrightness { get; set; } = true;
        [JsonPropertyName("showOsdVolume")] public bool ShowOsdVolume { get; set; } = true;
        [JsonPropertyName("debounceMs")] public int DebounceMs { get; set; }
        [JsonPropertyName("launchAtStartup")] public bool LaunchAtStartup { get; set; }
    }
}
