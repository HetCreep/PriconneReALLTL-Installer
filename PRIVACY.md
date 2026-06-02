# Privacy Policy

**PriconneReALLTL Installer** is a local, single-user desktop utility. It runs entirely on
your machine and does **not** operate any server, account system, or backend that belongs to
this project. There is **no telemetry of any kind**.

This document explains exactly what leaves your computer (a short, GitHub-only list), what is
stored locally, and what we deliberately do **not** collect.

> **TL;DR** — Zero telemetry. The only outbound traffic is to GitHub, to check for and download
> patch/installer updates. The single optional secret (a GitHub API token) is encrypted with
> Windows DPAPI, never logged, and never transmitted to anyone but `api.github.com`. If you see
> the app contact any host not in the table below, that is a bug — please report it.

---

## Local-first by design

- The installer is a portable / per-user Windows application. All work (downloading patches,
  extracting files, editing the game's `BepInEx` folder, managing shortcuts) happens on your
  device.
- We do **not** run any analytics service, account service, license server, "phone-home"
  endpoint, or crash-reporting backend.
- Nothing about you, your machine, your game, or your usage is sent anywhere — **except** the
  GitHub fetches listed below, which exist solely to download the software and patches you
  asked for.

---

## Outbound network — complete allow-list

These are the **only** hosts the application ever contacts. There is no other network activity.

| Host | Purpose | Trigger |
|---|---|---|
| `api.github.com` | Read public release/version metadata for the selected translation source, the modloader, and the installer's own updates; read the published SHA-256 asset digest for download verification; raise the request rate limit when you supply a token. | App startup / on-demand version check / self-update check / before verifying a downloaded zip. |
| `github.com` | Download a translation-patch release `.zip`; download the installer self-update `.exe`. | When you install/update/reinstall a patch, or apply a self-update. |
| `objects.githubusercontent.com` | GitHub's redirect target that actually serves release-asset binaries (the `.zip` / `.exe` above). | Same as `github.com` downloads (GitHub redirects asset requests here). |
| `raw.githubusercontent.com` | Read a per-source raw version file (e.g. the modloader interop `version`) directly from a source repository. | During a version check for the selected source / pinned modloader. |

All requests are HTTPS. **Anything else is a bug** — the app has no code path that contacts any
other host. If you observe traffic to a domain not in this table, please file a report (see
[Reporting a privacy concern](#reporting-a-privacy-concern)).

---

## No trackers, no analytics — explicitly

The application contains **none** of the following, in any form:

- No analytics SDK (no Google Analytics, no PostHog, no Mixpanel, no Amplitude, no Segment).
- No crash/error-reporting SDK (no Sentry, no Bugsnag, no Application Insights).
- No Discord Rich Presence, Discord webhooks, or any Discord integration.
- No heartbeat, beacon, "check-in", or background phone-home of any kind.
- No advertising, attribution, or A/B-testing frameworks.
- No machine fingerprinting, hardware IDs, install IDs, or user IDs.

---

## What we do **NOT** collect

| We do **not** collect / transmit | Notes |
|---|---|
| Email address or any contact info | The app has no account, sign-up, or login. |
| Your GitHub token | Stored **only** locally (encrypted, see below). It is sent **only** to `api.github.com` as an `Authorization` header to raise your rate limit — never to us or any third party. |
| Hardware ID / device fingerprint | Never generated or read. |
| Machine ID, install ID, or user ID | None exist. |
| IP address logs | We run no server, so there is nothing to log your IP against. |
| Crash dumps / stack traces | Never uploaded. Errors are written to the **local** log file only. |
| Usage / analytics / feature-interaction events | None are produced. |
| Game data, save data, or account credentials | The app never reads, stores, or transmits these. |

---

## The one stored secret: your GitHub token (optional)

The app needs **no** secrets to function. The single **optional** secret is a GitHub API token,
which only raises the unauthenticated rate limit (60 → 5,000 requests/hour). A classic PAT with
**no scopes** is sufficient because every repository the app reads is public.

How it is handled:

- **Encrypted at rest** with the Windows Data Protection API (**DPAPI, CurrentUser scope**) inside
  the per-user `user.config`. Only your Windows user account on this machine can decrypt it.
- **Masked on screen** — entered in a password field; the characters are not shown.
- **Never logged** — the token is redacted at the logging boundary, so it cannot appear in the
  log file even on errors.
- **Transmitted only to `api.github.com`** over HTTPS, as the standard `Authorization` header.

You can leave the token blank. Version checks are cached locally (~6 hours for patch/modloader
versions, ~7 days for the installer self-update check), so the app remains usable without one.

---

## Update checks (full disclosure)

The app checks for two kinds of updates, both against **GitHub releases only**:

- **Patch / modloader versions** — read from the selected source's GitHub release metadata and a
  raw version file. Cached locally for **~6 hours** to minimize requests.
- **Installer self-update** — checks this project's own GitHub release
  (`HetCreep/PriconneReALLTL-Installer`). Cached for **~7 days**.

Key facts:

- The self-update **never** performs a silent in-place swap. When a newer installer is available,
  **you** choose a save location and run the new executable yourself (manual apply).
- The startup auto-check can be turned **off** via the Settings toggle
  **"Check for Installer Updates on Startup"**.
- You can run a one-time live check at any time via **"Check for Updates Now"**, which bypasses the
  cache.

No update check transmits anything about you — it only reads public release metadata.

---

## Local data & your right to be forgotten

The app stores a small amount of data **locally only**:

- **Settings** (including the DPAPI-encrypted token, your selected TL source, and toggles) in the
  per-user `user.config` under `%LOCALAPPDATA%\PriconneReALLTLInstaller\`.
- **A download (zip) cache** to avoid re-downloading large patch archives.
- **Log files** (e.g. `ReALLTLInstaller.log`) next to the executable / in the install folder.
- An install **manifest** inside the game's `BepInEx` folder (used for safe, ref-counted
  uninstalls).

To remove everything:

- The bundled (Inno Setup) uninstaller, by default, **preserves** your settings/cache so a
  reinstall keeps your configuration. To wipe it manually, delete:
  - `%LOCALAPPDATA%\PriconneReALLTLInstaller\` (settings + token + version cache), and
  - the download (zip) cache — clearable in-app via **Settings → Clear Download Cache**, or by
    deleting the cache folder it reports.
- To remove the patch from the game itself, use the app's **Uninstall** operation (it follows the
  install manifest), then delete the items above.

Because no data ever leaves your machine, deleting these local files is a complete erasure — there
is no server-side copy of anything.

---

## Children's privacy

The app collects no personal data from anyone, including children. It has no accounts and no data
collection.

---

## Changes to this policy

If the data-handling behavior ever changes, this document and the
[allow-list table](#outbound-network--complete-allow-list) will be updated in the same release.
The allow-list is the authoritative statement of what the app may contact.

---

## Reporting a privacy concern

- For a suspected privacy or data-handling issue, open a
  [GitHub issue](https://github.com/HetCreep/PriconneReALLTL-Installer/issues).
- If the concern is also a **security** vulnerability (e.g. token leakage), please use private
  reporting instead — see [SECURITY.md](SECURITY.md)
  (**GitHub Security Advisories**, not a public issue).

See also: [README.md](README.md) · [SECURITY.md](SECURITY.md)
