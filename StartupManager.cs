using System;
using Microsoft.Win32;

namespace EdgeSlide;

/// <summary>Manages the HKCU "Run" entry for launch-at-Windows-startup.</summary>
internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "EdgeSlide";

    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Could not read startup key: {ex.Message}");
            return false;
        }
    }

    public static void Apply(bool enabled)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) return;

            if (enabled)
            {
                string exe = Environment.ProcessPath
                             ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                             ?? string.Empty;
                if (string.IsNullOrEmpty(exe)) return;
                key.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                if (key.GetValue(ValueName) != null)
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Could not update startup key: {ex.Message}");
        }
    }
}
