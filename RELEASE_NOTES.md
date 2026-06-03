**PriconneReALLTL Installer v3.0.2** — a hardening release from a full-project security + correctness review. Everything from v3.0.0 / v3.0.1 still applies; this fixes more defects on the unhappy paths (multi-language uninstall, a wrong system clock, interrupted state). A healthy install behaves the same.

## Fixes

- **Per-language "Remove Ignored"** — uninstalling one source's ignored data no longer deletes another installed language's user-edited XUnity files (uninstalling English no longer touches ไทย's substitutions / pre/post-processors).
- **Logs live in the app data folder** (`%LOCALAPPDATA%\PriconneReALLTLInstaller`), not the install folder — a Windows uninstall leaves no stray log behind.
- **"Check for Updates Now"** logs its result and resets the status bar (no stuck "Checking…").
- **Wrong-clock warning** — if your system clock is off by more than a day vs GitHub's time, the app warns once to correct it (a wrong clock breaks update checks and can stop translations from showing — see Known Issues).
- **Per-flow version-cache bypass** (AsyncLocal) — no more stale-version flash when switching source and checking for updates together.
- **Safe manifest-update fallback** on uninstall — a failed manifest write no longer risks a later double-removal of shared files.
- **Validated wrapped-launch target** — must be an existing `.exe`/`.lnk`, otherwise it falls back to DMM Game Player.
- **Hardened internals** — the release-tree fallback is awaited (no blocking-on-async); the release asset list is null-guarded; the self-update download won't stamp a `.exe` as a zip; **XXE-safe** settings import.

## Changed

- Old releases keep their **`SHA256SUMS.txt`** (only the heavy installer binaries are stripped) → an older build stays verifiable.
- Language labels: full names use each language's native form (**English / ไทย**), abbreviations use ISO codes (**EN / TH**).

## Verification

- Verify your download against **`SHA256SUMS.txt`**:
  `Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.2.exe`
- Authenticode: not signed this release (verify via SHA-256 above).

## Compatibility

- Windows 10 (64-bit) / 11 — same as DMM Game Player's own requirement. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.0.0 / v3.0.1 — settings, token, and your installed patch are unaffected.

## Known Issues

- If translations don't appear in-game and the patch is installed, check that your **Windows date/time is correct** — the game itself can misbehave on a wrong clock (the app now warns about large skew).
- Installing/uninstalling a source **manually** (by zip) is invisible to the tracked-install manifest — install via the app for a clean per-source uninstall.
