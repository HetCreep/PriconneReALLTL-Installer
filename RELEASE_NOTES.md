**PriconneReALLTL Installer v3.0.5** — a large correctness, safety, and security release from a full-codebase deep audit layered on top of the v3.0.4 test campaign. Most fixes matter on the unhappy paths (a locked file, a running instance, a corrupt manifest, a cold token) and several are leaks/races that manual testing can't surface. A healthy install behaves the same.

## Highlights

- **A locked translation file can no longer break a reinstall/uninstall.** The installer now checks that every tracked file is unlocked first and aborts cleanly — leaving your install 100% intact — if the game, antivirus, Windows Search, a cloud-sync app, or an editor is holding a file.
- **The installer/uninstaller detects a running instance** and offers to close it first, instead of failing on the locked program.
- **Wrapped launcher shortcuts get a clean lifecycle** — un-wrapped automatically when you uninstall the program (so they revert to plain game-launchers), Desktop "… (TL update)" copies removed when you un-manage them, and moved/deleted ones flagged "(missing)".
- **New: Settings → "Reclaim Space in Game Folder"** — frees disk by removing BepInEx's regenerable log + assembly cache and pruning leftover empty folders; never touches your patch, settings, or any user files.
- A pile of crash-safety, resource-leak, and UI-responsiveness fixes from the audit, plus consistent window titles and UI text.

## Security

- The optional GitHub token is **no longer loaded into a UI field**, **no longer attached to release-asset downloads** (closing a path where a redirect could forward it off-GitHub), and **validated off the UI thread**.
- Settings import is hardened against **XXE**; error dialogs no longer print stack traces.
- The honest-disclosure docs now acknowledge the inherited UI art as a known gap to re-skin.

## Fixed / Changed

See the [CHANGELOG](CHANGELOG.md) for the full v3.0.5 list.

## Verification

- Verify your download against **`SHA256SUMS.txt`**:
  `Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.5.exe`
- Authenticode: not signed this release (verify via SHA-256 above).

## Compatibility

- Windows 10 (64-bit) / 11 — same as DMM Game Player's own requirement. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.0.x — settings, token, and your installed patch are unaffected.
