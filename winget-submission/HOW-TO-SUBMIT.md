# Submitting EdgeSlide to winget

The three manifest files are ready under:

    winget-submission/manifests/p/PuttingPixelsTogether/EdgeSlide/0.1.1/

They point at the live, public v0.1.1 portable exe and use GitHub's own SHA-256 for that
file. You just need to get them into Microsoft's `winget-pkgs` repo as a pull request.

There are two ways. Pick one.

---

## Option A — wingetcreate (easiest, does the PR for you)

Microsoft's tool computes the hash, builds the manifests, and opens the PR.

1. Install it (once):

       winget install Microsoft.WingetCreate

2. Generate + submit from the release URL:

       wingetcreate new https://github.com/puttingpixelstogether-ops/edgeslide/releases/download/v0.1.1/EdgeSlide-Portable.exe

   Answer the prompts (use the values in the manifests here:
   PackageIdentifier `PuttingPixelsTogether.EdgeSlide`, version `0.1.1`, type `portable`,
   publisher `PuttingPixelsTogether`, etc.). When it asks to submit, say yes — it will ask
   for a GitHub Personal Access Token (classic, with `public_repo` scope). It then forks
   `winget-pkgs`, commits the manifests, and opens the PR.

You can skip the prompts entirely by passing the ready files:

       wingetcreate submit --token <YOUR_GITHUB_PAT> "winget-submission/manifests/p/PuttingPixelsTogether/EdgeSlide/0.1.1"

---

## Option B — manual pull request

1. Fork https://github.com/microsoft/winget-pkgs on GitHub.
2. Copy the whole `manifests/...` tree from this folder into your fork (same path):
   `manifests/p/PuttingPixelsTogether/EdgeSlide/0.1.1/` with the three `.yaml` files.
3. (Optional but recommended) validate locally first:

       winget validate --manifest manifests/p/PuttingPixelsTogether/EdgeSlide/0.1.1

4. Commit, push, and open a PR against `microsoft/winget-pkgs` `master`.
   Title convention: `New package: PuttingPixelsTogether.EdgeSlide version 0.1.1`.

---

## What happens after you submit

- A bot validates the manifest and tries a sandbox install. Watch the PR for labels.
- If it adds **Needs-Author-Feedback**, read the comment and reply / push a fix.
- A moderator reviews and merges. This usually takes a few hours to a few days.
- Once merged, anyone can run:

       winget install PuttingPixelsTogether.EdgeSlide

  (or `winget install edgeslide`, thanks to the Moniker).

## Notes / things reviewers may mention

- **Unsigned exe:** fine for winget; the SmartScreen note is expected and not a blocker.
- **Portable type:** EdgeSlide is a tray GUI app, so winget won't add a Start Menu
  shortcut — it registers the exe and a `edgeslide` command alias, and tracks it so
  `winget uninstall` works. If you'd rather have a Start Menu entry, switch to the NSIS
  installer later (`InstallerType: nullsoft`) in a future version's manifest.
- **Future updates:** for the next version, `wingetcreate update PuttingPixelsTogether.EdgeSlide`
  bumps the version + URL + hash and opens the PR automatically.

## Prerequisites (already met)

- [x] Repo public
- [x] v0.1.1 published, not a draft/prerelease
- [x] Installer at a permanent versioned URL (`/releases/download/v0.1.1/...`)
- [x] SHA-256 known: `D103D8498157DAA1FC23F9F8C6D2614C7B30DF2DA943A7F1BB93F897150CD37D`
