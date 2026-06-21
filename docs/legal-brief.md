# EdgeSlide — Legal & Privacy Brief (for the copywriter)

This is a factual brief describing exactly what EdgeSlide does, accesses, stores, and
transmits, so you can write an accurate Privacy Policy and Terms of Service / EULA.
**Do not embellish or soften the technical facts** — accuracy matters here because two
features (a keyboard hook and raw touchpad input) are things app-store reviewers and
privacy-conscious users look at closely. Where something is a placeholder the developer
must supply, it's marked `[FILL IN]`.

---

## 1. What the product is

EdgeSlide is a small Windows desktop utility (system-tray app). It turns the left and
right edge strips of a Windows Precision Touchpad into sliders: sliding on the left edge
adjusts screen brightness, sliding on the right edge adjusts master volume. It has no
account system, no cloud component, and runs entirely on the user's PC.

**The single most important message for both documents: EdgeSlide is privacy-by-design.
Everything happens locally on the device. The app itself sends no data anywhere — no
servers, no analytics, no telemetry, no accounts.**

---

## 2. Data the app accesses, and why (the inventory)

For each item: what it is, why it's needed, where it goes. None of this is transmitted
off the device.

- **Touchpad input (raw HID data).** The app reads the Precision Touchpad via Windows
  Raw Input to get finger positions (X/Y coordinates) and contact state in real time.
  This is the core function. It is processed in memory moment-to-moment to detect edge
  slides; it is **not** stored or sent anywhere (except optional local diagnostics — see
  §3). The app can also see the touchpad's hardware identifiers (vendor/product ID) and
  physical dimensions.

- **Keyboard activity — timing only (IMPORTANT, see §4).** The app installs a Windows
  low-level keyboard hook. It uses this **solely to record the timestamp of the most
  recent key press**, so it can ignore accidental edge touches while the user is typing.
  It does **not** record, store, log, or transmit which keys are pressed, key codes, or
  any typed content.

- **Display brightness.** The app sets screen brightness through the Windows WMI
  brightness API when the user slides the brightness strip.

- **System volume.** The app sets the default audio output device's master volume
  through the Windows Core Audio API when the user slides the volume strip.

- **Local settings & logs.** See §3.

---

## 3. What the app stores, where, and for how long

All local, all on the user's machine, in the user's own profile folders:

- **Settings** — `%APPDATA%\EdgeSlide\settings.json`. Stores user preferences only
  (strip width, which strip does what, invert direction, debounce, launch-at-startup).
  No personal data.
