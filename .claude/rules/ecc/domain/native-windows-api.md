> This file defines native-Windows-API standards for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Native Windows API Standard

Rules for the registry, COM/shell, process launch, file install/uninstall, and DLL loading. Misuse here causes crashes, security holes, or silent data corruption.

## Registry — Read-Only, Minimum Access

- The installer treats the registry as **read-only**. Its only registry use is **detection** (locating the game install and launcher backends, e.g. the PriconneMultiAccountLauncher Inno uninstall key, DMM/DGPFL paths).
- Open keys with the **minimum** access — `RegistryKeyPermissionCheck.ReadSubTree` / read rights only. Never request write/all-access for a read.
- **`HKLM` is read-only**; never write it. Do not write `HKCU` either — there is no persistent installer state in the registry (settings live in `user.config`).
- Dispose every opened key — wrap in `using` (`using (var key = Registry.LocalMachine.OpenSubKey(path, writable:false)) { … }`). A null result (missing key) is a normal "not found", not an error.
- Treat every value read as untrusted external input: null-check, validate the path exists (`File.Exists`/`Directory.Exists`) before using it.

## COM / Shell — Shortcut Wrap & Restore

Shortcut (`.lnk`) wrap/restore uses WSH `IWshShortcut` (via COM `WScript.Shell`) / `IShellLink`.

- COM that touches the shell must run on an **STA** thread (WinForms UI thread is STA — keep shortcut ops there, or initialize STA explicitly for a worker).
- **Never hand-write `.lnk` bytes.** The format is version-fragile; always go through the shell wrapper.
- Reading a `.lnk`: read `TargetPath` / `Arguments` / `WorkingDirectory` / `IconLocation`, transform, `Save()`.
- **Quote arguments**; pass the wrapped payload as base64 (`--launch <b64> [--targs <b64>] [--tdir <b64>]`) so spaces/special chars survive — never raw-concatenate untrusted strings into the args line.
- **Never embed a secret** (e.g. the GitHub token) in shortcut arguments — they are visible in the `.lnk` properties.
- **No Startup-folder entries** and no persistence shortcuts without explicit user opt-in (the installer creates none today).

## Process Launch — `ProcessStartInfo`, Never Shell Strings

`Process.Start` is permitted **only** for these argument shapes, each as a discrete value (never a shell-concatenated command line):

| Allowed target | Form |
|---|---|
| A **hardcoded HTTPS URL** (release page, wiki, latest) | `Process.Start("https://github.com/HetCreep/PriconneReALLTL-Installer/…")` |
| The **DMM URI** | `Process.Start("dmmgameplayer://play/GCL/priconner/cl/win")` |
| **explorer.exe** (open a folder) | `new ProcessStartInfo("explorer.exe") { Arguments = <validated path> }` |
| A **base64-decoded wrapped-shortcut target** | `new ProcessStartInfo { FileName = launchTarget }` where `launchTarget` came from the decoded `--launch` payload |

Rules:

- Always use `ProcessStartInfo` with the file name and arguments as **separate fields** — never build one shell string, never `cmd /c …`, never `UseShellExecute` to run an interpolated command line.
- Validate any path before launching `explorer.exe` against it (exists, inside an expected root).
- Do not pass the GitHub token (or any secret) as a process argument.

## File Install / Uninstall — Path-Guarded + Ref-Counted Manifest

- All extract/remove operations are **path-guarded to the game folder**. Before writing/deleting, resolve the full path and confirm it stays inside the game directory; reject `..\`, absolute, or escaping paths (the **zip-slip guard** in extraction, and the same guard on removal so a tampered manifest can't delete outside).
- Uninstall is driven by the **ref-counted manifest** `BepInEx\.priconnerealltl-manifest.json` (path → owning sources). A file is deleted only when its owner-set empties; shared engine files stay while another source needs them. Corrupt/missing manifest → fall back to the ProcessTree removal, then rewrite a fresh manifest (self-heal).
- **Mark the manifest `Hidden`** (clear the attribute before each overwrite, re-apply after) so users don't stumble on or delete it. Its loss is never fatal — it is advisory metadata.
- Verify the patch zip's **SHA-256 against GitHub's published asset digest** before touching the install; mismatch → delete + don't extract (post-download) or re-download (cached). No digest → don't block (structural corruption still throws in `ZipFile.OpenRead`).

## DLL Hijacking Defense

- **Newtonsoft.Json is embedded in the exe** (EmbeddedResource + `AppDomain.AssemblyResolve`) so it is not loaded as a loose DLL next to the exe — single self-contained file, no plant-a-DLL vector.
- Do not add relative-path / working-directory DLL loads. Any future native dependency must load by absolute path or be embedded the same way.

## Power & Sleep

- The installer does **not** block sleep or shutdown (no `SetThreadExecutionState`, no shutdown-block reason). It is a foreground utility; let the OS sleep/shut down normally.

## Anti-Patterns

| Pattern | Reason |
|---|---|
| Opening a registry key writable for a read | Excess privilege; risks accidental write |
| Writing any registry value | Installer is registry-read-only |
| Not disposing a `RegistryKey` (no `using`) | Handle leak |
| Hand-building `.lnk` bytes | Version-fragile, breaks across Windows builds |
| Token or secret in shortcut args / process args | Visible in `.lnk` properties / Task Manager |
| `Process.Start` with a concatenated shell string / `cmd /c <interpolation>` | Command injection |
| Extract/remove without the path guard | Zip-slip / out-of-folder deletion |
| Loading Newtonsoft (or any dep) as a loose adjacent DLL | DLL-hijack surface |

## Reference

- [credential-vault.md](credential-vault.md) — DPAPI; token never in args
- [legal-boundary.md](legal-boundary.md) — files-only, registry read-only
- [distribution-security.md](distribution-security.md) — embedded Newtonsoft is a build step, not a checked-in blob
- [../csharp/security.md](../csharp/security.md)
