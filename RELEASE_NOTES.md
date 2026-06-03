**PriconneReALLTL Installer v3.0.1** — a bug-fix release hardening install integrity, version detection, and crash-safety. Everything from v3.0.0 still applies; this fixes defects found in a full-project review. A healthy install behaves the same — these fixes matter on the unhappy paths (interrupted downloads, a wrong system clock, double-digit version numbers, failed operations).

## Fixes

- **Numeric version comparison** — `2.1.10` is now correctly newer than `2.1.9` (and `3.0.10` newer than `3.0.9`). Previously versions were compared letter-by-letter, so once a segment reached two digits a genuinely newer patch or installer update could be silently *not* offered.
- **Interrupted downloads can't corrupt a working install** — the ~330 MB patch zip downloads to a temporary file and is promoted to the reusable cache only after it passes verification, so a partial or aborted download is never reused and extracted over your game. The rate-limit cache fallback now also restores the expected SHA-256 so the integrity check stays correct.
- **No launch after a failed operation** — "Launch Game" starts the game only when the install/uninstall actually succeeded, so a half-patched install can't auto-launch (and the 5-second auto-exit can't hide the error).
- **Path-guarded config/ignored removal** — "Remove Config" / "Remove Ignored Patch Files" deletions are confined to the game folder, matching the existing extraction and main-removal guards.
- **A backward system clock no longer freezes update checks** — if your PC clock was set behind the time a check was cached (a dead CMOS battery, a manual change), the app used to treat the cached result as permanently fresh and stop checking; it now re-fetches when the cached age is out of range. (Separately: if translations don't appear in-game, make sure your system date/time is correct — the game itself can misbehave on a wrong clock.)
- **Crash-safety** — a download with no Content-Length no longer throws on the progress bar (clamped to 0–100%); a malformed DMM game-path entry is rejected instead of causing a null-path crash; the "open game folder" command quotes the path so folders with spaces open correctly.

## Verification

- Verify your download against **`SHA256SUMS.txt`** attached to this release:
  `Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.1.exe`
- Authenticode: not signed this release (verify via SHA-256 above).

## Compatibility

- Windows 10 / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player.
- Translation sources: EN ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), TH ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)); modloader pinned to ImaterialC.
- A GitHub token is optional — version checks are cached, so the app works fine unauthenticated.

## Upgrade Notes

- Drop-in over v3.0.0 — settings, token, and your installed patch are unaffected. No reinstall of the translation patch is needed.

## Known Issues

- Installing/uninstalling a source **manually** (by zip) is invisible to the app's tracked-install manifest, so a manual source can't be cleanly uninstalled by the app — install via the app for a clean per-source uninstall. (A 2-layer translation/modloader uninstall is planned.)
