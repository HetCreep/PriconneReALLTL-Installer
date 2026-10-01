**PriconneReALLTL Installer v3.1.7** — fixes from a second AI code review (CodeRabbit) round; every finding was checked against the code before being fixed. The important one affects Vietnamese / text-only installs, so please update.

## Fixed

- **A failed release re-read could install the wrong layer yet report success (Vietnamese / text-only sources).** After staging the modloader base, the installer re-read the selected source's release info and ignored the result. If that read failed (rate limit, offline), the engine base was downloaded and "verified" a second time, the translation text was never installed and — on Update / Reinstall, after the old files were removed — the operation still said "complete". The result is now checked and the operation stops before touching your install.
- **Hash verification now fails closed.** An error while hashing a download used to count as "verified"; it is now treated as unverified. Only a release with no GitHub-published digest at all (very old releases) is let through, and the log says so. The README's SHA-256 statement (English / ไทย / Tiếng Việt) was narrowed to match.
- **Wrapped-shortcut copies can no longer clobber or be confused with your own files.** The Desktop "(TL update)" copy gets a unique name and is never created over an existing file; whether the installer created a copy is recorded in the shortcut itself instead of guessed from its name, so a shortcut of yours that merely ends in "(TL update)" is restored, never deleted; and Remove keeps the entry (and tells you) when the delete or restore fails, instead of forgetting a shortcut that is still wrapped.

## Security

- Release workflow: the tag name now reaches the shell through the environment and must be a plain version (`v1.2.3`, `v1.2.3-rc.1`) before the privileged job uses it, and the manual-trigger path was removed so a release build runs from a `v*` tag only.

## CI

- Dependabot patch / minor bumps now auto-merge once the required checks pass (a new PR `build` check + dependency review); major bumps still wait for a human.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.6 — settings, token, and your installed patch are unaffected.
- Desktop "(TL update)" shortcut copies created by v3.1.6 or older carry no ownership marker, so **Remove** now restores them to their original launcher instead of deleting them — delete those leftover Desktop copies by hand if you don't want them. Copies made from this version on are deleted automatically.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
