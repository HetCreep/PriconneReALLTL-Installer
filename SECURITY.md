# Security Policy

## Supported versions

Only the latest release of PriconneReALLTL-Installer is supported. Please update before reporting an issue.

## Reporting a vulnerability

Please **do not** open a public issue for security problems.

Use GitHub's **private vulnerability reporting** on this repository:
**Security → Report a vulnerability** at
`https://github.com/HetCreep/PriconneReALLTL-Installer/security/advisories/new`.

Include what you found, steps to reproduce, and the affected version. We'll acknowledge and work on a fix; please allow reasonable time before any public disclosure.

## Security posture

What the installer does (and doesn't) do with sensitive data and the system:

- **GitHub API token (optional).** A token is only used to raise the GitHub API rate limit (60 → 5000 requests/hour). It is **stored encrypted with Windows DPAPI** (per-user) in the app's local `user.config`, is **never logged**, and is sent only to `api.github.com` over HTTPS. A **classic PAT with no scopes** is sufficient and recommended (all referenced repos are public). The app works without a token thanks to a ~6h version-check cache.
- **No telemetry.** The app makes no analytics or tracking calls. Network traffic is limited to GitHub (`api.github.com`, `raw.githubusercontent.com`, release asset downloads) and launching the game.
- **Archive extraction is path-guarded.** Patch archives are extracted with a zip-slip guard: any entry whose resolved path escapes the game folder is skipped, preventing path-traversal writes.
- **Downloads are from GitHub over HTTPS.** Patch, modloader, and plugin assets come from the configured source repositories' GitHub releases / raw content.
- **No elevation by default.** The app operates on the game folder and its own per-user settings; it does not require administrator rights for normal use.

## Scope

This project installs third-party BepInEx translation patches. The security of those patches and of the game itself is outside this project's control. Only install sources you trust.
