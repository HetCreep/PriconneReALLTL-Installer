**PriconneReALLTL Installer v3.0.4** — a small security + UI-polish release. Everything from v3.0.0–v3.0.3 still applies; a healthy install behaves the same.

## Security

- **Zip extraction now writes to the path it just validated.** The zip-slip (path-traversal) guard already confined every entry to the game folder — the extraction step now writes to that same guard-checked path instead of re-deriving it from the (untrusted) entry name. Resolves the CodeQL `cs/zipslip` alert; no behavioral change for a healthy patch.

## Changed

- **Language labels finalized** — the compact "TL Source" label shows the ISO code (**EN** / **TH**); the source picker shows each language's native full name (**English** / **ไทย**). Internal manifest keys are unchanged, so existing installs are unaffected.

## Verification

- Verify your download against **`SHA256SUMS.txt`**:
  `Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.4.exe`
- Authenticode: not signed this release (verify via SHA-256 above).

## Compatibility

- Windows 10 (64-bit) / 11 — same as DMM Game Player's own requirement. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.0.x — settings, token, and your installed patch are unaffected.
