> This file defines credential-handling standards for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Credential Vault Standard

Storage and lifecycle rules for the one secret this app handles: the **optional GitHub API token**.

## Scope

- The GitHub API token is the **only** credential. It is optional (the app works tokenless thanks to the version cache); when set, it raises the GitHub rate limit and validates the user's identity.
- There are no game credentials, no DMM tokens, no passwords. If a future change introduces any new secret, it falls under this file and must use the same DPAPI path.

## Storage — DPAPI CurrentUser Only

- Stored via .NET **DPAPI**: `ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser)`; read via `ProtectedData.Unprotect(..., DataProtectionScope.CurrentUser)`. The encrypted blob is base64 and saved as the `GithubAPIKey` setting in `user.config`.
- **`DataProtectionScope.CurrentUser` only.** `LocalMachine` is **forbidden** (would let any user on the box decrypt it).
- **Forbidden** storage: plaintext config, environment variables, registry plaintext, a third-party keystore/credential manager. DPAPI-in-`user.config` is the single approved path.
- The DPAPI `description`/entropy argument is **`null`** (generic) — do not put a username or any identifier in it (it can surface in some Windows audit views). Keep it generic.

## Validate After Acquire

- After the user enters/changes the token, validate it by calling `api.github.com/user` (`Helper.ValidateGitHubToken`); a valid result is **session-cached** so it is not re-hit on every version check.
- Reject/clear an obviously malformed value; surface "invalid or rate-limited" to the user without echoing the token.

## Use

- Decrypt **only at the moment of use** and pass straight into the `Authorization` header for an `api.github.com` request. Do not hold the decrypted token in a long-lived field.
- The token's **only** legitimate destination is an `Authorization` header to `api.github.com` — never any other host, never a query string, never a log (see telemetry-policy.md, log-sanitization.md).

## Clear / Scrub

- When the user clears the token in the UI, **overwrite the stored setting** (set `GithubAPIKey` empty and persist) so no stale encrypted blob lingers in `user.config`.
- The Inno uninstaller's `[UninstallDelete]` of `%LOCALAPPDATA%\PriconneReALLTLInstaller` removes `user.config` (and thus the blob) on uninstall.

## Never

| Pattern | Reason |
|---|---|
| Log the token, `Scrub`-bypass, or interpolate it into a message | Leak (see log-sanitization.md) |
| `ToString()` / `repr` a struct that exposes the token | Accidental dump |
| Put the token on the clipboard | Persists; other apps read it |
| Put the token in a window title | Visible in screenshots / alt-tab |
| Pass the token as a CLI / process / shortcut argument | Visible in Task Manager / `.lnk` properties |
| Show the token in an error dialog | Screen-share / screenshot leak |
| Store it with `LocalMachine` scope, or in plaintext/registry/env | Cross-user decryptable / unprotected |
| Transmit it to any host other than `api.github.com` | Exfiltration |
| Display it unmasked in the UI | The token field is masked input |

## Audit Trigger

Any change to the token code path — `Helper.EncryptString` / `DecryptString`, `ValidateGitHubToken`, the `GithubAPIKey` setting, the `GithubForm` token input, or any code that reads/writes the token — MUST be reviewed against this file before merge.

## Reference

- [log-sanitization.md](log-sanitization.md) — token redaction at the log boundary
- [telemetry-policy.md](telemetry-policy.md) — token only ever goes to `api.github.com`
- [native-windows-api.md](native-windows-api.md) — token never in process/shortcut args
- [../csharp/security.md](../csharp/security.md)
