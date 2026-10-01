**PriconneReALLTL Installer v3.1.8** — small follow-up fixes from the CodeRabbit review of the v3.1.7 code. Nothing here changes how you install or update; one is a window-freeze fix for when GitHub is unreachable.

## Fixed

- **A GitHub-token check that could not complete no longer blocks the window over and over.** When GitHub was unreachable or rate-limited, the check was retried on the window's own thread right after the background check had already failed the same way. A failed check is now remembered for 60 seconds. The **Validate** button in the token window always re-checks.

## Security

- Release workflow: pre-release tags (`v1.2.3-rc.1`) are rejected for now. The newest-tag guard cannot order them and the create step would not mark them as pre-releases, so one would have been published as "latest" and stripped the stable release's binaries. Plain `vX.Y.Z` tags only.

## CI

- The PR build check declares its read-only token at job level.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.7 — settings, token, and your installed patch are unaffected.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
- With a saved token and no network, the very first token check can still pause the window once (until the request times out); it is no longer repeated.
