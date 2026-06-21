# Claude Code prompt — Windows touchpad edge slider

---

## Project

Build a Windows system tray utility in C# (.NET 8, WinForms) called **EdgeSlider** that reserves the left and right 5mm strips of any Windows Precision Touchpad as continuous vertical sliders — left strip controls system brightness, right strip controls master volume. The app starts with no window visible — nothing in the taskbar, nothing in Alt+Tab. The settings panel is a standard WinForms form instantiated on demand and disposed when closed; it is never shown on startup. The only persistent UI is the system tray icon and the overlay HUD during active gestures.

---

## Core behaviour

- On finger contact in the left 5mm strip: map the Y position (0% at top → 100% at bottom, or inverted — user-configurable) to system brightness 0–100%.
- On finger contact in the right 5mm strip: map the Y position to master audio volume 0–100%.
- Changes are continuous — as the finger slides up/down, the value updates in real time.
- Show a native Windows HUD overlay (or a minimal toast near the tray) displaying the current value while the finger is in a strip.
- When no finger is in a strip, the touchpad behaves completely normally (cursor movement, scrolling, gestures all pass through untouched).

---

## Technical approach — read this carefully before writing any code

### Touchpad input
Use **Windows Raw Input** (`RegisterRawInputDevices` / `WM_INPUT`) to receive raw HID packets from the Precision Touchpad device.

Device filter:
- Usage page: `0x000D` (Digitizer)
- Usage: `0x0005` (Touch Pad)
- Flag: `RIDEV_INPUTSINK` so input is received even when the app is not focused.

Parse incoming HID reports using `HidP_GetUsageValueArray` / `HidP_GetUsageValue` from `hid.dll` (P/Invoke) to extract:
- `X` absolute position per contact point
- `Y` absolute position per contact point
- `TipSwitch` (finger on/off)
- `ContactID` (track individual fingers)

Get the touchpad's logical X/Y range by calling `HidP_GetValueCaps` on the preparsed data once at startup.

> Do NOT use WM_POINTER or the Windows Precision Touchpad gesture APIs — they give you post-processed gesture events, not raw coordinates. Raw Input is the correct path.

### Determining the 5mm strip width
At startup, query the physical touchpad dimensions using `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\HID\...\Device Parameters` or via the HID descriptor's physical min/max values. Convert 5mm to logical units using the formula:

```
logicalUnitsPerMm = (logicalMax - logicalMin) / physicalRangeInMm
stripWidthLogical = 5 * logicalUnitsPerMm
```

If physical dimensions cannot be reliably read, fall back to using the bottom 7% of the logical X range as the left strip and top 93% as the right strip boundary (configurable).

### Brightness control
Use `Windows.Devices.Power` or the `WmiSetBrightness` WMI method:

```csharp
using var searcher = new ManagementObjectSearcher(
    "root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
foreach (ManagementObject obj in searcher.Get())
    obj.InvokeMethod("WmiSetBrightness", new object[] { 1, targetBrightness });
```

Alternatively use `SetDeviceGammaRamp` as a fallback for non-WMI displays. Try WMI first.

### Volume control
Use **CoreAudio** via P/Invoke or the `NAudio` NuGet package (`NAudio.Core`). Target the default audio endpoint's master volume:

```csharp
// Using NAudio:
var device = new MMDeviceEnumerator()
    .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
device.AudioEndpointVolume.MasterVolumeLevelScalar = value; // 0.0f–1.0f
```

### False positive prevention

The core principle: a gesture is only valid if the finger **originated** in the activation zone. A finger that starts in the middle of the touchpad and drifts into the strip must never trigger a gesture, leaving the full touchpad surface freely usable for normal navigation.

**Entry zone rule (most important):**
Track each `ContactID` from its first `TipSwitch` on-event. Record the X position at first contact. Only open a gesture session if that initial X is within the **activation zone** — defined as 2× the configured strip width (so 10mm by default) from the edge. If the finger started outside this zone, ignore all subsequent position updates for that contact ID, even if it later moves into the strip.

```
// On new contact:
bool isEligible = (contactId not seen before) &&
                  (initialX <= activationZoneWidth  // left strip candidate
                   || initialX >= logicalMax - activationZoneWidth); // right

// Store per ContactID: { eligible, side, startX, startY, startTime }
// If not eligible: discard all further events for this ContactID
```

**Gesture confirmation (after entry zone check passes):**
- The finger must remain in the **inner strip** (1× strip width, e.g. 5mm) for at least **80ms continuously** before the gesture activates and starts affecting the controlled value.
- Movement must be primarily vertical: `abs(deltaY) > abs(deltaX) * 1.5` over the first 100ms of the session. If the dominant direction is horizontal, cancel the session for this contact.
- If the user is typing (last `WM_KEYDOWN` within 800ms), suppress all strip gestures entirely.

**On `TipSwitch` off:** clear the contact's session state unconditionally. The next touch starts fresh.

