**PriconneReALLTL Installer v3.0.6** — a small follow-up to v3.0.5: a second review pass over the v3.0.5 changes (no regressions found) plus a re-sweep of corners the first audit covered lightly. A healthy install behaves the same.

## Fixed

- **Completed the COM-handle cleanup from v3.0.5** — the "Create AutoUpdater shortcut" action now releases its shell objects like the other shortcut operations.
- **Folder-boundary check** — "is this file inside the game folder?" (used when adding an ignore-list entry) now respects a folder boundary, so a sibling folder with a similar name is no longer mistaken for being inside the game folder.
- **Modloader version parsing** — the installed-modloader version reads correctly when a component has three digits (e.g. `6.0.100`).
- **Internal hardening** — the uninstall manifest's staged state is reset at the start of each removal (defensive), and a dead no-op handler was removed.

## Verification

- Verify your download against **`SHA256SUMS.txt`**:
  `Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.6.exe`
- Authenticode: not signed this release (verify via SHA-256 above).

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.0.x — settings, token, and your installed patch are unaffected.
