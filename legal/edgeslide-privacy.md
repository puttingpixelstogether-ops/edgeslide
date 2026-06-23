# Privacy Policy

**EdgeSlide · Amazing SAS**
Last updated: June 2026

## What EdgeSlide is

A Windows tray utility. It reads your touchpad edges and adjusts brightness and volume. No account, no cloud, no servers. It runs on your PC and stays there.

## What the app accesses—and why

### Touchpad input

EdgeSlide reads your Precision Touchpad via the Windows Raw Input API. It gets finger position (X/Y coordinates) and contact state in real time—enough to know when you're sliding on an edge and how fast. This data is processed in memory, in the moment, to move a slider. It is never stored, never transmitted, never used for anything else.

The app can also read the touchpad's hardware identifiers (vendor ID, product ID) and physical dimensions to calibrate itself. Same rules: local only, not sent anywhere.

### Keyboard activity—timing only

This is the one that looks scarier than it is, so we'll be direct about it.

EdgeSlide installs a Windows low-level keyboard hook. It uses this hook **solely to record the timestamp of your most recent key press**—nothing else. Not which key. Not what you typed. Not key codes, sequences, or any content whatsoever. Just: "a key was pressed at this time."

The reason: if you're typing, EdgeSlide ignores edge touches so you don't accidentally change your brightness mid-sentence. That's it. The timestamp is held in memory and discarded. It is never written to disk, never transmitted, never associated with anything you typed.

If you're security-minded, the code is on [GitHub](https://github.com/puttingpixelstogether-ops/edgeslide). Read it.

### Display brightness and system volume

When you slide, EdgeSlide sets your screen brightness via the Windows WMI brightness API and your master volume via the Windows Core Audio API. It writes these values to your system. It does not log what they were or what you changed them to.

## What the app stores locally

Everything below lives on your machine, in your user profile. None of it is transmitted anywhere.

- **Settings**—`%APPDATA%\EdgeSlide\settings.json`—your preferences: strip width, which edge does what, invert direction, debounce timing, launch-at-startup. No personal data.
- **Log file**—`%APPDATA%\EdgeSlide\edgeslide.log`—operational diagnostics: start/stop times, touchpad detected, gesture events. Auto-trimmed at 512 KB. No typed content, no key data.
- **Verbose diagnostics (off by default)**—enabled only if you manually create a `diagnostics.on` file in the config folder. When active, raw X/Y coordinates are additionally logged. You control this.
- **WebView2 working files**—`%APPDATA%\EdgeSlide\WebView2\`—cache for the embedded settings screen (local HTML, no remote content).
- **Startup registry entry (optional)**—if you enable launch at startup, the app writes to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Removed when you disable the option.

To delete everything: remove `%APPDATA%\EdgeSlide`. That clears settings, logs, and WebView2 data. Uninstalling removes the app itself. You can do both independently.

## What the app does not do

- No data is sent off your device. No network requests, no servers, no APIs.
- No analytics, telemetry, crash reporting, or usage tracking of any kind.
- No user accounts, sign-in, email addresses, or profiles.
- No advertising. No data sold, shared, or rented to third parties. There's no data to sell.
- No collection of files, documents, browser history, or personal content.
- No keystroke logging. See the keyboard hook section above.

## Support and log files

If you contact us for support and choose to share your log file, that file may contain your touchpad's hardware identifiers and coordinate data. It will not contain personal content or key data. Sending it is always your choice. We use it only to diagnose the issue you reported and don't retain it beyond that.

## Third-party components

- **Microsoft Edge WebView2**—renders the local settings screen (local HTML; loads nothing from the internet). Governed by [Microsoft's privacy policy](https://privacy.microsoft.com/en-us/privacystatement).
- **NAudio** (MIT)—open-source audio library for volume control. No data handling.
- **.NET Runtime** (Microsoft)—bundled in the standalone build. Governed by Microsoft's terms.
- **System.Management** (Microsoft)—used for the brightness API.

## Children

EdgeSlide is a brightness and volume controller. It is not directed at children and does not knowingly collect data from anyone. There's no data collection to speak of.

## GDPR

The data controller is:

**Amazing SAS** · 32 rue Greffulhe · 92300 Levallois-Perret, France · SIRET: 88486627800010 · hello@edgeslide.app

Because EdgeSlide collects no personal data and transmits nothing off your device, most of the usual GDPR machinery doesn't apply in practice. There's nothing to access, correct, export, or delete on our end—we genuinely don't have it.

You have the right to lodge a complaint with the French data protection authority, the [CNIL](https://www.cnil.fr), if you believe your data rights are being violated.

## Changes

If something material changes—for example, if a future version adds any network feature—we'll update this page and the date at the top. If you're reading a version that has analytics or cloud features, check the updated policy.

## Contact

hello@edgeslide.app
EdgeSlide is made by PuttingPixelsTogether · Amazing SAS · France
