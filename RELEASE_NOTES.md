**PriconneReALLTL Installer v3.1.4** — a diagnostics release for the SHA256 integrity error that came back after v3.1.2 and v3.1.3.

This release does not claim to have found the definitive cause of every report — it adds one more resilience fix and evidence-gathering so the next occurrence is conclusive instead of another guess.

## Fixed

- **A stuck partial download could poison every retry.** On a digest mismatch the `.part` temp file is deleted so the next attempt starts clean — but a silently-failed delete (e.g. a file briefly locked by antivirus) left the `.part` in place, and the resumable-download logic would then append new bytes onto that same bad partial file on every subsequent attempt, forever, regardless of app version. The app now checks the delete actually succeeded and tells you to use **Settings → Clear Download Cache** if it didn't.

## Added

- **Digest-mismatch diagnostics.** When a download fails its SHA-256 check, the log now records the source, version, file size, expected vs. actual hash, and the asset URL.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.3 — settings, token, and your installed patch are unaffected.
- **If you're hitting the SHA256 error right now:** go to Settings → Clear Download Cache, then try again — this alone may resolve it if a stuck partial download is the cause. If it still fails, please share the log line(s) after "Digest mismatch detail" so the exact cause can be pinned down.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
