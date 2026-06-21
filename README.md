# EdgeSlide

A tiny Windows system-tray utility that turns the **left and right edge strips of your
Precision Touchpad into continuous sliders**:

- **Left strip → screen brightness**
- **Right strip → master volume**

Slide your finger up and down within the outer ~5 mm of the touchpad and the value
follows in real time, with a small on-screen HUD. The rest of the touchpad keeps
working exactly as normal — cursor movement, scrolling and gestures all pass through
untouched.

The app has **no main window**. It lives in the system tray; the settings panel only
appears when you open it.

---

## Download

Grab the latest **`EdgeSlide.exe`** from the
[**Releases page**](https://github.com/puttingpixelstogether-ops/edgeslide/releases/latest).

It's a single portable file — no installer, nothing else to download. Just run it and
it appears in your system tray. To remove it later, exit it from the tray and delete
the file.

> **First launch:** because the app isn't code-signed yet, Windows SmartScreen may show
> *"Windows protected your PC."* Click **More info → Run anyway**. This is expected for
> small independent apps; the warning fades as more people run the same file.

Requires a **Windows Precision Touchpad** (Windows 10/11). If you enable *Launch at
startup*, keep the `.exe` somewhere permanent first — startup remembers wherever the
file currently lives.

---

## What it does

- Reserves the outer left/right strips (default 5 mm, configurable 3–15 mm) of any
  Windows Precision Touchpad as vertical sliders.
- Left strip controls brightness, right strip controls volume (both reassignable, or
  disable either side).
- Shows a slim HUD in the bottom corner while a gesture is active; it auto-hides after
  1.5 s.
- Carefully avoids false positives: a gesture only starts if your finger **originated**
  in the edge zone, stayed in the inner strip for a short hold, moved mostly vertically,
  and you weren't just typing.

---

## How it works (brief)

| Concern | Approach |
|---|---|
| Touchpad input | **Raw Input** (`RegisterRawInputDevices` + `WM_INPUT`) on the digitizer (usage page `0x0D`, usage `0x05`), with `RIDEV_INPUTSINK` so input arrives even unfocused. |
| Report parsing | `HidP_GetCaps` / `HidP_GetValueCaps` / `HidP_GetUsageValue` / `HidP_GetUsages` from `hid.dll` to extract X, Y, TipSwitch and ContactID per finger. Handles both per-finger and batched multi-contact reports. |
| Brightness | WMI `WmiMonitorBrightnessMethods.WmiSetBrightness`. |
| Volume | CoreAudio via **NAudio** (`MMDeviceEnumerator` → `AudioEndpointVolume.MasterVolumeLevelScalar`). |
| HUD | A chromeless, top-most, no-activate layered `Form`. |

Source files are organised one-concern-per-file: `TouchpadListener` /
`ParsedDevice` (input), `StripGestureDetector` (validation/mapping),
`BrightnessController`, `VolumeController`, `OverlayForm`, `SettingsForm`,
`TrayApp`, `Settings`, `NativeMethods`.

---

## Install (easiest — one double-click)

On a Windows 10/11 PC, **double-click `build.bat`**. It will:

1. Make sure a .NET SDK is present (and offer to install the latest via `winget` if not).
2. Build EdgeSlide **framework-dependent** — it uses the .NET runtime you already
   have installed rather than downloading runtime packs.
3. Copy the output to `%LOCALAPPDATA%\EdgeSlide`.
4. Add a Start Menu shortcut and offer to launch it.

The project targets `net10.0-windows`, so you need the **.NET 10 SDK** to build and
the **.NET 10 Desktop Runtime** to run (the SDK includes the runtime, so if you built
it you can run it).

> Why a build step at all? This app uses WinForms and Windows-only P/Invoke, so it can
> only be compiled on Windows. A pre-built binary can't be produced on Linux/macOS.

## Build manually (for developers)

Requires the **.NET 10 SDK**. From the project folder:

```sh
dotnet build -c Release        # debug/dev build
dotnet run   -c Release        # build and run
dotnet publish -c Release -o publish   # framework-dependent output (what build.bat does)
```

To produce a fully standalone exe that needs no .NET install on the target machine
(larger; downloads runtime packs once), add `-r win-x64 --self-contained true`.

The app runs as a **standard user** — it never requests elevation.

---

## Settings

Left-click the tray icon (or right-click → **Settings**) to open the panel:

- **Strip width (mm)** — 3–15, default 5
- **Left strip action** — Brightness / Volume / Disabled
- **Right strip action** — Volume / Brightness / Disabled
- **Invert direction** — make the bottom of the strip 0%
- **Confirmation hold (ms)** — 50–300, default 80 (how long your finger must stay in
  the strip before the slider engages)
- **Launch at Windows startup** — adds/removes an `HKCU\...\Run` entry

Settings persist to `%APPDATA%\EdgeSlide\settings.json`. A rolling log is written to
`%APPDATA%\EdgeSlide\edgeslide.log`.

Right-click the tray icon for **Settings**, **Enabled** (toggle on/off), and **Exit**.

---

## Known limitations

- **Precision Touchpad only.** Older "legacy" touchpads that don't expose a Windows
  Precision Touchpad HID digitizer won't work; the app shows a tray warning and runs in
  a disabled state.
- **Brightness needs a WMI-controllable display.** Many desktops and external-only
  monitor setups return no `WmiMonitorBrightnessMethods` object; brightness control is
  disabled gracefully in that case (volume still works).
- **Physical-size detection is best-effort.** HID value caps don't reliably expose the
  unit/exponent needed to convert to true millimetres, so when the physical width can't
  be determined the strip falls back to a fixed fraction (~7%) of the touchpad's logical
  X range. Adjust the strip width in settings to taste.
- **OEM utilities may compete for input.** Lenovo Vantage, Asus Armoury Crate and
  similar tools also hook the touchpad. If raw-input packets stop arriving, EdgeSlide
  logs a warning rather than crashing.

---

## Uninstall

1. Right-click the tray icon → **Exit**.
2. In settings, turn off **Launch at Windows startup** before exiting (or delete the
   `EdgeSlide` value under
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).
3. Delete the installed app folder: `%LOCALAPPDATA%\EdgeSlide`.
4. Delete the config/log folder: `%APPDATA%\EdgeSlide`.
5. Delete the Start Menu shortcut:
   `%APPDATA%\Microsoft\Windows\Start Menu\Programs\EdgeSlide.lnk`.

No other system changes are made.
