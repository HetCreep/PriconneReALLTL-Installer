**PriconneReALLTL Installer v3.1.1** — a patch release fixing a v3.1.0 defect in the Vietnamese install path, found by a closeout audit. If you use the Vietnamese source with the AutoUpdater shortcut, please update.

## Fixed

- **The AutoUpdater shortcut no longer breaks a Vietnamese install.** v3.1.0's auto-update path (the "update + play" shortcut) did not chain the modloader engine base for a text-only source like Vietnamese, so a Vietnamese-only install updated this way would have its BepInEx engine removed and never restored — leaving the game unmodded. The auto-update path now stages and re-applies the ImaterialC engine base before the text layer, with the same SHA-256 verify-before-touch ordering as the main-window operations. (The main-window Install / Update / Reinstall were already correct in v3.1.0 and are unaffected.)
- **Logging hardening (defense-in-depth)** — the logger's own fallback diagnostic messages now also pass through the credential redactor. No token was ever exposed by these paths; this closes the gap structurally.

## Changed

- The installer's file-properties description now reads `(EN/TH/VN)`.
- Corrected the wording (from v3.1.0) describing the Vietnamese fixup plugins — they load because the ImaterialC engine base ships them active, not via a per-source toggle.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.0 — settings, token, and your installed patch are unaffected.
- Vietnamese users who set up an AutoUpdater shortcut on v3.1.0: after updating to v3.1.1, run **Reinstall** once in the app to restore the engine base if your install was already affected.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
