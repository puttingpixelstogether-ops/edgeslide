# Terms of Service

**EdgeSlide — Amazing SAS**
Last updated: [FILL IN effective date]

EdgeSlide is a free Windows utility that turns touchpad edges into brightness and volume controls. Using it means you agree to these terms. They're short.

---

## What EdgeSlide is

A system-tray app for Windows. It reads your Precision Touchpad, adjusts brightness and volume, and gets out of your way. No account, no backend, no cloud. Everything runs on your machine.

---

## License — open source, MIT

EdgeSlide is open-source software, released under the **MIT License**:

> MIT License
>
> Copyright (c) 2026 Amazing SAS
>
> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

In plain language: use it, modify it, share it, build on it. Free, forever. Just keep the copyright notice in copies.

---

## Pay-what-you-want

The app is free. Full stop. If you want to support development, there's an optional donation. A few things to know:

- It's voluntary. Paying nothing gets you exactly the same software as paying something.
- It grants no extra features, priority support, or entitlements of any kind.
- Payments are non-refundable. They're a thank-you, not a purchase. There's nothing to return.
- Donations are processed by **Ko-fi** (ko-fi.com/puttingpixelstogether/tip), whose own terms and privacy policy govern the transaction. We don't handle payment details directly.

---

## No warranty

EdgeSlide is provided as-is, consistent with the MIT License above. We built it to work and intend to keep it working. But we make no guarantee that it will always be available, always be bug-free, or always behave exactly as expected on every combination of hardware, drivers, and Windows version in existence.

Specifically: the app reads touchpad input, modifies system brightness, and modifies system volume. Behavior depends on your hardware and drivers. Compatibility is limited to Windows Precision Touchpads on Windows 10 and 11. If it doesn't work on your setup, we're sorry. The GitHub issue tracker is the right place.

---

## Limitation of liability

To the maximum extent permitted by applicable law, Amazing SAS and the author (stillfalling) are not liable for any indirect, incidental, special, or consequential damages arising from your use of EdgeSlide — including but not limited to disruption to your workflow, unexpected changes to system brightness or volume, or hardware behavior you didn't anticipate.

You set the sliders. You run the app. Consequences of using it are yours.

**Note for EU/French users:** Under French and EU consumer law, liability for gross negligence, willful misconduct, and certain statutory consumer rights cannot be excluded. The limitation above applies only to the maximum extent the law allows. Nothing here removes your statutory rights.

---

## Hardware compatibility

EdgeSlide requires a **Windows Precision Touchpad**. It will not work on older HID-compliant touchpads or external mice. To check if your laptop qualifies: **Settings → Bluetooth & devices → Touchpad** — if you see gesture controls, you're good.

The app modifies display brightness via Windows WMI and audio volume via the Windows Core Audio API. Results depend on your display hardware and audio drivers. We can't guarantee compatibility with every monitor, docking station, or audio setup.

---

## Open-source attributions

EdgeSlide includes or depends on the following third-party components:

**NAudio** — Copyright (c) Mark Heath. MIT License.
Used for volume control via the Windows Core Audio API.
[github.com/naudio/NAudio](https://github.com/naudio/NAudio)

**.NET Runtime** — Copyright (c) Microsoft Corporation. MIT License.
Bundled in the standalone build.
[dotnet.microsoft.com](https://dotnet.microsoft.com)

**System.Management** — Copyright (c) Microsoft Corporation.
Used for display brightness via WMI.

**Microsoft Edge WebView2** — Copyright (c) Microsoft Corporation.
Used to render the local settings screen. Governed by Microsoft's terms and privacy policy.
[microsoft.com/en-us/edge/webview2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

Full license texts for MIT-licensed components are included in the application package.

---

## Changes

We may update these terms. When we do, the date at the top changes. Continuing to use EdgeSlide after a change means you accept the new terms. These terms describe the current version; if a future version adds paid features or network functionality, the relevant sections will be updated.

---

## Governing law

These terms are governed by French law. Any disputes go to the competent courts of France.

---

## Contact

Something feels wrong about these terms, or you found a bug: hello@edgeslide.app

EdgeSlide is made by stillfalling · Amazing SAS · France