### Overlay / HUD
Show a slim `Form` (no chrome, `TopMost = true`, `ShowInTaskbar = false`, `FormBorderStyle = None`) in the bottom-left or bottom-right corner of the screen. It appears on gesture start and auto-hides after 1.5s of no movement. Display:
- A vertical bar showing current value.
- A percentage label.
- The icon/label for the active control (brightness sun icon, volume speaker icon — use Unicode: ☀ and 🔊 or simple drawn shapes).

Use `SetWindowPos` with `HWND_TOPMOST` and `WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT` so it doesn't steal focus or appear in Alt+Tab.

---

## System tray & settings

System tray icon (use a simple programmatically drawn icon — no external assets required):
- **Left-click**: instantiate and show `SettingsForm` if not already open; bring to front if it is.
- **Right-click**: context menu with: Settings, Toggle enabled/disabled, Exit.

Settings panel (persist to `%APPDATA%\EdgeSlider\settings.json`):
```
Strip width (mm): [slider 3–15mm, default 5]
Left strip action: [Brightness | Volume | Disabled]
Right strip action: [Volume | Brightness | Disabled]
Invert direction: [checkbox]
Sensitivity (debounce ms): [slider 50–300ms, default 80]
Launch at Windows startup: [checkbox]  ← writes to HKCU Run key
```

---

## Project structure

```
EdgeSlider/
├── EdgeSlider.csproj          (.NET 8, WinForms, x64)
├── Program.cs                 (entry point, Application.Run with tray)
├── TrayApp.cs                 (ApplicationContext subclass, owns tray icon)
├── TouchpadListener.cs        (Raw Input registration + HID parsing)
├── StripGestureDetector.cs    (debounce, direction check, strip hit test)
├── BrightnessController.cs    (WMI brightness)
├── VolumeController.cs        (NAudio / CoreAudio)
├── OverlayForm.cs             (HUD overlay)
├── SettingsForm.cs            (settings UI)
├── Settings.cs                (model + JSON persistence)
└── NativeMethods.cs           (all P/Invoke declarations in one place)
```

---

## NuGet dependencies

```xml
<PackageReference Include="NAudio" Version="2.2.1" />
<PackageReference Include="System.Management" Version="8.0.0" />
```

No other third-party dependencies. Do not use WPF. WinForms only.

---

## NativeMethods.cs — required P/Invoke signatures

Include at minimum:
- `RegisterRawInputDevices` (user32)
- `GetRawInputData` (user32)
- `GetRawInputDeviceInfo` (user32 — for preparsed data)
- `HidP_GetCaps`, `HidP_GetValueCaps`, `HidP_GetUsageValue` (hid.dll)
- `SetWindowPos`, `SetLayeredWindowAttributes` (user32 — for overlay)
- All structs: `RAWINPUTDEVICE`, `RAWINPUTHEADER`, `RAWINPUT`, `HIDP_CAPS`, `HIDP_VALUE_CAPS`

---

## Build & run requirements

- Target: `net8.0-windows`, `<PlatformTarget>x64</PlatformTarget>`, `<UseWindowsForms>true</UseWindowsForms>`
- Must compile and run on Windows 10/11 with no additional runtime installs beyond .NET 8.
- The app must request no elevated permissions — run entirely as a standard user.
- Include a `README.md` covering: what it does, how to build (`dotnet build`), known limitation (Precision Touchpad only), and how to uninstall (delete `%APPDATA%\EdgeSlider`, remove from startup).

---

## What to build first (suggested order)

1. Scaffold the project and tray icon — confirm it runs and sits in tray.
2. Raw Input registration and HID packet receipt — log raw bytes to console to confirm touchpad events arrive.
3. HID parsing — extract X, Y, TipSwitch per contact and log them.
4. Strip detection — identify left/right strip contacts from coordinates.
5. Brightness controller — test WMI set with a hardcoded value.
6. Volume controller — test NAudio set with a hardcoded value.
7. Wire gesture detector to controllers with debounce.
8. Build overlay HUD.
9. Settings persistence and settings form.
10. Startup registry entry, tray right-click menu, polish.

Build and test each step before proceeding to the next.

---

## Known risks to handle explicitly

- **Touchpad not found**: if no Precision Touchpad HID device is detected at startup, show a tray balloon: "EdgeSlider: No compatible touchpad found. Precision Touchpad required." and run in disabled mode.
- **WMI brightness fails**: on some systems `WmiMonitorBrightnessMethods` returns no objects (desktop monitors, external-only setups). Catch and log; disable brightness control gracefully.
- **HID parsing varies by manufacturer**: some touchpads report each finger as a separate HID report, others batch contacts. Handle both patterns in `TouchpadListener.cs`.
- **Conflicts with OEM drivers**: Lenovo Vantage and Asus Armoury Crate also hook touchpad input. If raw input packets stop arriving, log a warning but do not crash.
