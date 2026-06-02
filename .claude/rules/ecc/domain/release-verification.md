> This file defines user-facing release-verification standards for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Release Verification

Bridges [distribution-security.md](distribution-security.md) (how we build) with what an end user can check before running the binary.

## User Workflow — Authoritative

- The user **does not build locally**. They verify only via downloaded GitHub release builds from `https://github.com/HetCreep/PriconneReALLTL-Installer/releases`.
- The only artifacts that matter for trust are the ones published on that Releases page.

## What Every Release Provides

Mandatory attached artifacts:

| Artifact | Purpose |
|---|---|
| `PriconneReALLTLInstaller.exe` (portable, `assets[0]`) | No-install variant; also the self-update target |
| `…Setup.exe` (Inno, per-user) | Installer with uninstaller |
| `SHA256SUMS.txt` | SHA-256 of every binary artifact |
| Release notes (GitHub release body) | Highlights / Fixes / Security / Verification / Compatibility / Upgrade Notes / Known Issues |

Not provided (state honestly): Authenticode-signed binaries, PGP-signed checksums, SBOM. Their absence is documented, not hidden.

## Verification Levels The Project Supports

### L1 — SHA-256 (available now, the primary check)

```powershell
Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller.exe
# Compare to the value in SHA256SUMS.txt or the release notes.
```

Mismatch → the binary was modified after release: **stop, do not run**.

### L3 — Authenticode (future, not yet available)

Currently the binary is **not** Authenticode-signed (see distribution-security.md). `Get-AuthenticodeSignature` will show `NotSigned` / `UnknownError` — this is expected today. When a cert is obtained this becomes the strongest check; until then, rely on L1.

### Reproducible-build env (advanced)

Each release publishes a build-environment manifest in its notes (the .NET SDK / MSBuild / NuGet / Inno Setup versions used by CI). An advanced user can clone at the tag, build with `MSBuild PriconneReALLTLInstaller.sln /p:Configuration=Release`, and compare (modulo embedded timestamps). Reproducibility is best-effort, not guaranteed.

> There is no L2/PGP and no L4 SBOM step for this project — do not reference them in release notes.

## Release-Notes Template

```markdown
## Highlights
- <key user-visible change>

## Fixes
- <bug fix>

## Security
- <redaction fix, dependency bump, egress audit result, etc.>

## Verification
- Portable exe SHA-256: `<hex>`
- Setup SHA-256: `<hex>`
- Authenticode: not signed this release (verify via SHA-256 above)
- Build env: .NET Framework 4.7.2 · MSBuild <ver> · Inno Setup <ver>

## Compatibility
- Windows 10 / Windows 11
- Requires the game (Princess Connect! Re:Dive via DMM Game Player) installed
- Translation sources: EN (ImaterialC/PriconneRe-TL), TH (PeterkleCG/PriconneTH); modloader pinned to ImaterialC

## Upgrade Notes
- <breaking change / migration steps, if any>

## Known Issues
- <issue> — workaround: <...>
```

There is **no "Upstream Sync" section** — this is a detached fork; routine pulling from upstream `tynave/PriconneReTL-Installer` is suspended (it's a reference only, never used for self-update). Keep the `PriconneReALLTL` rebrand on any change.

## In-App Update Check

- Hits `api.github.com/repos/HetCreep/PriconneReALLTL-Installer/releases/latest` over HTTPS (TLS validated), compares versions (normalized, leading `v`/`V` stripped), 7-day cache.
- If newer: the user is offered a **manual** download (SaveFileDialog) and runs the new exe themselves; never silent in-place.
- Auto-download + auto-install is out of scope — it would defeat user-side verification.
- The AutoUpdater feature repos (`HetCreep/PriconneReALLTL-AutoUpdater[App]`) do not exist yet; that path 404s and is handled softly — not a release blocker.

## What The User Does Not Do

| Action | Why not |
|---|---|
| Build from source as part of normal use | Verification is via published builds |
| Trust binaries from third-party hosts (Discord, Telegram, email) | Only the official GitHub Releases page |
| Run pre-releases unknowingly | Pre-releases are clearly tagged; opt-in only |

## Rollback

If a release introduces a regression:

1. Tag a **new patch release** with the fix (preferred — Reinstall re-extracts everything, so a forward fix is clean).
2. If severe: edit the bad release on GitHub to **mark it pre-release** (removes it from the "latest" pointer).
3. **Never** fully unpublish a release users may already hold — its notes/`SHA256SUMS.txt` are how they verify their existing copy (binary assets may be stripped, the verification record must stay — see distribution-security.md).
4. The new release's notes say: **"supersedes vX.Y.Z which had <bug>"**.

## Reference

- [distribution-security.md](distribution-security.md) — build/signing chain, asset stripping
- [telemetry-policy.md](telemetry-policy.md) — update-mechanism rules
- [install-safety.md](install-safety.md) — how a downloaded patch is verified + applied
