**PriconneReALLTL Installer v3.1.6** — a performance patch: the patch download (and the extract / remove steps) no longer crawl. If a ~370 MB download was taking tens of minutes for you, this is the fix.

## Fixed

- **Downloads, extraction and removal were throttled by the progress bar.** The progress callback fired for every 80 KB chunk (and for every extracted / removed file), and each call made the worker thread wait for the window to redraw — thousands of synchronous round-trips per step. Measured on one machine: the same download loop with no window ran at ~22 MB/s (a ~370 MB patch in ~17 seconds), while the app managed ~70 KB/s (199 MB in 47 minutes). The windows now update the bar only when the whole percent changes (at most 100 updates per step) and post it without making the download wait. Applies to the main window, the auto-update window and the self-update window.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.5 — settings, token, and your installed patch are unaffected.
- An interrupted download keeps its partial file and resumes where it stopped, so you can close a slow v3.1.5 or older download, update, and carry on.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
