> This file defines the telemetry/privacy policy for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Telemetry & Privacy Policy

Default and enforced posture: **zero telemetry.** No data leaves the user's machine except GitHub API/download traffic the user's own actions trigger.

## Why Zero

1. **Trust** — users may attach a personal GitHub token; the binary must demonstrably not phone home.
2. **Legal exposure** — collecting data about users of a game-modding tool raises privacy + unfair-competition concerns (see legal-boundary.md).
3. **Simplicity** — the tool needs no analytics to install patch files.

## Outbound Network — Allow-List Only

The installer makes outbound connections ONLY to these hosts, and to nothing else, ever:

| Host | Purpose |
|---|---|
| `api.github.com` | Release metadata, modloader `git/ref/tags`, raw version file, rate-limit info, token validation (`/user`) |
| `github.com` | Release page links, self-update release page |
| `objects.githubusercontent.com` | Release asset (patch zip / exe) download |
| `raw.githubusercontent.com` | Raw `version` file reads at a tag |

These four hosts serve the configured patch-source repos (EN `ImaterialC/PriconneRe-TL`, TH `PeterkleCG/PriconneTH`, future TH plugin `HetCreep/PriconneALLTLFixup`), the pinned modloader (ImaterialC), and self-update (`HetCreep/PriconneReALLTL-Installer`). Any other outbound destination in source is a **release blocker**.

> Process.Start to the DMM URI (`dmmgameplayer://…`) and to `explorer.exe` are local OS handoffs, not network egress by the installer — see native-windows-api.md.

## Forbidden

| Source | Status |
|---|---|
| Discord RPC / presence / webhook | **Forbidden** |
| Sentry / Rollbar / Bugsnag / any APM | Forbidden |
| Google Analytics / Plausible / Matomo / any analytics SaaS | Forbidden |
| Crash reporting that posts off-machine | Forbidden |
| Anonymous usage metrics | Forbidden, even with opt-in |
| Auto-submit of logs to any endpoint | Forbidden |
| Fonts / scripts / images loaded from a third-party CDN | Forbidden — bundle/embed locally |
| Heartbeat ping before the actual version check | Forbidden |
| Update check that sends a machine ID, hardware info, or any fingerprint | Forbidden |
| Query-param fingerprinting on any URL (`?os=…&id=…`) | Forbidden |

## Update / Version-Check Constraints

- The version check carries **no identifying query parameters**. Version info travels only in a static `User-Agent` header, which the GitHub API **requires** — that is the sole permitted "identifier", and it is constant, not per-user.
- User settings and the GitHub token are **never transmitted** anywhere except the token's single legitimate use: as an `Authorization` header to `api.github.com` (see credential-vault.md).
- Self-update never contacts a maintainer-controlled server — only the GitHub Releases hosts above.

## Local Data

- Logs (`ReALLTL*.log`) and the zip cache live locally (`%LOCALAPPDATA%\PriconneReALLTLInstaller`, cache under `%LOCALAPPDATA%\…\zipcache`).
- Logs are redacted per [log-sanitization.md](log-sanitization.md). No log uploader, no "share logs" button that posts off-machine. The user may copy logs and share them manually.
- The Settings menu "Clear Download Cache" deletes locally; it sends nothing.

## Telemetry Pseudo-Mechanisms (also forbidden)

- DNS/ICMP heartbeats to any dev-controlled host.
- A "statistics" page that fetches from a maintainer URL even if the response is discarded.
- Any auto-translation / spell-check / grammar service call for user text.
- Opening the user's browser to a URL carrying a machine ID.

## PR Audit Triggers

Every PR that:

- adds or changes any `http://` / `https://` URL literal, **or**
- adds a new outbound-capable dependency (anything that can open a socket / make HTTP calls),

MUST list in its description the outbound host(s) it touches and confirm each is in the allow-list above. A genuinely new host requires a [legal-boundary.md](legal-boundary.md) review + an explicit maintainer ack before merge.

## User-Facing Disclosure

`README.md` and the in-app About dialog state:

> This installer contains no telemetry, analytics, or crash reporting. It connects only to GitHub (api.github.com, github.com, objects.githubusercontent.com, raw.githubusercontent.com) to fetch translation patches and check for updates. You can disable update checks in Settings.

## Reference

- [log-sanitization.md](log-sanitization.md)
- [credential-vault.md](credential-vault.md) — the token never leaves the machine except as a GitHub auth header
- [legal-boundary.md](legal-boundary.md)
- [distribution-security.md](distribution-security.md) — installer makes no network calls
