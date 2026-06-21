# EdgeSlide — Pragmatic Complexity Review

> Reviewer focus: over-engineering, requirements alignment, consistency drift, build/runtime correctness. Scope: full source tree (~2,400 LOC, .NET WinForms tray app).

## 1. Complexity Assessment: Medium — and mostly justified

This is a lean, well-structured WinForms tray app. The heavy parts — raw HID parsing (`ParsedDevice`, `TouchpadListener`), the gesture state machine (`StripGestureDetector`), and the HID unit-decode math — are **essential** complexity. Windows exposes no simpler API for reading touchpad edge contacts, so this is not over-engineering. No Redis, no DI container, no middleware stack, no enterprise patterns shoehorned into an MVP. The "latest-value-wins" applier in `TrayApp.ApplyLoop` is a sensible coalescer, not gold-plating.

The real problems are **housekeeping and consistency drift from a rename**, plus one **contradiction between the build script and the project file**. None require architectural change.

**Do NOT refactor the HID/gesture code — that complexity is load-bearing.**

---

## 2. Key Issues Found

### [Critical] Build script contradicts the .csproj — shipped exe may not run
- `build.bat:7` advertises *"a standalone EdgeSlide.exe (no .NET install needed to run)"*.
- `EdgeSlide.csproj` is deliberately **framework-dependent** (`No RuntimeIdentifier / SelfContained`).
- Result: a user on a machine without the .NET 10 runtime double-clicks the exe and nothing happens.
- **Fix:** make the promise and the csproj agree (see §3).

### [High] Stale rename leftovers — `EdgeSlider` vs `EdgeSlide` (context loss)
The project was renamed from `EdgeSlider` to `EdgeSlide`, but not everything followed:
- `publish/EdgeSlider.exe` + `publish/EdgeSlider.dll` (Jun 19) sit beside the current `publish/EdgeSlide.exe` (Jun 21).
- `obj/Release/net10.0-windows/EdgeSlider.AssemblyInfo.cs` (and the `win-x64` variant) still generated under the old name.
- `build.bat:5` comment says *"the .NET 8 SDK"* while the script actually installs `Microsoft.DotNet.SDK.10` and the csproj targets `net10.0-windows`.
- Credit: `uninstall.bat` step 8 **correctly** cleans up legacy `EdgeSlider` leftovers — that part is fine.

### [High] Orphaned duplicate settings UI — 444 lines of dead code
- `settings.html` (444 lines, repo root) is referenced by **nothing** — not in any `.cs`, `.csproj`, or `.bat`.
- The live UI is the embedded `SettingsUI.html` (325 lines), loaded via `EmbeddedResource` and `SettingsForm.LoadHtml()`.
- Two near-identical files invite editing the wrong one.

### [Medium] `BrightnessController.SetScalar` rebuilds the WMI query on every call
- Each brightness tick news up a fresh `ManagementObjectSearcher` and runs `SELECT * FROM WmiMonitorBrightnessMethods`.
- WMI is slow; during a slide this re-queries continuously.
- `Probe()` already proved the object exists but caches nothing.

### [Low] Dead event `GestureEnded`
- `StripGestureDetector` raises `GestureEnded` at lines 97 and 124, but nothing subscribes.
- The HUD hides via its own timer (`OverlayForm`), so the event is dead wiring. Consume it or delete it.

### [Low] Tray-icon dimming via per-pixel `GetPixel`/`SetPixel`
- `TrayApp.CreateTrayIcon` loops every pixel to desaturate the disabled icon.
- Slow per-pixel GDI, but runs only on enable/disable toggle, so impact is negligible. Tidiness only.

---

## 3. Recommended Simplifications

### Critical — make the build match reality (pick ONE)

**Option A — actually be standalone** (matches the "just double-click" promise):
```xml
<!-- EdgeSlide.csproj -->
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
```

**Option B — stay framework-dependent** and fix the wording in `build.bat` to *"requires the .NET 10 runtime."*

Do not claim both.

### High — delete the cruft
- `git rm settings.html`
- Clear stale `publish/EdgeSlider.*` and the `obj/` rename artifacts.
- Fix the two stale comments in `build.bat` (".NET 8" → ".NET 10"; reconcile "standalone" wording with Option A/B above).
- Removes ~450 lines and a whole class of "which file/exe is real?" confusion.

### Medium — cache the brightness object once
```csharp
// Probe() keeps the proven ManagementObject instead of re-querying each tick
private ManagementObject? _monitor;

// in Probe():
//   _monitor = (ManagementObject)results.Cast<ManagementBaseObject>().First();

// in SetScalar():
//   _monitor?.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, target });
```
Re-probe only on failure. Removes a query-per-frame and shrinks the method.

### Low — resolve `GestureEnded`
Delete it, or subscribe in `TrayApp` to hide the HUD immediately on finger-lift instead of waiting for the 1.5s auto-hide timer.

---

## 4. Priority Actions

1. **Fix the build/csproj contradiction** (Critical) — the only issue that can hand a user a non-working app.
2. **Purge rename leftovers + the orphaned `settings.html`** (High) — biggest confusion-per-minute payoff, near-zero risk.
3. **Cache the WMI brightness object** (Medium) — the one genuine runtime-cost simplification.

Everything else is fine as-is.

---

## 5. Validation After Changes
- Confirm a clean-machine `build.bat` run still produces a launchable exe (test the self-contained-vs-framework choice end to end).
- Confirm the self-contained-vs-framework decision matches documented intent in `touchpad-slider-prompt.md` / `README.md`.

---

## Issue Summary Table

| # | Severity | Issue | File(s) | Action |
|---|----------|-------|---------|--------|
| 1 | Critical | Build claims standalone; csproj is framework-dependent | `build.bat:7`, `EdgeSlide.csproj` | Reconcile (self-contained or fix wording) |
| 2 | High | `EdgeSlider` rename leftovers + ".NET 8" stale comment | `publish/EdgeSlider.*`, `obj/...EdgeSlider.AssemblyInfo.cs`, `build.bat:5` | Delete artifacts, fix comments |
| 3 | High | Orphaned duplicate UI, 444 lines dead | `settings.html` | Delete (live UI is `SettingsUI.html`) |
| 4 | Medium | WMI query rebuilt every tick | `BrightnessController.cs` (`SetScalar`) | Cache `ManagementObject` from `Probe()` |
| 5 | Low | Dead event, no subscriber | `StripGestureDetector.cs:97,124` | Consume in `TrayApp` or remove |
| 6 | Low | Per-pixel icon desaturation | `TrayApp.cs` (`CreateTrayIcon`) | Optional: `ColorMatrix` |
