# Changelog

All notable changes to EdgeSlide are documented here. This project follows
[Semantic Versioning](https://semver.org) and the spirit of
[Keep a Changelog](https://keepachangelog.com).

## [0.2.1] - 2026-06-25

### Fixed
- Volume now follows the current default playback device. Previously, connecting Bluetooth
  headphones (or otherwise switching output) after EdgeSlide had started left it adjusting
  the old device; it now re-checks the default output at the start of each volume slide.
- The Settings window now opens tall enough to show every control (it could clip near the
  bottom on some displays).

### Internal
- Volume changes are de-duplicated (only real level changes are sent to the audio device).

## [0.2.0] - 2026-06-25

### Added
- Per-strip slider mode, set independently for each edge in Settings:
  - **Relative** (new default): the value adjusts up or down from its current level by how
    far you slide, so where you first touch doesn't matter — starting low no longer snaps
    the value to 0.
  - **Absolute**: the original behaviour, where the value jumps to your finger's position
    (top = max, bottom = min).
- Separate "Brightness overlay" and "Volume overlay" switches in Settings → Behaviour, so
  EdgeSlide's on-screen slider can be turned off per control — useful when Windows already
  shows its own indicator for one of them (some systems show a native volume OSD).
- A script to build an ARM64 portable exe (`build-portable-arm64.bat`) for anyone running
  Windows on ARM who wants to build it themselves.

## [0.1.1] - 2026-06-24

### Changed
- Brightness no longer re-queries WMI on every update during a slide, so brightness
  changes are smoother and lighter on the system.
- The on-screen HUD now hides the instant you lift your finger, instead of lingering for
  the auto-hide timeout.

### Fixed
- Settings window looked cramped or clipped (e.g. "Left edge", dropdowns showing just "B")
  on high-DPI laptops. The window now sizes itself correctly for the display's scaling, and
  the layout is responsive — columns grow to fill the width and reflow (3 → 2 → 1) instead
  of cramming, so nothing clips at any size.
- Suppressed a harmless `WindowsBase` (MSB3277) build warning that the WebView2 package
  produces in a WinForms app.

### Internal
- Build scripts close any running EdgeSlide before building (so files aren't locked), and
  now have a friendlier, colored CLI.
- Removed an orphaned duplicate settings file; synced the in-repo legal docs with the
  published website versions.

## [0.1.0] - 2026-06

First public release.

- Turn the left and right edges of a Windows Precision Touchpad into sliders:
  left edge = brightness, right edge = volume (both reassignable, or disable a side).
- Top of the touchpad = maximum, bottom = minimum, with an optional invert toggle.
- Single-finger only: two-finger scrolling and other gestures pass through untouched.
- Ignores edge touches while you're typing.
- Configurable strip width and activation hold; dark settings panel.
- Launch-at-startup option. Lives in the system tray with no main window.
