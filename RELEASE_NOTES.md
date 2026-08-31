**PriconneReALLTL Installer v3.1.2** — a patch release fixing a download-integrity error that could get stuck repeating on every retry.

## Fixed

- **"Downloaded file failed the SHA256 integrity check" repeating on every retry.** A fresh download is checked against a digest pulled from the 6-hour version-check cache. If a source re-uploads its release asset under the same tag, that cached digest goes stale — every following download attempt (even a fully clean one) failed the same comparison until the cache expired on its own, up to 6 hours later. The installer now drops the stale cache entry the moment a fresh download fails its digest check, so the next attempt re-fetches live release metadata instead of repeating the same failing comparison. The digest check itself is unchanged — a genuinely corrupted or tampered download still stops before touching your install.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.1 — settings, token, and your installed patch are unaffected.
- If you previously hit the stuck SHA256 error, this release fixes it going forward with no manual cache-clearing needed.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
