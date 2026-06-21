using System;
using System.Diagnostics;
using static EdgeSlide.NativeMethods;

namespace EdgeSlide;

/// <summary>
/// Installs a low-level keyboard hook and records the timestamp of the most
/// recent key press, so the gesture detector can suppress strip gestures while
/// the user is typing.
/// </summary>
public sealed class KeyboardActivityMonitor : IDisposable
{
    private IntPtr _hook = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc; // keep a reference so the GC won't collect it
    private long _lastKeyDownTicks;
    private bool _disposed;

    public void Start()
    {
        if (_hook != IntPtr.Zero) return;

        _proc = HookCallback;
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule? curModule = curProcess.MainModule;
        IntPtr hMod = curModule != null ? GetModuleHandle(curModule.ModuleName) : GetModuleHandle(null);

        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
        if (_hook == IntPtr.Zero)
            Logger.Warn("Failed to install keyboard hook; typing-suppression disabled.");
    }

    /// <summary>True if a key was pressed within <paramref name="windowMs"/> milliseconds.</summary>
    public bool TypedWithin(int windowMs)
    {
        long last = System.Threading.Interlocked.Read(ref _lastKeyDownTicks);
        if (last == 0) return false;
        double elapsedMs = (Stopwatch.GetTimestamp() - last) * 1000.0 / Stopwatch.Frequency;
        return elapsedMs <= windowMs;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                System.Threading.Interlocked.Exchange(ref _lastKeyDownTicks, Stopwatch.GetTimestamp());
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        _proc = null;
    }
}
