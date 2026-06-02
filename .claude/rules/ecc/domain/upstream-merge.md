> This file defines the upstream-merge protocol for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Upstream Merge Protocol

Governs the relationship with upstream `tynave/PriconneReTL-Installer` (remote `origin`).

## Policy — Routine Pulling Suspended

- **Inbound merges from upstream are SUSPENDED.** Upstream is a **reference-only**, amicable baseline (its README links to our fork). The fork is rebranded (`PriconneReALLTLInstaller`), re-identified (own GUIDs + strong-name key), and feature-ahead (multi-source EN/TH, manifest, SHA-256 verify, self-update repoint).
- **Self-update NEVER points upstream.** It points only at `HetCreep/PriconneReALLTL-Installer/releases/latest`. All GitHub endpoints in code (release API, modloader, raw version, releases page, wiki, self-update) target **only the fork's own repos / the configured patch-source repos** — never `tynave/*` for any functional call.

## Current Model — Mostly One-Way

- **Outbound (preferred):** contribute **brand-neutral** fixes back to tynave as PRs (a generic bug/perf fix directly; a larger refactor via an Issue first). Strip our branding from anything pushed upstream.
- **Inbound (exceptional only):** none by default. The single trigger to pull is upstream shipping a **genuine fix the installer needs** (e.g. a GitHub API behavior change that breaks installs) that we have not already solved. Then **cherry-pick the one fix commit** — never a bulk `git merge` of upstream history — with the backdoor audit below and the rebrand preserved.

## Emergency Inbound Cherry-Pick

```powershell
git fetch origin
git log --oneline master..origin/master   # what upstream has that we don't
git cherry-pick <single-fix-sha>           # one commit, then audit + reconcile branding
```

### Per-Commit Audit (mandatory, even for a one-line fix)

- [ ] Does it add/alter **network egress**? → must stay within the [telemetry-policy.md](telemetry-policy.md) allow-list (the 4 GitHub hosts).
- [ ] Does it touch the **token** path? → re-validate [credential-vault.md](credential-vault.md) (DPAPI CurrentUser, never logged/transmitted).
- [ ] Does it reintroduce **telemetry** (Discord, analytics, crash-report, heartbeat)? → reject per telemetry-policy.md.
- [ ] Does it rename anything back to `PriconneReTLInstaller` / change namespace / assembly / exe / GUIDs / `.snk`? → **reject the rename**, keep the `PriconneReALLTL` brand and identity.
- [ ] Does it touch `.iss` / CI / build? → re-validate the signing chain + the unique Inno `AppId` per [distribution-security.md](distribution-security.md).
- [ ] Does it add a **binary blob / committed DLL / exe** to the repo? → **stop**, manual provenance required.
- [ ] Does it touch file install/uninstall? → re-confirm the path guard + zip-slip + manifest per [native-windows-api.md](native-windows-api.md).

### Backdoor Red Flags

| Red flag | Action |
|---|---|
| New URL / host not in the egress allow-list | Reject or quarantine |
| `Convert.FromBase64String` / `Encoding…GetString` on a literal blob then run/load it | Decode + inspect before accepting |
| Any dynamic code exec (`Assembly.Load` of fetched bytes, reflection-invoke of a literal) | Reject |
| `Process.Start` built from a concatenated/interpolated shell string | Reject (see native-windows-api.md) |
| New native/Win32 call (`DllImport`, `ProtectedData` scope change, registry **write**) | Justify before merge |
| Disabled TLS / cert validation (`ServerCertificateValidationCallback => true`, `verify off`) | Reject |
| New `.dll` / `.exe` / `.snk` checked into the repo | Reject without provenance proof |
| Commit-author email differs across commits in the same PR | Verify each commit |

## Rebrand Preservation (on any inbound change)

Always keep the fork side for:

- Namespace / RootNamespace / AssemblyName / exe / project folder / `.sln` / `.csproj` = `PriconneReALLTLInstaller`.
- The fork's GUIDs and `PriconneReALLTLInstaller.snk` strong-name key.
- All functional GitHub URLs → `HetCreep/PriconneReALLTL-*` (self-update) or the configured patch-source repos; never repoint to `tynave/*`.
- `LICENSE`: keep tynave's MIT copyright (legal) + the HetCreep fork notice; keep the "Based on tynave/PriconneReTL-Installer" attribution string (goodwill, intentional — not a stale link).

## Diverging Permanently

When a file must stay divergent from upstream, note it in MEMORY.md (path, why, review date) and re-evaluate occasionally — but the default state is *already* permanent divergence, so this is the norm, not the exception.

## Reference

- [credential-vault.md](credential-vault.md)
- [telemetry-policy.md](telemetry-policy.md)
- [distribution-security.md](distribution-security.md)
- [native-windows-api.md](native-windows-api.md)
- [../common/git-workflow.md](../common/git-workflow.md)
