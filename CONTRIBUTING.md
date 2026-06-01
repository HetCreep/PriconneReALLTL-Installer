# Contributing to PriconneReALLTL-Installer

Thanks for your interest in improving the installer. This guide covers how to build, the conventions we follow, and how to submit changes.

## Project at a glance

- **Language / framework:** C# · WinForms · **.NET Framework 4.7.2** (no .NET Core / 5+)
- **Build system:** MSBuild (Visual Studio 2022) + NuGet
- **Output:** `PriconneReALLTLInstaller\bin\Release\PriconneReALLTLInstaller.exe`
- **Strong-name signed** with `PriconneReALLTLInstaller.snk` (committed so CI can sign)

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

## Releases

Releases are automated. Pushing a `v*` tag (e.g. `v2.4.0`) triggers `.github/workflows/release.yml`, which builds on `windows-latest`, signs, and publishes a GitHub Release with the built `.exe`. Use the `v` prefix — the workflow trigger and self-update both expect it.

## Submitting a PR

- Make sure the solution builds in `Release`.
- Describe what changed and how you verified it (there is no automated test suite yet — manual verification notes are valuable).
- Link any related issue.

## Credits

This is an MIT-licensed fork of [tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer). Please keep upstream attribution intact.
