# EdgeSlide — Improvement Recommendations (for an AI assistant)

**Audience:** another Claude (or similar coding assistant) working in this repo, alongside
the project owner — who is **not an engineer**.

**How to use this document:**

- Everything below is a **recommendation to discuss**, not a task list to execute.
- Treat each item as a proposal. **Before doing any of it, explain it in plain language and
  ask the owner whether they want it done.** Some items also need a decision *from* the
  owner before any work can start — those are marked **Needs a decision**.
- Prefer one small, reviewable change per item. Work on a branch. Do a clean build after
  anything that touches the project file or moves files, and report the result.
- Nothing here is urgent or breaking. The project is already in good shape; these are polish
  and "make it easier for others to use and trust" suggestions.

---

## Context the owner may not have stated

- EdgeSlide is a small Windows tray app (C# / .NET / WinForms, ~2,400 lines) that turns the
  edge strips of a Precision Touchpad into brightness/volume sliders.
- It ships as a single downloadable `.exe` via GitHub Releases. The README, changelog,
  license, legal docs, issue templates, and packaging are already in place and good.
- The recommendations below were produced by reviewing the repo from the perspective of a
  developer (or cautious end user) discovering it for the first time.

---

## A. Repository weight

### A1. The demo GIF is large and lives in git history
- **Observation to share:** `Howitworks.gif` is ~18.7 MB and is committed. It dominates both
  the working tree (~37 MB) and the git history (~19 MB), so everyone who clones downloads it.
- **Recommendation:** consider compressing the GIF substantially, or converting it to MP4/WebM
  (typically ~10× smaller), and embedding that in the README.
- **Needs a decision:** if the owner also wants the large file removed from past history (not
  just going forward), that requires rewriting git history — a one-time, irreversible step that
  changes commit IDs. Recommend asking explicitly: *"Just shrink it going forward, or also
  scrub it from history?"*
- **Suggested questions for the owner:**
  - "Want me to compress the demo so the repo is lighter to download?"
  - "Should I also remove the old large copy from git history (irreversible), or leave history alone?"

---

## B. Trust and distribution (the app is an unsigned `.exe`)

### B1. Publish a checksum with each release
- **Why it may matter:** the download isn't code-signed, so cautious users get a SmartScreen
  warning. A published SHA-256 lets people verify the file is the real one.
- **Recommendation:** consider generating a SHA-256 for the released `.exe` and listing it on the
  Releases page / README.
- **Suggested question for the owner:** "Want me to add a checksum to each release so people can
  verify the download?"

### B2. Add a `SECURITY.md`
- **Why it may matter:** the app does low-level touchpad input hooking, so security-minded users
  look for a clear statement of what it does and doesn't do (no network, no telemetry, runs as a
  standard user, what files/registry keys it touches). Much of this is already in the README;
  `SECURITY.md` is where reviewers expect to find it.
- **Recommendation:** consider adding `SECURITY.md` summarizing those points and how to report a
  concern.
- **Suggested question for the owner:** "Want a short SECURITY file that spells out the app's
  privacy/permissions and how to report issues?"

---

## C. Automation (currently there is no CI)

### C1. Add a build/release workflow
- **Observation to share:** there is no `.github/workflows/` directory, so there's no automated
  proof the code builds, and releases are built and uploaded by hand.
- **Recommendation:** consider a GitHub Actions workflow that builds on each push/PR (and shows a
  status badge in the README) and, on a version tag, builds the release `.exe`, computes its
  checksum, and attaches it to a GitHub Release.
- **Suggested questions for the owner:**
  - "Want an automatic build check so the badge shows the code compiles?"
  - "Want releases to be built and published automatically when you tag a version?"

---

## D. Community / contribution files

### D1. `CONTRIBUTING.md`
- **Recommendation:** consider a short contributor guide (how to build, the one-concern-per-file
  layout, how to run). Most of this already exists in the README's "Build from source" section.
- **Suggested question for the owner:** "Want a brief CONTRIBUTING guide for anyone who wants to
  help?"

### D2. `CODE_OF_CONDUCT.md` (optional)
- **Recommendation:** optional drop-in if the owner wants the standard GitHub community files
  complete.
- **Suggested question for the owner:** "Do you want a standard code-of-conduct file, or skip it?"

---

## E. Tidying files that confuse first-time readers

### E1. AI-prompt files at the repo root
- **Observation to share:** `touchpad-slider-prompt.md` (root) and `docs/original-prompt.md`
  appear to be near-duplicate generation prompts. Having them — especially at the root — can read
  as unfinished to a newcomer.
