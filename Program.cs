using System;
using System.Threading;
using System.Windows.Forms;

namespace EdgeSlide;

internal static class Program
{
    // Single-instance guard so two copies don't both fight over raw input.
    private static Mutex? _instanceMutex;

    [STAThread]
    private static void Main()
    {
        const string mutexName = "EdgeSlide_SingleInstance_{B1F4C2A0-6E2D-4D5B-9A0E-2C7F8A1D4E91}";
        _instanceMutex = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            // Already running; just exit quietly.
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            // TrayApp is an ApplicationContext: no main window, lives in the tray.
            using var app = new TrayApp();
            Application.Run(app);
        }
        finally
        {
            _instanceMutex.ReleaseMutex();
            _instanceMutex.Dispose();
        }
    }
}
