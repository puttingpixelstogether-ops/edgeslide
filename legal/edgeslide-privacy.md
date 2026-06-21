# Privacy Policy

**EdgeSlide — Amazing SAS**
Last updated: [FILL IN effective date]

Short version: EdgeSlide doesn't know who you are, doesn't want to, and has no way to find out. Everything the app does happens on your machine. Nothing leaves it. This page explains that in slightly more words, because we're required to.

---

## What EdgeSlide is

A Windows tray utility. It reads your touchpad edges and adjusts brightness and volume. That's the whole product. No account, no cloud, no servers. It runs on your PC and stays there.

---

## What the app accesses — and why

### Touchpad input

EdgeSlide reads your Precision Touchpad via the Windows Raw Input API. It gets finger position (X/Y coordinates) and contact state in real time — enough to know when you're sliding on an edge and how fast. This data is processed in memory, in the moment, to move a slider. It is never stored, never transmitted, never used for anything else.

The app can also read the touchpad's hardware identifiers (vendor ID, product ID) and physical dimensions to calibrate itself. Same rules: local only, not sent anywhere.

### Keyboard activity — timing only

This is the one that looks scarier than it is, so we'll be direct about it.

EdgeSlide installs a Windows low-level keyboard hook. It uses this hook **solely to record the timestamp of your most recent key press** — nothing else. Not which key. Not what you typed. Not key codes, sequences, or any content whatsoever. Just: "a key was pressed at this time."

The reason: if you're typing, EdgeSlide ignores edge touches so you don't accidentally change your brightness mid-sentence. That's it. The timestamp is held in memory and discarded. It is never written to disk, never transmitted, never associated with anything you typed.

If you're security-minded, the code is on GitHub. Read it.

### Display brightness and system volume

When you slide, EdgeSlide sets your screen brightness via the Windows WMI brightness API and your master volume via the Windows Core Audio API. It writes these values to your system. It does not log what they were or what you changed them to.

---

## What the app stores locally

Everything below lives on your machine, in your user profile. None of it is transmitted anywhere.

**Settings** — `%APPDATA%\EdgeSlide\settings.json`
Your preferences: strip width, which edge does what, invert direction, debounce timing, launch-at-startup. No personal data.

**Log file** — `%APPDATA%\EdgeSlide\edgeslide.log`
Operational diagnostics: when the app started and stopped, which touchpad was detected (including hardware vendor/product IDs and coordinate range), and gesture events. Auto-trimmed at 512 KB. No typed content. No personal documents. No key data.

**Verbose diagnostics (optional, off by default)** — enabled only if you manually create a `diagnostics.on` file in the config folder. When active, the log additionally records raw finger X/Y coordinates, useful for troubleshooting. You turn this on; you turn this off. We don't control it.

**WebView2 working files** — `%APPDATA%\EdgeSlide\WebView2\`
Cache and working files for the embedded settings screen (local HTML, no remote content). See the third-party section below.

**Startup registry entry (optional)** — if you enable "Launch at Windows startup," the app writes a value to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Disabled when you turn the option off; nothing lingers.

### How to delete all of it

Delete `%APPDATA%\EdgeSlide`. That removes settings, logs, and WebView2 data. Uninstalling the app removes the application itself. You can do both independently.

---

## What the app does not do

Let's be explicit:

- No data is sent off your device. No network requests. No servers. No APIs.
- No analytics, telemetry, crash reporting, or usage tracking of any kind.
- No user accounts, sign-in, email addresses, or profiles.
- No advertising. No data sold, shared, or rented to third parties. There's no data to sell.
- No collection of files, documents, browser history, or personal content.
- No keystroke logging. See the keyboard hook section above.

---

## Support and log files

If you contact us for support and choose to share your log file, that file may contain your touchpad's hardware identifiers and coordinate data. It will not contain personal content or key data. Sending it is always your choice. We use it only to diagnose the issue you reported and don't retain it beyond that.

---

## Third-party components

EdgeSlide bundles or depends on the following:

**Microsoft Edge WebView2** — used to render the local settings screen (which is local HTML; it loads nothing from the internet). WebView2 is a Microsoft component. Its behavior — including any data handling by the Edge runtime itself — is governed by [Microsoft's privacy policy](https://privacy.microsoft.com/en-us/privacystatement), not ours. We have no control over it and make no representations about it.

**NAudio** (MIT License) — open-source audio library used for volume control. No data handling.

**.NET Runtime** (Microsoft) — bundled in the standalone build. Governed by Microsoft's terms.

**System.Management** (Microsoft) — used for the brightness API. No data handling beyond what's described above.

---

## Children

EdgeSlide is a brightness and volume controller. It is not directed at children and does not knowingly collect data from anyone — children or otherwise. There's no data collection to speak of.

---

## GDPR — your rights

The data controller for EdgeSlide is:

**Amazing SAS**
France
hello@edgeslide.app

Because EdgeSlide collects no personal data about you and transmits nothing off your device, most of the usual GDPR machinery doesn't apply in practice. There's nothing to access, correct, export, or delete on our end — we genuinely don't have it.

That said: you have the right to lodge a complaint with the French data protection authority, the **CNIL** (Commission Nationale de l'Informatique et des Libertés), if you believe your data rights are being violated. [www.cnil.fr](https://www.cnil.fr)

The local data stored on your own machine (settings, logs) is yours. Delete it whenever you want, as described above.

---

## Changes

If something material changes — for example, if a future version adds any network feature — we'll update this page and the date at the top. These documents describe the current, network-free version. If you're reading a version that has analytics or cloud features, check the updated policy.

---

## Contact

Questions about this policy: hello@edgeslide.app
EdgeSlide is made by stillfalling · Amazing SAS · France
