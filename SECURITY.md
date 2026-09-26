# Security Policy

## Supported versions

| Version | Supported |
|---|---|
| Latest stable release | ✅ Yes |
| Older releases | ❌ No — please update first |
| Pre-releases (`-rc` / `-beta`) | ⚠️ Testing only |

PriconneReALLTL-Installer targets **Windows** running **.NET Framework 4.8**. Please reproduce on the latest release before reporting.

## Reporting a vulnerability

Please **do not** open a public issue for security problems.

Use GitHub's **private vulnerability reporting** on this repository:
**Security → Report a vulnerability** —
`https://github.com/HetCreep/PriconneReALLTL-Installer/security/advisories/new`.

Include:

- The **affected version** (shown bottom-right in the app).
- **Steps to reproduce.**
- An **impact assessment** (what an attacker gains).
- A **suggested fix**, if you have one.

### Response targets

| Stage | Target |
|---|---|
| Acknowledgement | ≤ 72 hours |
| Fix — Critical | ≤ 72 hours |
| Fix — High | ≤ 7 days |
| Fix — Medium | Next release |
| Fix — Low | Next major |

We credit reporters unless you ask otherwise. Please allow reasonable time for a fix before public disclosure. Issues in a **bundled dependency** (e.g. `Newtonsoft.Json`) that affect more than this project are handled via coordinated disclosure.

## Security posture

What the installer does (and doesn't) do with sensitive data and the system:

- **GitHub API token (optional).** A token only raises the GitHub API rate limit (60 → 5000 requests/hour). It is **stored encrypted with Windows DPAPI** (CurrentUser scope) in the app's per-user `user.config`, is **masked on screen**, is **redacted at the logging boundary** (so it cannot appear in a log even on error), and is sent only to `api.github.com` over HTTPS as an `Authorization` header. A **classic PAT with no scopes** is sufficient and recommended (all referenced repos are public). The app works without a token thanks to a ~6 h version-check cache.
- **No telemetry.** No analytics, crash-reporting, Discord, heartbeats, or fingerprinting. The complete outbound allow-list is in **[PRIVACY.md](PRIVACY.md)** — GitHub only.
- **Archive extraction is path-guarded.** Patch archives are extracted with a zip-slip guard: any entry whose resolved path escapes the game folder is skipped.
- **Downloads verified.** The patch `.zip` is checked against GitHub's published **SHA-256 asset digest** before any file is removed or extracted; a mismatch aborts without touching the install.
- **Removal is path-guarded.** Uninstall only deletes files that resolve **inside the game folder** (a tampered manifest cannot delete elsewhere), driven by a ref-counted install manifest.
- **Download sources are hardcoded in the exe, not the config.** Every repository URL (translation sources, modloader, self-update, external plugins) is compiled into the program. The editable `*.exe.config` contains no download URLs, so editing it cannot redirect the installer to a malicious source.
- **No elevation by default.** The app runs `asInvoker`; it operates on the game folder and its own per-user settings and does not require administrator rights.

## Hard No's

This tool will **never**:

- Patch, hook, or modify the game's own binaries, or any Cygames/DMM-shipped file.
- Read or write the game's process memory.
- Automate the game or send synthetic game-server requests (no botting / auto-anything).
- Handle game or DMM account credentials, or switch accounts.
- Bundle or redistribute Cygames/DMM game assets in what it downloads or deploys to your game (it installs only third-party translation patches).
- Add telemetry, analytics, crash-reporting, or any Discord integration.
- Contact any host outside the GitHub allow-list in [PRIVACY.md](PRIVACY.md).

> **Disclosed gap — UI art:** the installer's own interface art is currently Cygames-derived (inherited from the upstream project), which is at odds with the game-assets rule above. This is acknowledged honestly — like the unsigned-binary gap below — and is tracked to be re-skinned to original or open-licensed art.

### Note on BepInEx (mod loading)

The installer's purpose is to deploy **BepInEx** — a third-party mod loader that injects into the game — plus translation files, **into the game's `BepInEx` folder**. This is a **user-chosen modding action**, distinct from the "no injection / no automation" rules above, which concern the game's *own* code and servers. It is disclosed honestly: installing translation mods may violate the game's Terms of Service and can carry an **account-ban risk** (see the README disclaimer). The installer only ever writes published third-party files; it never modifies the game's binaries, reads its memory, or automates it.

## Build & distribution integrity

- **CI-only builds.** Every distributed artifact is built by **GitHub Actions** on a `v*` tag — never on a maintainer's machine. Local builds are for development only and are never published.
- **Checksums.** Each release attaches **`SHA256SUMS.txt`** and lists the SHA-256 inline in the release notes so you can verify your download (see [PRIVACY.md](PRIVACY.md) / README "Verifying your download").
- **Strong-name signed**, **not Authenticode-signed (yet).** The exe carries a strong name (committed `.snk`) for assembly identity, but is **not** Authenticode code-signed, so Windows SmartScreen may warn on first run. Until a signing certificate is obtained, **verify via SHA-256**. (Authenticode signing is a planned follow-up.)
- **Unique installer identity.** The Inno Setup installer uses a **unique AppId GUID** distinct from upstream `tynave/PriconneReTL-Installer`, so the two never collide; the installer does not clobber an upstream install.
- **Supply chain.** NuGet dependencies are pinned via `packages.config`; the only runtime dependency (`Newtonsoft.Json`) is embedded in the exe. No remote one-shot tooling is fetched at runtime. New dependencies require a supply-chain + license justification in the PR.

## Auto-update

The installer self-update is **notify-and-manual-apply**, never a silent in-place swap: when a newer release exists you choose a save location and run the new exe yourself. The startup check can be disabled (Settings → *Check for Installer Updates on Startup*). Because the release ships `SHA256SUMS.txt`, you can verify the downloaded exe before running it. (Authenticode verification will be added once the binary is signed.)

## Out of scope

- An **account ban** for using translation mods (an inherent, disclosed risk of modding).
- Attacks requiring **physical/local access** to your machine (including reading the DPAPI-encrypted token as your own Windows user).
- In-game breakage caused by a **third-party translation patch** itself (those patches are maintained in their own repositories — install only sources you trust).
- The security of the upstream **BepInEx** loader and the third-party translation patches themselves.

## See also

- **[PRIVACY.md](PRIVACY.md)** — exactly what leaves your machine (GitHub-only allow-list).
- **[README.md](README.md)** — disclaimer and download verification.
