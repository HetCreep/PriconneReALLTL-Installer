**PriconneReALLTL Installer v3.0.7** — fixes the v3.0.6 title-bar regression and rolls in menu icons + resumable downloads. A healthy install behaves the same.

## Added

- **Menu icons** — the Settings and Help menus now show a small icon beside each item, rendered from a Windows system icon font (no new artwork, no extra download).
- **Resumable patch downloads** — if a download is interrupted (a slow or flaky connection), running it again **resumes from where it stopped** instead of re-downloading the ~330 MB from zero, with automatic retry and backoff. The file is still SHA-256-verified before anything touches your install, so an incomplete or mismatched download can never overwrite a working patch.

## Fixed

- **The blue Windows title bar is gone** — v3.0.5/v3.0.6 accidentally resurfaced the OS caption on every window (and clipped the bottom status row when logs were collapsed). All windows are borderless custom-chrome again.
- **Minimizing from the taskbar works again.**
- **Cleaner log messages** — no literal `\n` in the "modloader outdated" notice, no stray `$` in the export/import lines, a routine "found config file(s)" message no longer shows as a red error, and consistent international-English wording (including an "occurred" spelling fix).

## Changed

- **The bug-report template is now a guided form** — dropdowns for translation source / operation / launcher plus required fields.
- **CONTRIBUTING's coding-conventions** now document the project's real standards (logging style, install-safety invariants, the borderless-form rule).

## Security

- **Logged file paths now mask your Windows username** (`%USERPROFILE%`) — less personal information ends up in a log you might paste into an issue. (The GitHub token was already redacted.)

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.0.x — settings, token, and your installed patch are unaffected.
