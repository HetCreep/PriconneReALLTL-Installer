> This file defines log-sanitization standards for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Log Sanitization

Every log line is a potential credential leak. This file defines what must be redacted, how, and where.

## Principle

- The only secret in this app is the **optional GitHub API token**. It MUST never appear in logs — not in errors, not in stack traces, not in a URL, not in an HTTP dump.
- Redaction happens at the **logging boundary** (`LoggerFunctions.LogRedactor.Scrub`), not at call sites. Call sites must not have to remember to redact.
- Redaction is **fail-closed**: when a value matches a token shape, redact it. A `[REDACTED]` line is always preferable to a leaked token.
- A user may screenshot a log, paste it into a GitHub issue, or sync it to the cloud. Assume every logged line becomes public.

## What Gets Redacted

| Class | Examples | Replacement |
|---|---|---|
| GitHub token shapes | `ghp_…`, `gho_…`, `ghu_…`, `ghs_…`, `ghr_…` (classic), `github_pat_…` (fine-grained) | `[REDACTED]` |
| `Authorization` header values | `Bearer <token>`, `token <token>` | `[REDACTED]` |
| Any string that matches the token regex anywhere in the line | (substring match, not just `key=value`) | `[REDACTED]` |

`LogRedactor.Scrub` is invoked by **every** `Logger.Log` / `Logger.Error` overload (file sink and UI/status sink) before the line is written. Adding a new log sink REQUIRES routing it through `Scrub` too.

## What Is Kept (so logs stay useful)

- HTTP status codes (`403`, `404`, `200`, rate-limit info)
- The endpoint **path without query string** (e.g. `api.github.com/repos/<owner>/<repo>/releases/latest`)
- Response sizes, durations, retry counts
- Exception types and sanitized messages
- Version strings, file counts, extract/remove summaries, SHA-256 digests of patch zips (public release data, not secret)

## Implementation Rules

- The token is stored DPAPI-encrypted; **decrypt only at the moment of use** and pass it straight into the `Authorization` header — never assign it to a variable that gets logged or interpolated into a message string.
- Never log a full `HttpResponseMessage` / `HttpRequestMessage` (headers carry `Authorization`).
- Never build a log message with the token via `$"…{token}…"` or string concat — even "temporarily for debugging".
- Consider masking the **Windows username** inside logged file paths (`C:\Users\<name>\…`) where it adds no diagnostic value — it is PII a user may not want in a pasted log. (The token is the hard requirement; username masking is recommended.)
- Surface user-facing errors without the token: show "GitHub request failed (HTTP 403 — rate limited)", not the raw request.

## Forbidden Logging Patterns

| Pattern | Reason |
|---|---|
| `logger.Log($"token={token}", …)` | Interpolated before `Scrub` can't save a value the regex misses; never put the token in a message at all |
| `Console.WriteLine(token)` / `Debug.WriteLine(token)` | Bypasses the logger + redaction entirely |
| Logging the raw `HttpResponseMessage` or request headers | Contains the `Authorization` header |
| Logging the decrypted token to "verify it loaded" | Direct leak |
| A new log/trace sink that does not call `LogRedactor.Scrub` | Bypasses the boundary |

## Pre-Release Audit

Before tagging a release:

1. Run a real session with a real GitHub token set (do a few API-hitting actions: version check, rate-limit info, an install).
2. Open the produced `ReALLTL*.log` and search for fragments of that token (first ~12 chars, the `ghp_`/`github_pat_` prefix, and the literal `Authorization`/`Bearer`).
3. Any hit → release blocked; fix the redaction path; re-test.
4. Record the audit result in the release PR / notes.

## Reference

- [credential-vault.md](credential-vault.md) — what counts as a credential (the GitHub token)
- [telemetry-policy.md](telemetry-policy.md) — logs never leave the machine
- [../csharp/security.md](../csharp/security.md)
