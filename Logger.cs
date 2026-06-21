using System;
using System.Diagnostics;
using System.IO;

namespace EdgeSlide;

/// <summary>
/// Lightweight file + debug logger. Logs to %APPDATA%\EdgeSlide\edgeslide.log.
/// Best-effort: never throws to the caller.
/// </summary>
internal static class Logger
{
    private static readonly object Gate = new();
    private static string LogPath =>
        Path.Combine(Settings.ConfigDirectory, "edgeslide.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        Debug.WriteLine(line);
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Settings.ConfigDirectory);
                // Keep the log from growing without bound.
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 512 * 1024)
                    File.WriteAllText(LogPath, string.Empty);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }
}
