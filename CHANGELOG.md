# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

_Nothing yet._

## [3.0.5] — 2026-06-04

A large correctness, safety, and security release from a full-codebase deep audit (parallel reviewers across the install engine, the credential/network paths, and the WinForms/async surface) layered on top of the v3.0.4 test campaign. Most fixes matter on the unhappy paths — a locked file, a running instance, a corrupt manifest, a cold token — and several are leaks/races that manual testing can't surface. No behavioral change to a healthy install.

### Fixed
- **A locked translation file can no longer break a reinstall/uninstall.** Before deleting anything, the installer now checks that every tracked file is unlocked and writable; if the game is open, or antivirus / Windows Search / a cloud-sync app / an editor is holding a file, it aborts cleanly and leaves your install 100% intact (with a "close X and retry" message) instead of half-removing then failing.
- **The installer/uninstaller now detects a running instance** and offers to close it first, instead of failing to replace or remove the locked program.
- **Wrapped launcher shortcuts have a clean lifecycle** — removing one deletes the Desktop "… (TL update)" copy; un-wrapping restores the original working directory; and **uninstalling the program first un-wraps your managed shortcuts** so they revert to plain game-launchers instead of pointing at a deleted exe. Shortcuts that were moved or deleted are flagged "(missing)".
- **Import / Export carries only portable preferences** (translation source, ignore list, toggles) — not machine-specific shortcut paths or internal migration state — and rejects a non-settings file with a clear message.
- **Normal progress text is no longer shown in alarming red** (red is reserved for real errors); operational steps read neutral.
- **Every window now has a title** (visible in Task Manager / Alt-Tab) and the UI text capitalization is consistent.
- **The AutoUpdater window now shows the active translation source** (e.g. "TL Patch Versions (TH)"), and its pre-launch cancel window is a bit longer.
- **New: Settings → "Reclaim Space in Game Folder"** frees disk by removing BepInEx's regenerable files (its log + assembly cache) and pruning leftover empty folders — your translation patch, settings, and any user files are never touched.
- **A corrupt install manifest is now backed up and rebuilt** instead of silently dropping another installed language's tracking; old install-folder logs from pre-3.0.2 versions are swept on uninstall.
- **Crash-safety, resource-leak, and UI-responsiveness fixes** from the audit: guarded background handlers (no silent process exit), released COM/font/event-handler resources, and moved token validation + a couple of operations off the UI thread.

### Security
- The optional GitHub token is **no longer loaded into a UI text field**, is **no longer attached to release-asset downloads** (closing a path where a redirect could forward it off-GitHub), and is **validated off the UI thread**.
- Settings import is hardened against **XXE**; error dialogs no longer print stack traces.
- The honest-disclosure docs now acknowledge the inherited UI art as a known gap to re-skin (it was previously, incorrectly, described as containing no game-derived assets).

### Changed
- The installer now shows a **license + acknowledgments page** during setup (MIT + credits to the upstream and the patch authors). The root `LICENSE.txt` stays pure MIT.
- Internally, the install manifest is now written **after** a successful removal, so a locked-file abort can never leave it inconsistent.

## [3.0.4] — 2026-06-03

Security + a small UI polish. No behavioral change to a healthy install.

### Security
- **Zip extraction writes to the path it just validated** — the zip-slip guard already confined every entry to the game folder; extraction now writes to that same guard-checked path instead of re-deriving it from the (untrusted) entry name. Resolves the CodeQL `cs/zipslip` alert and removes a "check-one-path, write-another" pattern.

### Changed
- **Language labels finalized** — the compact "TL Source" label shows the ISO code (**EN** / **TH**); the source picker shows each language's native full name (**English** / **ไทย**). Internal manifest keys are unchanged, so existing installs are unaffected.

## [3.0.3] — 2026-06-03

Security + robustness hardening. No behavioral change to a healthy install.

### Fixed
- **Wrapping a launcher shortcut is no longer silent and works for protected folders** — the action always reports its result, and when a shortcut lives in a write-protected folder (e.g. the All-Users Start Menu, `C:\ProgramData\…`) it now wraps a copy on your Desktop ("… (TL update)") instead of failing quietly.
- **Self-update can no longer crash the app** — the installer-update path is guarded against unhandled exceptions.

