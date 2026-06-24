# Changelog

All notable changes to EdgeSlide are documented here. This project follows
[Semantic Versioning](https://semver.org) and the spirit of
[Keep a Changelog](https://keepachangelog.com).

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
