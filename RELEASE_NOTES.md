**PriconneReALLTL Installer v3.1.5** — fixes from a full-repository AI code review (CodeRabbit). All 22 findings were checked against the code first and every one turned out to be real; this release fixes them. Includes a token-egress fix and a self-update verification fix, so please update.

## Security

- **Your GitHub token is no longer sent to `raw.githubusercontent.com`.** The modloader-version check reused its authenticated client for a raw-file request, contradicting the privacy policy (token to `api.github.com` only). It now uses a separate, unauthenticated client.
- **The installer's self-update download is now SHA-256-verified** against the digest GitHub publishes for it. Before, the check was skipped while the log still said "verified". A leftover partial download from an older self-update is discarded instead of resumed, so an old exe's head can no longer be glued to a newer exe's tail. If GitHub publishes no digest, the log now says it could not verify instead of claiming success.
- Setup no longer offers an all-users (admin) install — per-user only, so the in-app self-update keeps working.
- Release workflow hardening: checkout no longer persists credentials, and the newest-tag guard / old-release cleanup no longer silently stop at 100 tags / 30 releases.

## Fixed

- **Bogus "SHA256 integrity check" failures from mismatched release info.** A download link, its release version and its digest could come from different fetches (a fast TL-source switch, a caller holding an older link). They are now re-paired right before downloading, and the operation aborts safely — before touching your install — if they cannot be.
- **The TL-source selector is locked while an operation runs** (switching mid-install pointed the manifest owner, `Language=` and digest at the wrong source).
- **Update / Reinstall no longer deletes a patch file you added to the ignore list after installing it.**
- **Your "Launch Game" choice is no longer reset** by every operation or failed version check.
- **Settings migration now runs for every entry point** — `autoupdate` shortcuts and the uninstaller's shortcut cleanup previously could read empty defaults right after an update.
- **A saved GitHub token is no longer deleted when GitHub is merely unreachable** — only a definitive "Bad credentials" clears it, and a rejected token no longer stalls the window on every refresh.
- The UI font stays allocated for the life of the process and is registered once instead of on every window.
- Errors the installer's internal helper used to drop silently (corrupt-manifest backup, `Language=` sync failures) now reach the log.
- Self-update window: saves a `.exe` (it proposed `.zip`), handles a release with no notes, logs safely from background threads.
- Startup, auto-update and source-switch handlers can no longer throw an unhandled exception.

## Docs

- The privacy-issue link is described as public, the erasure statement no longer claims nothing ever leaves your machine, README icons have alt text.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.4 — settings, token, and your installed patch are unaffected.
- If you installed with the previous Setup and chose an all-users install, reinstall per-user (this version's Setup only offers per-user).

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
- A release with no GitHub-published SHA-256 digest can't be verified (the log says so); this only affects old releases.