- **Recommendation:** consider keeping a single copy under `docs/` (framed as "how this was
  built"), or removing them, per the owner's taste. Some owners like the transparency; some prefer
  a cleaner root.
- **Suggested question for the owner:** "Do you want to keep the original AI prompt in the repo as
  a 'how it was built' note, consolidate to one copy, or remove it?"

### E2. Duplicate code-review document
- **Observation to share:** `CODE_REVIEW.md` exists at the root (ignored by git but physically
  present) and is identical to the tracked `docs/CODE_REVIEW.md`. Editing the wrong one is easy.
- **Recommendation:** consider deleting the loose root copy, and deciding whether the internal
  review doc should be public at all.
- **Suggested question for the owner:** "Keep the code-review notes public under docs/, or remove
  them from the public repo?"

---

## F. .NET version

### F1. Pick one .NET version and state it consistently — Needs a decision
- **Observation to share:** the project file, README, and packaging scripts say **.NET 10**, but
  both prompt files still say **.NET 8**, and the code-review docs still describe the ".NET 8"
  wording as an open issue. So the repo currently tells the version story three different ways.
- **Recommendation:** recommend the owner choose a single target, then reconcile every mention:
  - **Option A — .NET 8 (LTS):** the version originally designed for; the most widely installed
    SDK, so it's the easiest for others to build. Nothing in the app needs a newer runtime.
  - **Option B — .NET 10:** also a supported LTS; keep it, but make the choice deliberate.
- **Suggested question for the owner:** "Which .NET version should this officially target — 8 or
  10? I'll make every file agree once you pick." (A gentle default suggestion: **.NET 8**, for the
  widest build compatibility — but it's the owner's call.)

### F2. Pin the SDK with a `global.json`
- **Why it may matter:** there's no `global.json`, so someone whose machine has a different .NET
  SDK gets a confusing build error instead of a clear "install version X" message.
- **Recommendation:** consider adding a `global.json` once F1 is decided.
- **Suggested question for the owner:** "Want me to add a small file that tells contributors
  exactly which .NET version to install?"

### F3. The project-file comment reads as a personal note
- **Observation to share:** a comment in `EdgeSlide.csproj` says the target is "the .NET 10 SDK
  *you already have installed*," which is phrased for the author's machine rather than for readers.
- **Recommendation:** consider rewording it to explain *why* the version was chosen.
- **Suggested question for the owner:** "Want me to reword that comment so it makes sense to anyone
  reading the project?"

---

## G. File layout / organization — Needs a decision

### G1. Source and scripts are loose in the repo root
- **Observation to share:** there are ~30 entries in the root, including 14 loose `.cs` files and
  4 build/uninstall scripts. Developers usually expect source under `src/` and scripts grouped
  together.
- **Recommendation:** consider reorganizing into something like:
  - `src/` — the `.cs` files, the `.csproj`, `app.manifest`, `SettingsUI.html`
  - `scripts/` — the build and uninstall scripts
  - `assets/` — icons (see G2)
  - `docs/`, `packaging/`, and the README/CHANGELOG/LICENSE staying where they are
- **Important caution to relay:** this is not a free move. The project file's relative paths and
  every build script's path to the project must be updated together, followed by a clean build to
  confirm nothing broke. Recommend doing it as **one coherent change on a branch**, not a loose
  file move, and only with the owner's go-ahead.
- **Suggested question for the owner:** "Want me to reorganize the files into clear folders
  (src/, scripts/, assets/)? It's safe but touches the build setup, so I'd do it carefully on a
  branch and test the build."

### G2. Icons live in three places
- **Observation to share:** icon files exist at the root (`icon.png`, `icon.svg`), under
  `assets/icons/`, and under `packaging/`. The project file even points its app icon and its
  embedded icon at two different folders.
- **Recommendation:** consider giving icons a single home (e.g. `assets/`) and updating the
  project file to reference that one location.
- **Suggested question for the owner:** "Want me to put all the icons in one folder and point the
  project at that single copy?"

---

## Suggested order (only after the owner approves each)

1. Decide the .NET version (F1) and the file-layout question (G1) — these unlock the rest.
2. Quick, low-risk wins: `SECURITY.md` (B2), checksum (B1), `CONTRIBUTING.md` (D1),
   reconcile version text (F1/F3), `global.json` (F2), tidy duplicate/prompt files (E1, E2).
3. CI/release workflow (C1).
4. Icon consolidation (G2) and, if approved, the `src/`/`scripts/` reorganization (G1).
5. GIF compression (A1); history rewrite only if the owner explicitly opts in.

**Reminder:** confirm each item with the owner before doing it, and report back after each change
with what was done and the build result.
