> This file defines the legal/ethical engineering boundary for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md. **This is engineering policy, not legal advice.**

# Legal Boundary

Defines what the installer is allowed to do and what is categorically forbidden. It reflects the maintainers' precautionary risk posture, not a lawyer's opinion.

## Context

The tool installs community **translation patches** (BepInEx mods) for Cygames' Princess Connect! Re:Dive, distributed via DMM Game Player (DMM.com LLC, Japan). The patches are third-party, published on GitHub. The installer fetches, verifies, and deploys them; it does not create or own the translation content.

## What The Installer Does — Allowed

| Action | Why allowed |
|---|---|
| Read the user's own machine/registry **read-only** to detect the game folder + launcher | User inspecting their own system |
| Download a **published** third-party translation patch + modloader from GitHub Releases | Redistribution by the patch authors; we only fetch what they published |
| Write those published translation files into the game's `BepInEx\` folder | User-chosen modding of their own install (see BepInEx reconciliation) |
| Uninstall those same files (path-guarded, ref-counted manifest) | Reversing the user's own action |
| Wrap / restore a launcher `.lnk` shortcut on the user's machine | Standard Windows shell editing |
| Self-update the installer from its own GitHub Releases | Standard software update |

## BepInEx Reconciliation (read before citing any "never inject DLLs" rule)

Generic security/legal guidance says "never inject DLLs into the game." The installer's **core purpose** is deploying BepInEx — a mod loader that *does* inject. We do not pretend otherwise; we frame it precisely:

- BepInEx deployment is a **user-chosen modding action**, clearly disclosed, NOT a covert or automated one. It is a deliberate, documented capability of the tool — **not** a flat "Hard No."
- The installer only ever writes **published third-party translation files** into `BepInEx\`. It does **not** patch the game's own binaries, does **not** read game memory, and does **not** automate the game.
- The disclosure obligation is real: the README/About MUST state this may violate DMM/Cygames ToS and may result in account suspension/ban. That risk is the user's informed choice.
- This is categorically distinct from botting/automation (forbidden below). Modding ≠ cheating; we ship the former, never the latter.

## What The Installer Does Not Do — Categorically Forbidden

| Action | Why forbidden |
|---|---|
| Patch, hook, or modify the game's own binaries (`PrincessConnectReDive.exe` / Cygames-shipped files) | TPM circumvention / derivative work — we only add files under `BepInEx\`, never alter game binaries |
| Read or scan game-process memory | Data scraping of copyrighted content / TPM |
| Automate or bot the game (auto-battle, auto-quest, auto-gacha, daily-login automation) | Cygames ToS violation; arguably unauthorized access |
| Bypass region lock, age verification, or payment verification | TPM / unfair competition |
| Reverse-engineer or decrypt Cygames game assets | Copyright |
| Redistribute Cygames-shipped assets (icons, art, audio, asset bundles) | Copyright — none ship in this repo |
| Sell access, paywall the installer, or gate it behind donations tied to features | Unfair competition |

## Acceptance Test (before merging code that touches the game folder)

- **"Files only, binaries untouched?"** — does the game's own binary on disk stay bit-identical to what was shipped, with our changes confined to `BepInEx\`? Must be yes.
- **"Published content only?"** — is every file we write something a third-party author already published on GitHub? Must be yes.
- **"No automation against the game server?"** — does the change cause any request to Cygames/DMM game servers without the user acting? Must be no.
- **"No new outbound host?"** — does it add traffic to any host outside the egress allow-list? Must be no (see telemetry-policy.md).

Any failing answer blocks the merge.

## License & Trademark

- The project is an **MIT fork of `tynave/PriconneReTL-Installer`**. Keep tynave's MIT copyright in `LICENSE` (legal requirement) plus the HetCreep fork notice.
- "Princess Connect! Re:Dive", "プリコネ" and related marks are trademarks of **Cygames Inc.** "DMM", "DMM GAMES", "DMM Game Player" are trademarks of **DMM.com LLC**.
- Use those names **nominatively only** (to describe what the tool works with), never as branding. No Cygames/DMM logos in repo banner, installer art, or UI.

## User-Facing Disclaimer (required)

`README.md` and the in-app **About** dialog MUST carry a clear unofficial / no-affiliation / AS-IS notice. Template:

> This is an unofficial third-party installer for community translation patches. It is not affiliated with, endorsed by, or supported by Cygames Inc. or DMM.com LLC. Installing these patches (which use the BepInEx mod loader) may violate the game's or DMM's Terms of Service and may result in account suspension. The maintainers provide this software AS-IS with no warranty and accept no liability for any account action taken by Cygames or DMM.

## Engineering Implications

| Implication | Source |
|---|---|
| No memory-scan / game-binary-patch / botting module ever ships | TPM + ToS rules |
| File ops are path-guarded to the game folder + ref-counted manifest; never touch game binaries | "files only" rule |
| Registry access is read-only (detection) | "read your own machine" stays read-only |
| No Cygames/DMM assets in the repo | Copyright |
| Cannot sell, paywall, or tie donations to features | Unfair competition |

## Reference

- [telemetry-policy.md](telemetry-policy.md) — no data harvesting; egress allow-list
- [distribution-security.md](distribution-security.md) — signing/integrity (Authenticode gap disclosed)
- [native-windows-api.md](native-windows-api.md) — path-guarded file ops, read-only registry
- [../csharp/security.md](../csharp/security.md)
