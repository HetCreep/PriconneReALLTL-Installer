# Contributing to PriconneReALLTL-Installer

Thanks for your interest in improving the installer. This guide covers how to build, the conventions we follow, and how to submit changes.

## Project at a glance

- **Language / framework:** C# · WinForms · **.NET Framework 4.7.2** (no .NET Core / 5+)
- **Build system:** MSBuild (Visual Studio 2022) + NuGet
- **Output:** `PriconneReALLTLInstaller\bin\Release\PriconneReALLTLInstaller.exe`
- **Strong-name signed** with `PriconneReALLTLInstaller.snk` (committed so CI can sign)

## What we accept (and don't)

**Welcome:** bug fixes (with repro steps), new translation sources, security/robustness hardening, documentation, and UI translations.

**Not accepted** — these are out of scope by design (see [`.claude/rules/ecc/domain/`](.claude/rules/ecc/domain) and [SECURITY.md](SECURITY.md)):

- Game **automation**, memory reading, or anything that touches the game's *own* binaries/servers (the installer only deploys published third-party translation files).
- **Telemetry**, analytics, crash-reporting, or Discord integration of any kind.
- Any **new outbound host** beyond the GitHub allow-list documented in [PRIVACY.md](PRIVACY.md).
- Reintroducing the upstream **`tynave` / `PriconneReTL`** branding, GUIDs, or endpoints — this fork keeps its own identity.

Open an issue first for anything beyond a one-line fix.

## Building locally

```bash
nuget restore PriconneReALLTLInstaller.sln
MSBuild PriconneReALLTLInstaller.sln /p:Configuration=Release
```

A clean build produces a small number of pre-existing warnings (`CS0108`, `CS0414`) — please don't introduce new ones.

## Branching & commits

- Work off `master`.
- One logical change per PR; keep diffs focused.
- **Commit message format:**

  ```
  <type>(<scope>): <imperative subject, ≤72 chars>

  - what changed
  - affected files / functions
  ```

  Types: `feat`, `fix`, `perf`, `refactor`, `docs`, `chore`, `ci`.

## Coding conventions

- Match the surrounding style; small, cohesive files.
- **WinForms Designer files (`*.Designer.cs`):** avoid manual *layout* edits — prefer creating controls in code (see how the "TL Source" selector is built in `MainForm`). Namespace / string-literal edits are fine.
- Validate input at boundaries; handle errors explicitly (no silent swallowing).
- Never hardcode a translation patch repo — sources live in the `Helper.PatchSource` registry and every URL derives from `Helper.GetCurrentPatchSource()`. Adding a language = one list entry.
- Keep the public docs (`README.md`, this file, `CHANGELOG.md`) in sync with behavior changes.

## Adding a translation source

1. Add a `PatchSource` entry in `HelperFunctions.cs` (`PatchSources`): display/short name, owner/repo, the per-source `Version.txt` path + regex, and any plugin profile / external plugin downloads.
2. Build and verify the new source appears in the **TL Source** dropdown and that install / update / version detection work.

## Releases & distribution

**All distributed builds come from CI — never upload a locally built exe.** Pushing a `v*` tag (e.g. `v3.0.0`) triggers `.github/workflows/release.yml`, which builds on `windows-latest`, compiles the per-user Inno Setup installer, attaches **`SHA256SUMS.txt`**, and publishes a GitHub Release (portable exe + `-Setup.exe`). Use the `v` prefix — the workflow trigger and self-update both expect it.

Before tagging: bump the version in **`Properties/AssemblyInfo.cs`** *and* **`installer/PriconneReALLTLInstaller.iss`**, and update **`RELEASE_NOTES.md`** + **`CHANGELOG.md`**.

## Forking this project

If you fork this for your own distribution, you **must** establish a distinct identity so the two installers never collide (and so your self-update doesn't point here):

1. Generate a **new Inno `AppId` GUID** in `installer/PriconneReALLTLInstaller.iss`.
2. Generate **new assembly/project GUIDs** and a **new strong-name key** (`.snk`).
3. Repoint the **self-update URL** and **patch-source repos** (`HelperFunctions.cs`: `PatchSource` registry, `ModloaderSource`, the self-update repo) to your own.
4. Update the branding strings and this documentation.

## Engineering standards

Changes to release/build, install/update/uninstall, token handling, networking, or native (registry/shortcut/process) code must uphold the project's security & privacy posture — see [SECURITY.md](SECURITY.md) and [PRIVACY.md](PRIVACY.md). In short: GitHub-only network egress, the GitHub token stays DPAPI-encrypted and is never logged, downloads are SHA-256-verified before they touch an install, removals are path-guarded to the game folder, and there is zero telemetry.

## Submitting a PR

- Make sure the solution builds in `Release` (no new warnings).
- Never print/log a secret — use the `Logger` (the token is redacted at the logging boundary; keep it that way).
- No new NuGet dependency without a supply-chain + license justification in the PR.
- Describe what changed and how you verified it (there is no automated test suite yet — manual verification notes are valuable).
- Link any related issue. Security issues go through [SECURITY.md](SECURITY.md), **not** a public PR/issue.

By contributing you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).

## Credits

This is an MIT-licensed fork of [tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer). Please keep upstream attribution intact.
