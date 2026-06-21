# Packaging EdgeSlide

Two ways to distribute the app:

| Goal | Use | Output | Extra tool needed |
|---|---|---|---|
| Microsoft Store | `msix\build-msix.bat` | `EdgeSlide.msix` | Windows SDK (`makeappx`) |
| Direct download / share | `nsis\build-nsis.bat` | `EdgeSlide-Setup.exe` | NSIS (`makensis`) |

Both build scripts publish the app **self-contained** (the .NET runtime is bundled),
so the people you give it to don't need to install anything.

Shared assets (`EdgeSlide.ico` and the MSIX tile images under `msix\Assets`) are
generated from the same touchpad-with-edge-strips icon used in the tray.

---

## MSIX (Microsoft Store)

### Build
1. Install the **Windows SDK** (or run from a *Developer Command Prompt for VS*, which
   puts `makeappx.exe` on PATH).
2. Run `msix\build-msix.bat`. It publishes the app, lays the manifest + assets on top,
   and packs `EdgeSlide.msix`.

### Submit to the Store
1. In **Partner Center**, reserve the app name and create the submission.
2. Copy the **Package Identity** values (Name + Publisher) it gives you into
   `msix\Package.appxmanifest` (the `<Identity>` element), then rebuild.
3. Upload `EdgeSlide.msix`. **The Store signs it for you** — you do *not* need to buy
   a code-signing certificate for this path.

### Test locally before submitting (sideload)
The Store signs Store packages, but to install the `.msix` yourself first you must sign
it with a certificate whose subject matches the manifest `Publisher` (`CN=EdgeSlideDev`):

```powershell
# one-time: create a self-signed cert
New-SelfSignedCertificate -Type Custom -Subject "CN=EdgeSlideDev" `
  -KeyUsage DigitalSignature -FriendlyName "EdgeSlideDev" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3","2.5.29.19={text}")

# export it (replace <THUMBPRINT> from the command above), set a password
$pwd = ConvertTo-SecureString -String "test1234" -Force -AsPlainText
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\<THUMBPRINT>" -FilePath EdgeSlideDev.pfx -Password $pwd

# sign the package (signtool ships with the Windows SDK)
signtool sign /fd SHA256 /a /f EdgeSlideDev.pfx /p test1234 msix\EdgeSlide.msix
```

Then install the cert into **Trusted People** (double-click the .pfx → Local/Current user
→ Trusted People), and double-click `EdgeSlide.msix` to install.

---

## NSIS (direct-download installer)

### Build
1. Install **NSIS** from https://nsis.sourceforge.io
2. Run `nsis\build-nsis.bat`. It publishes the app and compiles
   `EdgeSlide-Setup.exe`.

`EdgeSlide-Setup.exe` is a single file you can hand to anyone. It installs **per-user**
(no admin prompt) into `%LOCALAPPDATA%\Programs\EdgeSlide`, adds a Start Menu shortcut
and an Add/Remove Programs entry, and offers to launch on finish.

### SmartScreen
Because the installer is unsigned, Windows SmartScreen will show
"Windows protected your PC" the first time someone runs it. They can click
**More info → Run anyway**. To remove that prompt you'd need an **OV/EV code-signing
certificate** (a paid product from a CA) and sign `EdgeSlide-Setup.exe` and
`EdgeSlide.exe` with `signtool`. This is optional and only affects the first-run
warning, not whether the app works.

---

## Licensing note

These scripts rely only on free tooling:

- **Windows SDK** — free.
- **NSIS** — free (zlib/libpng license), including commercial use.

(Inno Setup was considered but, as of v6.5.0, it asks commercial users to buy a license,
so it isn't used here.)
