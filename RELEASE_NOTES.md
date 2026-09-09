**PriconneReALLTL Installer v3.1.3** — a patch release fixing the real cause of the recurring SHA256 integrity error for the Vietnamese source.

## Fixed

- **"Downloaded file failed the SHA256 integrity check" on Install / Update / Reinstall / AutoUpdate for Vietnamese.** Vietnamese ships text-only, so every operation first stages the ImaterialC modloader base, then re-fetches the Vietnamese release's own link + digest before downloading it. A parameter-shadowing bug meant the actual download kept using the *original* link the caller had passed in, while the integrity check compared it against the freshly re-fetched digest. If Vietnamese published a newer version between your last version check and the moment you clicked Install/Update — routine, since it ships often — every attempt failed the same way until the app restarted. v3.1.2 didn't fix this: that release addressed a stale on-disk cache, but this mismatch was two in-memory values drifting apart mid-operation, not a cache problem. All four affected call sites now use the refreshed link.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.2 — settings, token, and your installed patch are unaffected.
- If Vietnamese install/update was stuck failing, this release fixes it — no manual workaround needed.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
