# <img src="PriconneReALLTLInstaller/Resources/jewel.ico" width="28"> PriconneReALLTL Installer

[![Latest release](https://img.shields.io/github/v/release/HetCreep/PriconneReALLTL-Installer?sort=semver&display_name=tag)](https://github.com/HetCreep/PriconneReALLTL-Installer/releases/latest)
[![Build](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/release.yml/badge.svg)](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/release.yml)
[![CodeQL](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/codeql.yml/badge.svg)](https://github.com/HetCreep/PriconneReALLTL-Installer/actions/workflows/codeql.yml)
![License: MIT](https://img.shields.io/badge/license-MIT-green)
![.NET Framework 4.8](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)

**📖 Read this in:** English · [ไทย](README.th.md)

A WinForms installer/updater GUI for **Princess Connect! Re:Dive** BepInEx translation patches — supporting **multiple, user-selectable translation sources (English / ไทย)** and one-click *update + play* launch shortcuts.

**"ReALLTL"** = supports **all** translation patches. A rebranded, fully detached fork of [tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) with its own identity (name, GUIDs, strong-name key, self-update URL) so the two installers never collide.

---

## 🌟 Features

* **Selectable translation source (English / ไทย)** — switch between [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) and [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) right from the main screen. Every patch URL derives from the selected source — adding another source is a single list entry.
* **Shortcut-wrapped launching** — the GUI focuses on patching. To play, "wrap" an existing launcher shortcut (DMM, [DMMGamePlayerFastLauncher](https://github.com/fa0311/DMMGamePlayerFastLauncher), or a [PriconneMultiAccountLauncher](https://github.com/HetCreep/PriconneMultiAccountLauncher) account shortcut). Pressing the wrapped shortcut updates the patch, then launches that exact target — one click, *update + play*, per account. Reversible (un-wrap restores the original).
* **PriconneMultiAccountLauncher integration** — auto-detected via its Inno Setup uninstall key (HKCU/HKLM) with an `%APPDATA%` fallback.
* **Modloader pinned to ImaterialC** — the BepInEx IL2CPP interop baseline always comes from the canonical ImaterialC release, independent of the chosen TL source.
* **Safe installs & clean uninstalls** — the downloaded patch `.zip` is **SHA-256 verified** against GitHub's published digest before any file is touched; extraction is **zip-slip guarded**; uninstall is **ref-counted** (with both English and ไทย installed, removing one keeps the other and the shared modloader working) and **path-guarded** to the game folder.
* **Rate-limit friendly** — GitHub version checks are cached (~6 h; the installer self-update check ~7 days), so a GitHub API token is *optional*. If set, the token is stored encrypted (Windows DPAPI), masked on screen, and never logged.
* **Verifiable cloud builds** — built **only** by GitHub Actions on a `v*` tag; each release attaches **`SHA256SUMS.txt`**. Strong-name signed (not yet Authenticode-signed — verify via SHA-256, see below).

---

## 💾 Download

**[Latest release →](https://github.com/HetCreep/PriconneReALLTL-Installer/releases/latest)**

Each release ships two options:

| Asset | What it is |
|---|---|
| **`PriconneReALLTLInstaller-<version>.exe`** | Portable, single file — run it anywhere, nothing to install. This is also what the built-in self-update downloads. |
| **`PriconneReALLTLInstaller-<version>-Setup.exe`** | Per-user installer (no admin). Adds a Start Menu shortcut and an uninstall entry that also clears the app's local cache/settings. Self-update still works (it installs under your user profile). |

Either is fine — the portable exe if you just want to run it, the setup if you prefer a Start Menu entry and a clean uninstall.

---

## ✅ Verifying your download

Every release attaches **`SHA256SUMS.txt`** and lists the SHA-256 inline in the release notes. Verify the file you downloaded:

```powershell
Get-FileHash -Algorithm SHA256 .\PriconneReALLTLInstaller-v3.0.4.exe
```

Compare the output against the hash in `SHA256SUMS.txt` / the release notes — they must match exactly.

> The binaries are **strong-name signed but not Authenticode code-signed (yet)**, so Windows **SmartScreen** may show *"Windows protected your PC"* on first run. Choose **More info → Run anyway** only if the SHA-256 matches. Authenticode signing is a planned improvement.

Only download from the **[official Releases page](https://github.com/HetCreep/PriconneReALLTL-Installer/releases)** — never a third-party mirror, Discord, or direct message.

---

## ⚙️ Good to know

- **Launching the game.** The main **Launch Game** button starts the game through **DMM** (a plain launch). For *per-account* play or one-click **update + play**, **wrap** an existing launcher shortcut (DMM / DMMGamePlayerFastLauncher / a PriconneMultiAccountLauncher account) or create an **AutoUpdater shortcut** from the toolbar — pressing it updates the patch, then launches that target.
- **Settings menu.** Besides the startup-update toggle, it holds: **Clear Download Cache**, **Edit Ignored Files** (protect files from being overwritten/removed on update), **Import / Export Settings**, **GitHub API Settings** (the optional token), and **Rate-Limit Info**.
- **Uninstalling.** Removing the app (Windows / Setup uninstall) clears *its own* data — settings, token, cache, logs — but **does not remove the patch from your game**. Run the app's **Uninstall** operation first to remove the translation, *then* uninstall the app.

## 🔐 Privacy & Security

- **[PRIVACY.md](PRIVACY.md)** — zero telemetry; the complete, GitHub-only outbound allow-list.
- **[SECURITY.md](SECURITY.md)** — security posture, the "Hard No's", and how to report a vulnerability privately.

---

## ⚠️ Disclaimer

This is an **unofficial, fan-made** tool. It is **not affiliated with, endorsed by, or associated with** Cygames, Inc., DMM, or tynave. *Princess Connect! Re:Dive* and all related names and assets are trademarks / property of their respective owners, used here **nominatively** for identification only.

This installer deploys **third-party translation mods** (via the BepInEx mod loader) into your game. **Modifying the game may violate its Terms of Service and could put your account at risk of suspension or ban.** You install and use these modifications **at your own risk**.

The software is provided **"AS IS", without warranty of any kind**. The authors accept **no liability** for any account action, data loss, or damage arising from its use. See the [LICENSE](LICENSE.txt).

---

## 🛠️ Credits & Attribution

MIT-licensed fork. Heartfelt thanks to:

* [PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) by [tynave](https://github.com/tynave) — the upstream installer this is forked from
* [PriconeTL_Updater](https://github.com/touanu/PriconeTL_Updater) by [touanu](https://github.com/touanu) — original inspiration
* Translation patches: [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) · [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย)
* All in-game assets are the property of CyberAgent, Inc. / Cygames, Inc. and their respective creators.

### 🤖 Built with AI Co-Engineers

- **Claude Code** (Anthropic) — C# rebrand + detachment, multi-source English/ไทย, shortcut-wrap launch architecture, per-source version detection, rate-limit caching, build/CI.
- **Antigravity** (Google DeepMind) — earlier native multi-launcher UI, ClickOnce deactivation, and GitHub Actions cloud compilation.
