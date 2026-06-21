using System;
using System.Runtime.InteropServices;

namespace EdgeSlide;

/// <summary>
/// All P/Invoke declarations, constants, and interop structs in one place.
/// Covers: Raw Input (user32), HID parsing (hid.dll), and overlay window styling (user32).
/// </summary>
internal static class NativeMethods
{
    // ---------------------------------------------------------------------
    // HID usage pages / usages
    // ---------------------------------------------------------------------
    public const ushort HID_USAGE_PAGE_GENERIC = 0x0001;
    public const ushort HID_USAGE_PAGE_DIGITIZER = 0x000D;

    public const ushort HID_USAGE_GENERIC_X = 0x0030;
    public const ushort HID_USAGE_GENERIC_Y = 0x0031;

    public const ushort HID_USAGE_DIGITIZER_TOUCH_PAD = 0x0005;
    public const ushort HID_USAGE_DIGITIZER_TIP_SWITCH = 0x0042;
    public const ushort HID_USAGE_DIGITIZER_CONTACT_ID = 0x0051;
    public const ushort HID_USAGE_DIGITIZER_CONTACT_COUNT = 0x0054;

    // ---------------------------------------------------------------------
    // Raw Input device-registration flags
    // ---------------------------------------------------------------------
    public const uint RIDEV_INPUTSINK = 0x00000100;
    public const uint RIDEV_REMOVE = 0x00000001;

    // GetRawInputData commands
    public const uint RID_INPUT = 0x10000003;
    public const uint RID_HEADER = 0x10000005;

    // Raw input type
    public const uint RIM_TYPEHID = 2;

    // GetRawInputDeviceInfo commands
    public const uint RIDI_PREPARSEDDATA = 0x20000005;
    public const uint RIDI_DEVICENAME = 0x20000007;
    public const uint RIDI_DEVICEINFO = 0x2000000b;

    // Window messages
    public const int WM_INPUT = 0x00FF;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_SYSKEYDOWN = 0x0104;

    // HIDP status codes
    public const int HIDP_STATUS_SUCCESS = 0x00110000;

    // HIDP report types
    public const int HidP_Input = 0;
    public const int HidP_Output = 1;
    public const int HidP_Feature = 2;

    // ---------------------------------------------------------------------
    // Overlay window styling (SetWindowPos / extended styles)
    // ---------------------------------------------------------------------
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public const uint LWA_ALPHA = 0x00000002;

    // ---------------------------------------------------------------------
    // Structs
    // ---------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    // We only ever read HID raw input, so model the HID variant of the union.
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWHID
    {
        public uint dwSizeHid;   // size of one HID report, in bytes
        public uint dwCount;     // number of HID reports in bRawData
        // followed by bRawData[dwSizeHid * dwCount] — read manually from the buffer
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RID_DEVICE_INFO_HID
    {
        public uint dwVendorId;
        public uint dwProductId;
        public uint dwVersionNumber;
        public ushort usUsagePage;
        public ushort usUsage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RID_DEVICE_INFO
    {
        public uint cbSize;
        public uint dwType;
        public RID_DEVICE_INFO_HID hid; // valid when dwType == RIM_TYPEHID
        // The keyboard variant of the union (6 DWORDs = 24 bytes) is the largest.
        // hid is 16 bytes, so 8 bytes (2 DWORDs) of padding make the struct match
        // the native sizeof RID_DEVICE_INFO (32 bytes) that cbSize must equal.
        private readonly uint _pad0;
        private readonly uint _pad1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    // HIDP_VALUE_CAPS is a large struct with a union on the trailing fields.
    // The layout below matches the Windows SDK (hidpi.h) for the "Range/NotRange"
    // and "Data field" unions. We use explicit layout to map the unions safely.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HIDP_VALUE_CAPS
    {
        public ushort UsagePage;
        public byte ReportID;
        [MarshalAs(UnmanagedType.U1)] public bool IsAlias;
        public ushort BitField;
        public ushort LinkCollection;
        public ushort LinkUsage;
        public ushort LinkUsagePage;
        [MarshalAs(UnmanagedType.U1)] public bool IsRange;
        [MarshalAs(UnmanagedType.U1)] public bool IsStringRange;
        [MarshalAs(UnmanagedType.U1)] public bool IsDesignatorRange;
        [MarshalAs(UnmanagedType.U1)] public bool IsAbsolute;
        [MarshalAs(UnmanagedType.U1)] public bool HasNull;
        public byte Reserved;
        public ushort BitSize;
        public ushort ReportCount;
        public ushort Reserved2a;
        public ushort Reserved2b;
        public ushort Reserved2c;
        public ushort Reserved2d;
        public ushort Reserved2e;
        public uint UnitsExp;   // HID unit exponent (must be present for correct alignment)
        public uint Units;      // HID unit code
        public int LogicalMin;
        public int LogicalMax;
        public int PhysicalMin;
        public int PhysicalMax;
        // --- union: Range vs NotRange ---
        public ushort UsageMin;       // Range.UsageMin   / NotRange.Usage
        public ushort UsageMax;       // Range.UsageMax   / NotRange.Reserved1
        public ushort StringMin;
        public ushort StringMax;
        public ushort DesignatorMin;
        public ushort DesignatorMax;
        public ushort DataIndexMin;   // Range.DataIndexMin / NotRange.DataIndex
        public ushort DataIndexMax;
    }

    // ---------------------------------------------------------------------
    // user32 — Raw Input
    // ---------------------------------------------------------------------
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterRawInputDevices(
        [In] RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize,
        uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICELIST
    {
        public IntPtr hDevice;
        public uint dwType;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceList(
        IntPtr pRawInputDeviceList,
        ref uint puiNumDevices,
        uint cbSize);

    // ---------------------------------------------------------------------
    // user32 — overlay window styling
    // ---------------------------------------------------------------------
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetLayeredWindowAttributes(
        IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    // ---------------------------------------------------------------------
    // hid.dll — preparsed-data parsing
    // ---------------------------------------------------------------------
    [DllImport("hid.dll")]
    public static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

    [DllImport("hid.dll")]
    public static extern int HidP_GetValueCaps(
        int reportType,
        [In, Out] HIDP_VALUE_CAPS[] valueCaps,
        ref ushort valueCapsLength,
        IntPtr preparsedData);

    [DllImport("hid.dll")]
    public static extern int HidP_GetUsageValue(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        out uint usageValue,
        IntPtr preparsedData,
        IntPtr report,
        uint reportLength);

    [DllImport("hid.dll")]
    public static extern int HidP_GetUsageValueArray(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        IntPtr usageValue,
        ushort usageValueByteLength,
        IntPtr preparsedData,
        IntPtr report,
        uint reportLength);

    // Returns number of buttons currently set; we use the data-length overload
    // to learn how many usages are active in a report.
    [DllImport("hid.dll")]
    public static extern int HidP_GetUsages(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        [In, Out] ushort[] usageList,
        ref uint usageLength,
        IntPtr preparsedData,
        IntPtr report,
        uint reportLength);

    [DllImport("hid.dll")]
    public static extern int HidP_MaxUsageListLength(
        int reportType,
        ushort usagePage,
        IntPtr preparsedData);

    // ---------------------------------------------------------------------
    // Low-level keyboard hook (for typing-suppression)
    // ---------------------------------------------------------------------
    public const int WH_KEYBOARD_LL = 13;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(
        int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);
}
