using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EdgeSlide;

public enum StripAction
{
    Disabled = 0,
    Brightness = 1,
    Volume = 2
}

/// <summary>How a strip maps finger movement to the control's value.</summary>
public enum SliderMode
{
    /// <summary>Value snaps to the finger's position (top = max, bottom = min).</summary>
    Absolute = 0,
    /// <summary>Value adjusts from its current level by how far the finger moves,
    /// so where you first touch doesn't jump the value.</summary>
    Relative = 1
}

/// <summary>
/// User-configurable settings, persisted to %APPDATA%\EdgeSlide\settings.json.
/// </summary>
public sealed class Settings
{
    // Strip geometry
    public double StripWidthMm { get; set; } = 5.0;        // 3–15 mm
    public StripAction LeftStripAction { get; set; } = StripAction.Brightness;
    public StripAction RightStripAction { get; set; } = StripAction.Volume;

    // Behaviour
    public bool InvertDirection { get; set; } = false;     // false: top=100%, bottom=0%
    public SliderMode LeftStripMode { get; set; } = SliderMode.Relative;   // per-strip mapping
    public SliderMode RightStripMode { get; set; } = SliderMode.Relative;
    public bool ShowOverlayBrightness { get; set; } = true; // draw EdgeSlide's HUD for brightness
    public bool ShowOverlayVolume { get; set; } = true;     // draw EdgeSlide's HUD for volume
    public int DebounceMs { get; set; } = 80;              // 50–300 ms confirmation hold
    public bool LaunchAtStartup { get; set; } = false;
    public bool Enabled { get; set; } = true;              // master on/off (tray toggle)

    // Fallback strip sizing when physical dimensions can't be read.
    // Spec default: left strip = bottom (low-X) % of logical range; right strip = top (high-X) %.
    public double FallbackStripFractionOfRange { get; set; } = 0.07; // 7%

    [JsonIgnore]
    public static string ConfigDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EdgeSlide");

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                Settings? loaded = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
                if (loaded != null)
                {
                    loaded.Clamp();
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to load settings, using defaults: {ex.Message}");
        }

        return new Settings();
    }

    public void Save()
    {
        try
        {
            Clamp();
            Directory.CreateDirectory(ConfigDirectory);
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>Keep values inside their valid UI ranges.</summary>
    public void Clamp()
    {
        StripWidthMm = Math.Clamp(StripWidthMm, 3.0, 15.0);
        DebounceMs = Math.Clamp(DebounceMs, 50, 300);
        FallbackStripFractionOfRange = Math.Clamp(FallbackStripFractionOfRange, 0.02, 0.20);
    }

    public Settings Clone()
    {
        return new Settings
        {
            StripWidthMm = StripWidthMm,
            LeftStripAction = LeftStripAction,
            RightStripAction = RightStripAction,
            InvertDirection = InvertDirection,
            LeftStripMode = LeftStripMode,
            RightStripMode = RightStripMode,
            ShowOverlayBrightness = ShowOverlayBrightness,
            ShowOverlayVolume = ShowOverlayVolume,
            DebounceMs = DebounceMs,
            LaunchAtStartup = LaunchAtStartup,
            Enabled = Enabled,
            FallbackStripFractionOfRange = FallbackStripFractionOfRange
        };
    }
}
