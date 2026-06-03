**PriconneReALLTL Installer v3.0.3** — a small security + robustness release. Everything from v3.0.0–v3.0.2 still applies; a healthy install behaves the same.

## Fixes

- **Shortcut wrapping is no longer silent + handles protected folders** — "Set / Modify launch shortcut" now always tells you the result, and if a shortcut is in a write-protected folder (e.g. the All-Users Start Menu under `C:\ProgramData\…`) it wraps a copy on your Desktop ("… (TL update)") instead of doing nothing.
- **Self-update can't crash the app** — the installer self-update flow is guarded against unhandled exceptions.

## Security

- The validated GitHub token is cached by **SHA-256 hash**, never held as plaintext in a long-lived field.
- Diagnostic logging no longer uses `Console.WriteLine` (it bypassed the log redactor); debug traces are compiled out of release builds.
- **CodeQL code scanning** is now enabled (C# static analysis on every push / PR), alongside Dependabot.

## Verification

- Verify your download against **`SHA256SUMS.txt`**:
  `Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.3.exe`
- Authenticode: not signed this release (verify via SHA-256 above).

## Compatibility

- Windows 10 (64-bit) / 11 — same as DMM Game Player's own requirement. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.0.x — settings, token, and your installed patch are unaffected.