- **Log file** — `%APPDATA%\EdgeSlide\edgeslide.log`. Operational diagnostics:
  app start/stop, detected touchpad info (including hardware vendor/product IDs and the
  touchpad's coordinate range), and gesture events. Auto-trimmed at ~512 KB. No typed
  content, no personal documents.
- **Optional verbose diagnostics** — if the user creates a `diagnostics.on` file in the
  config folder, the log additionally records raw finger X/Y coordinates (used only for
  troubleshooting). Off by default; user-initiated.
- **WebView2 data** — `%APPDATA%\EdgeSlide\WebView2\`. Cache/working files for the
  embedded settings screen (which is local HTML; see §6).
- **Startup entry (optional)** — if the user enables "Launch at Windows startup," the
  app adds a registry value under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Removed when the user turns the
  option off.

Retention: all files persist until the user deletes them or uninstalls. The brief for
the writer: describe how a user can view/delete this data (delete the `%APPDATA%\EdgeSlide`
folder; uninstall removes the app).

**Support note to cover in the policy:** if a user voluntarily sends their log file to
the developer for support, that file may contain hardware identifiers and touchpad
coordinate data (not personal content). Sending it is always the user's choice.

---

## 4. The keyboard hook — handle this explicitly and carefully

This deserves its own paragraph in the Privacy Policy because it will otherwise read as
a red flag. The copy should:

- State plainly that EdgeSlide uses a system keyboard hook.
- Be equally plain that it captures **only the time of the last keystroke**, never the
  keys themselves, and that nothing about typing is stored or transmitted.
- Explain the legitimate purpose: to suppress accidental edge-slider activations while
  the user is typing.
- Avoid weasel words. Direct honesty here builds trust; vagueness destroys it.

App-store reviewers (Microsoft Store especially) may flag input hooks — the listing and
privacy policy must disclose this clearly or risk certification issues.

---

## 5. What the app does NOT do (state these affirmatively)

- No data is sent off the device by the app. No network requests, no servers, no APIs.
- No analytics, telemetry, crash reporting, or usage tracking.
- No user accounts, sign-in, email collection, or profiles.
- No advertising, no data sold or shared with third parties.
- No collection of files, browsing data, or personal content.
- No keystroke logging (see §4).

---

## 6. Permissions, system access, and third-party components

- **Privilege level:** runs as a standard user; never requests admin/elevation. (MSIX
  build declares the `runFullTrust` capability, standard for a Win32 desktop app.)
- **Third-party / bundled components** (relevant for the EULA's attribution and the
  privacy policy's "third parties" section):
  - **.NET runtime** (Microsoft) — bundled in the standalone build.
  - **NAudio** — open-source audio library (MIT license) for volume control.
  - **System.Management** (Microsoft) — for the brightness API.
  - **Microsoft Edge WebView2** — Microsoft component used to render the local settings
    screen. The settings page is local HTML embedded in the app (no remote content).
    However, the WebView2 / Edge runtime is a Microsoft product with its own update and
    data-handling behavior governed by Microsoft's terms — the policy should note that
    this third-party runtime is subject to Microsoft's privacy terms, not the
    developer's.
- The EULA should include open-source attributions/licenses for the above.

---

## 7. Documents to produce & what each must cover

### A) Privacy Policy
Required for the Microsoft Store listing even though little/no data is collected.
Sections to include:
1. Plain-language summary: "EdgeSlide does not collect or transmit your data; everything
   stays on your device."
2. What the app accesses locally and why (§2) — including the keyboard-hook disclosure (§4).
3. What's stored locally and where (§3); how to view/delete it.
4. No telemetry / no third-party sharing / no sale of data (§5).
5. Third-party components and that WebView2/Edge follows Microsoft's terms (§6).
6. Children: not directed at children; collects no personal data (see §8).
7. **Data controller (GDPR):** Amazing SAS (France) — identify it as the controller and
   give the contact. Because no personal data is collected or transmitted, the
   data-subject-rights section can be brief, but still name the controller, the contact,
   and the right to lodge a complaint with the French DPA (CNIL).
8. Changes to the policy + effective date.
9. Contact: hello@edgeslide.app (Amazing SAS, France).

### B) Terms of Service / EULA
The software itself is **open source under the MIT License**, so the MIT `LICENSE` file
in the repository is the authoritative software license (permissive: use, modify,
redistribute freely, "AS IS," no warranty). The Terms document therefore does **not**
need a restrictive proprietary license grant — instead it should:
1. State that the software is provided under the MIT License and point to it; the app is
   free to use.
2. **Pay-what-you-want / donations:** payments are entirely **voluntary**, are not
   required to use the software, grant no extra features or entitlements, and are
   **non-refundable** (they're support, not a purchase). Mention the payment is processed
   by `[FILL IN payment provider once chosen, e.g. Stripe / Ko-fi]`, whose own terms and
   privacy policy apply to the transaction.
3. **Disclaimer of warranty** — "as is," no warranty of fitness (consistent with MIT),
   especially because the app reads input and changes system settings (brightness/volume).
4. **Limitation of liability** — not liable for incidental/consequential damages, e.g.
   if brightness/volume behavior interferes with the user's workflow or hardware. Note:
   under French/EU consumer law, liability for gross negligence/willful misconduct and
   certain statutory rights cannot be excluded — the writer should phrase the cap so it
   applies "to the maximum extent permitted by law."
5. **Hardware/behavior disclaimer** — the app controls display brightness and audio
   volume and intercepts touchpad input; behavior depends on the user's hardware and
   drivers; compatibility limited to Windows Precision Touchpads.
6. Third-party/open-source license attributions (§6).
7. Governing law: **France**; changes to terms; contact: **hello@edgeslide.app**.

### C) Store listing short privacy summary
A 2–3 sentence version of the Privacy Policy for the Microsoft Store "privacy" field.

---

## 8. Developer-supplied details (confirmed)

- **Company / data controller:** Amazing SAS (France)
- **Developer / author:** stillfalling
- **Contact:** hello@edgeslide.app
- **Website / policy hosting:** edgeslide.app
- **Licensing model:** Free to use · Open source under the **MIT License** · Pay-what-you-want (voluntary donations)
- **Governing law:** France (EU; GDPR applies — see §9)
- **Copyright line for MIT `LICENSE` file:** `Copyright (c) 2026 Amazing SAS`

Still to decide:
- Payment/donation provider (for the donations clause): `[FILL IN]`
- Effective date of the published policies: `[FILL IN]`

---

## 9. Tone & compliance notes for the writer

- Lead with the privacy-positive story; it's genuinely strong (local-only, no telemetry).
- Be scrupulously accurate and direct about the keyboard hook and the touchpad/hardware
  data — under-disclosure risks Store rejection and user distrust; over-claiming (e.g.
  implying encryption or cloud features that don't exist) is also wrong. There is no
  network transmission to describe, so don't invent data-handling/transfer language.
- Keep GDPR framing simple but present (Amazing SAS is an EU/French company, so GDPR
  applies): name Amazing SAS as data controller, give the hello@edgeslide.app contact,
  mention the right to complain to the CNIL (French DPA). Because no personal data is
  collected or transmitted, the rest of the data-subject-rights machinery is largely
  moot — but still tell users how to delete local data. (No US presence stated, so CCPA
  framing is optional; the writer can add a short note if targeting US users.)
- If analytics or any network feature is ever added later, both documents must be
  revised; note that these documents describe the current, network-free version.
