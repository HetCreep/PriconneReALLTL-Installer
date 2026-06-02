> This file defines release/build-integrity standards for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Distribution Security

Rules for how the installer is built, signed, and delivered. A user must be able to trust the binary they download.

## Build Pipeline — CI-Only

- Release builds run in **GitHub Actions only** (`.github/workflows/release.yml`), never from a maintainer's machine. Rationale: reproducibility, audit trail, no laptop-compromise → supply-chain compromise.
- Workflow triggers on a **`v*` tag only**. A no-`v` tag (e.g. `2.4.0`) must NOT produce a release (those are upstream tynave's convention).
- Pin every `uses:` action to a commit SHA, not a floating tag.
- No `pull_request_target` triggers (privilege-escalation surface).
- Restrict `GITHUB_TOKEN` permissions to the minimum the job needs.
- Pipeline outline: checkout at tag → `nuget restore PriconneReALLTLInstaller.sln` → `MSBuild … /p:Configuration=Release` (strong-name signs via the committed `.snk`) → build the per-user Inno installer (`iscc`) → compute SHA-256 of every artifact → attach portable exe + Setup + `SHA256SUMS.txt` to the GitHub Release.
- A maintainer with the same tag + the documented SDK/MSBuild/Inno versions should reproduce a byte-equal build modulo embedded timestamps; publish those tool versions in the release notes (see release-verification.md).

## Code Signing

- **Strong-name signed (mandatory):** `PriconneReALLTLInstaller.snk` is committed so CI can sign; assembly identity is distinct from upstream (own GUIDs + own key). Strong-naming is an identity/anti-collision measure, NOT a trust/anti-tamper measure.
- **Authenticode = honest gap.** The exe and Setup are **NOT** Authenticode code-signed. Consequence: SmartScreen / Defender may warn on first run.
  - This gap MUST be documented in `README.md` and the release notes ("this binary is not code-signed").
  - The mitigation we DO ship is `SHA256SUMS.txt` for manual integrity verification.
  - Do not pretend it is signed; do not suppress the warning by any trick. If an Authenticode cert is ever obtained, add `signtool sign … /fd SHA256 /tr <timestamp> /td SHA256` + `signtool verify /pa` to CI and stop documenting it as a gap.

## SHA256SUMS

- Every release attaches `SHA256SUMS.txt` listing the hash of each binary artifact (portable exe + Setup).
- Release notes embed the primary artifacts' SHA-256 inline so a user can verify without downloading the manifest separately.
- The hash line for any artifact a user may already hold must remain retrievable forever (see "Old-Release Asset Stripping").

## Inno Setup Installer Rules

`PriconneReALLTLInstaller.iss` must:

- Use a **unique `AppId` = `{45A4682D-8431-422B-BA7C-304E9C318EAA}`** — never reuse upstream tynave's or any other AppId (collisions break uninstall).
- Install **per-user** (`PrivilegesRequired=lowest`); no admin elevation.
- Carry `[UninstallDelete]` for `%LOCALAPPDATA%\PriconneReALLTLInstaller` so config + token + cache are removed on uninstall.
- Never auto-launch anything except the installed app itself (an optional, user-checkable "launch now" `[Run]` of `PriconneReALLTLInstaller.exe` is the ONLY allowed `[Run]` entry).
- Keep per-user install so the in-app self-update keeps working without elevation.

## Installer Forbidden Actions

| Forbidden | Reason |
|---|---|
| Bundling any additional software, toolbar, or "offer" | Trust / PUP behavior |
| Adding Run keys, startup-folder entries, or scheduled tasks | Unwanted persistence |
| Any internet access during install (download steps, "phone home") | Telemetry + supply-chain surface (see telemetry-policy.md) |
| `[Run]` of any binary other than `PriconneReALLTLInstaller.exe` | Arbitrary execution |
| Pascal `[Code]` that reads/writes/executes outside the install dir or `%LOCALAPPDATA%\PriconneReALLTLInstaller` | Scope escape |
| Registering the app with any online service on install completion | Telemetry |

## Self-Update Integrity Stance

- Self-update checks `HetCreep/PriconneReALLTL-Installer/releases/latest` over HTTPS with normal TLS validation (never disable cert checks). 7-day cache; disable via the "Check for Installer Updates on Startup" toggle; on-demand "Check for Updates Now".
- Apply is **MANUAL only**: the app downloads the new exe to a user-chosen path (SaveFileDialog) and the user runs it. **Never** silent in-place replacement of the running binary.
- Honestly note in-app/README that the downloaded update is **not Authenticode-verified**; `SHA256SUMS.txt` is provided so the user can verify the download before running it.
- Update host is GitHub only — no third-party update server, no query-param fingerprinting (see telemetry-policy.md).

## Old-Release Asset Stripping

- Stripping a superseded release's **installer/exe assets** to save storage is allowed.
- But every release MUST retain its **source archive** and the **`SHA256SUMS.txt` record in its notes**, so anyone who downloaded an older copy can still verify it. This is the reconciliation of "save space" with "never unpublish a verifiable artifact" — we strip the heavy binary, we never erase the verification record.

## Anti-Patterns

| Pattern | Reason |
|---|---|
| Building a release on a laptop and uploading | No reproducibility; laptop compromise = release compromise |
| Auto-merging PRs that touch `.iss` / CI / build config | Privilege-escalation vector |
| Reusing upstream's Inno `AppId` | Uninstall confusion, install collision |
| Self-update that overwrites the running exe in place | Bypasses user verification |
| Updating from any host that is not GitHub Releases | Telemetry + supply-chain surface |
| Embedding a prebuilt third-party binary not produced from source in CI | Supply-chain (note: Newtonsoft.Json is embedded **as a build step**, restored from NuGet, not a checked-in blob — that is allowed) |

## Reference

- [release-verification.md](release-verification.md) — user-facing verification flow
- [telemetry-policy.md](telemetry-policy.md) — update mechanism + egress constraints
- [legal-boundary.md](legal-boundary.md) — license / trademark
- [upstream-merge.md](upstream-merge.md) — CI/build merge protocol
- [../csharp/security.md](../csharp/security.md)