### Security
- The validated GitHub token is cached by **SHA-256 hash**, never kept as plaintext in a long-lived field.
- Diagnostic logging no longer uses `Console.WriteLine` (which bypassed the log redactor); debug traces are compiled out of release builds.
- **CodeQL code scanning** added (C#) — static security analysis on every push/PR, complementing Dependabot.

## [3.0.2] — 2026-06-03

A hardening release from a full-project security + correctness review. No behavioral change to a
healthy install — these fixes matter on the unhappy paths (multi-language, a wrong clock, interrupted state).

### Fixed
- **"Remove Ignored Patch Files" is now per-language** — uninstalling one source's ignored data no longer deletes another installed language's user-edited XUnity files (uninstalling English no longer touches ไทย's `_Substitutions.txt` / pre/post-processors).
- **Runtime logs write to the app data folder** (`%LOCALAPPDATA%\PriconneReALLTLInstaller`), not the install folder — a Windows uninstall leaves no stray `ReALLTLInstaller.log` / `ReALLTLAutoUpdater.log`.
- **"Check for Updates Now"** logs its result and resets the status bar (no more stuck "Checking…").
- **Wrong-system-clock detection** — if the clock is off by more than a day vs GitHub's server time, the app warns once to correct it (a wrong clock breaks update checks and can stop translations from showing).
- **Version-cache bypass is per-flow** (AsyncLocal) — fixes a race where switching source + "Check Now" could briefly show a stale version.
- **A failed manifest update during uninstall falls back to the safe full-removal path** instead of leaving a stale manifest (which could double-remove shared files later).
- **A missing wrapped-launch target is validated** before launch (must be an existing `.exe`/`.lnk`), else it falls back to DMM Game Player.
- **Hardened internals** — the release-tree fallback is awaited properly (no blocking-on-async deadlock risk); the release asset list is null-guarded; the self-update download no longer tries to stamp a `.exe` as a zip.

### Changed
- Old releases now keep their **`SHA256SUMS.txt`** (only the heavy installer binaries are stripped), so an older build stays verifiable.
- Language labels standardized — full names use each language's native form (**English / ไทย**), abbreviations use ISO codes (**EN / TH**).

### Security
- **XXE prevention** on settings import — XML is parsed with DTD and external-entity resolution disabled.

## [3.0.1] — 2026-06-03

Bug-fix release: correctness, install integrity, and crash-safety hardening from a full-project
review. No behavioral change to a healthy install — all fixes matter on the unhappy paths.

### Fixed
- **Version comparison is now numeric, not lexicographic** — `2.1.10` is correctly newer than `2.1.9` (and `3.0.10` newer than `3.0.9`); semver patch numbers ≥ 10 and date tags with a trailing letter no longer mis-sort, so a genuinely newer patch or installer update can't be silently skipped.
- **Interrupted downloads can no longer corrupt a working install** — the patch zip downloads to a temp file and is promoted to the reusable cache only after it verifies, so a partial/aborted download is never reused and extracted; the rate-limit cache fallback also restores the expected SHA-256 digest.
- **The game is no longer launched after a failed operation** — "Launch Game" starts the game only when the install/uninstall actually succeeded, so a half-patched install can't auto-launch and hide the error.
- **Config / ignored-file removal is path-guarded** — deletions during "Remove Config / Remove Ignored" are confined to the game folder (parity with the main removal + extraction guards).
- **A backward system clock no longer freezes update checks** — the version cache treated a clock set *backward* as "always fresh"; it now re-fetches when the cache age is out of range.
- **A missing Content-Length no longer crashes the progress bar** — download progress is clamped to 0–100%.
- **A malformed game-path entry is rejected** instead of treated as valid (no more null-path crash).
- **The "open game folder" command quotes the path** so folders with spaces open correctly.

## [3.0.0] — 2026-06-03 (first public release)

PriconneReALLTL-Installer is a rebranded, fully detached fork of
[tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer)
("ReALLTL" = supports **all** translation patches), with its own identity so the two
installers never collide. This is the first version published as a GitHub Release
(the earlier 2.x line was internal, dev-only).

### Added
- **Selectable translation source (English / ไทย).** Switch between
  [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) and
  [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) from the main screen.
  Every patch URL derives from the selected source — adding a language is a single registry entry.
- **Per-source installed-version detection** — each source declares its own `Version.txt` path + regex.
- **Per-user Inno Setup installer** alongside the portable exe — no admin, Start Menu shortcut, and a clean
  uninstall that also clears the app's local cache/settings. Self-update keeps working (per-user install).
- **SHA-256 verify-before-touch** — the downloaded patch zip is verified against GitHub's published digest
  before any file is removed or extracted, so a corrupt/interrupted download can't half-overwrite a working install.
- **Local zip cache** — the ~330 MB patch zip is reused on repeat installs / source switches (no re-download).
- **Ref-counted uninstall (install manifest)** — with English + ไทย installed, uninstalling one keeps the other and the
  shared modloader working; uninstalling the last source removes everything.
- **Per-source `AutoTranslatorConfig.ini` sync** — language + texture list follow the active source automatically.
- **Shortcut-wrapped launching** — "wrap" an existing launcher shortcut (DMM, DMMGamePlayerFastLauncher,
  or a PriconneMultiAccountLauncher account) so pressing it updates the patch, then launches that target. Reversible.
- **PriconneMultiAccountLauncher integration** — detected via its Inno Setup uninstall key (HKCU/HKLM) with an `%APPDATA%` fallback.
- **GitHub version-check caching** so an API token is optional; the installer self-update check caches for 7 days.
  Tokens are stored DPAPI-encrypted and never logged. A **Check for Updates Now** menu action runs an on-demand live check.
- **Strong-name signing** and a GitHub Actions release workflow: builds the portable exe + installer, attaches
  `SHA256SUMS.txt`, and keeps installer assets only on the latest release.

### Changed
- Rebranded namespace / assembly / exe / solution / project; new GUIDs and strong-name key; self-update repointed to `HetCreep/PriconneReALLTL-Installer`.
- Modloader baseline pinned to ImaterialC (the canonical IL2CPP interop), independent of the selected TL source.
- All GitHub calls run off the UI thread — a slow or rate-limited response no longer freezes the window.
- Installed files keep their real source-build timestamps; folders take their newest contained file's date.
- Window fits and centers to the screen working area; "TL Source" selector made prominent and on its own line.
- **Target framework upgraded to .NET Framework 4.8** (from 4.7.2) — preinstalled on Windows 10 1903+/Windows 11, so users need no extra runtime; still serviced by Microsoft.
- **Own neutral installer logo** — replaced the borrowed ImaterialC English-mod artwork ("PRINCESS CONNECT! Re:Dive" / "Unofficial En patch") with a language-neutral "Priconne Re:ALLTL" wordmark, so no single language is baked into the branding.

### Fixed
- Operations (Update / Reinstall / Uninstall / Launch) and the auto-update shortcut flow are **no longer disabled**
  when the modloader-latest check fails — that check is now a soft, non-blocking warning.
- Version checks fall back to the last cached value on a GitHub error (403/rate-limit) instead of showing "N/A".
- Versions are normalized (leading `v`/`V` stripped) before comparison and display.
- Fresh installs decide update-vs-install correctly (no more spurious 404 on a clean target).
- Archive extraction hardened with a zip-slip (path-traversal) guard; removal is path-guarded to the game folder.

### Removed
- Housekeeping: 7 dead settings and ~5.6 MB of orphan image/font/resource assets pruned (zero functional change; build stays zero-warning).

### Security
- **DPAPI secret-buffer zeroing** — the GitHub token's plaintext and decrypted byte buffers are cleared (`Array.Clear`) immediately after use.
- **Log-injection prevention** — CR/LF/tab in any logged string are neutralized before the line is written (OWASP A09), on top of the existing fail-closed token redaction.
- **Dependabot** alerts + automated security fixes enabled, with a weekly NuGet / GitHub-Actions update config.

### Attribution
- MIT-licensed fork of tynave/PriconneReTL-Installer; original inspiration touanu/PriconeTL_Updater. Translation patches by ImaterialC (English) and PeterkleCG (ไทย).

[Unreleased]: https://github.com/HetCreep/PriconneReALLTL-Installer/compare/v3.0.4...HEAD
[3.0.4]: https://github.com/HetCreep/PriconneReALLTL-Installer/releases/tag/v3.0.4
[3.0.3]: https://github.com/HetCreep/PriconneReALLTL-Installer/releases/tag/v3.0.3
[3.0.2]: https://github.com/HetCreep/PriconneReALLTL-Installer/releases/tag/v3.0.2
[3.0.1]: https://github.com/HetCreep/PriconneReALLTL-Installer/releases/tag/v3.0.1
[3.0.0]: https://github.com/HetCreep/PriconneReALLTL-Installer/releases/tag/v3.0.0
