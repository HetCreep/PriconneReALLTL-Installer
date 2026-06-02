# <img src="PriconneReALLTLInstaller/Resources/Item_Jewel_Art.ico"> PriconneReALLTL Installer

A WinForms installer/updater GUI for **Princess Connect! Re:Dive** BepInEx translation patches — supporting **multiple, user-selectable translation sources (English / ไทย)** and one-click *update + play* launch shortcuts.

**"ReALLTL"** = supports **all** translation patches. A rebranded, fully detached fork of [tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) with its own identity (name, GUIDs, strong-name key, self-update URL) so the two installers never collide.

---

## 🌟 Features

* **Selectable translation source (EN / TH)** — switch between [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) and [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) right from the main screen. Every patch URL derives from the selected source — adding another source is a single list entry.
* **Shortcut-wrapped launching** — the GUI focuses on patching. To play, "wrap" an existing launcher shortcut (DMM, [DMMGamePlayerFastLauncher](https://github.com/fa0311/DMMGamePlayerFastLauncher), or a [PriconneMultiAccountLauncher](https://github.com/HetCreep/PriconneMultiAccountLauncher) account shortcut). Pressing the wrapped shortcut updates the patch, then launches that exact target — one click, *update + play*, per account. Reversible (un-wrap restores the original).
* **PriconneMultiAccountLauncher integration** — auto-detected via its Inno Setup uninstall key (HKCU/HKLM) with an `%APPDATA%` fallback.
* **Modloader pinned to ImaterialC** — the BepInEx IL2CPP interop baseline always comes from the canonical ImaterialC release, independent of the chosen TL source.
* **Per-source plugin profiles** — each TL source declares which BepInEx fixup plugins it uses; installing or switching a source toggles the rest off via a `.bak` rename (English loads `PriconneSkillTLFixup`/`PriconneTLFixup`; other languages shelve those and load their own). Every fixup DLL stays on disk — only the active source's set loads. Plugins maintained in a standalone repo are fetched from that repo's own GitHub release on install.
* **Rate-limit friendly** — GitHub version checks are cached (~6h), so a GitHub API token is *optional* for typical use. If set, the token is stored encrypted (Windows DPAPI), never logged.
* **Clean cloud builds** — strong-name signed, GitHub Actions MSBuild release workflow; ClickOnce manifest signing disabled.

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

## 🛠️ Credits & Attribution

MIT-licensed fork. Heartfelt thanks to:

* [PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) by [tynave](https://github.com/tynave) — the upstream installer this is forked from
* [PriconeTL_Updater](https://github.com/touanu/PriconeTL_Updater) by [touanu](https://github.com/touanu) — original inspiration
* Translation patches: [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) · [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย)
* All in-game assets are the property of CyberAgent, Inc. / Cygames, Inc. and their respective creators.

### 🤖 Built with AI Co-Engineers

- **Claude Code** (Anthropic) — C# rebrand + detachment, multi-source EN/TH, shortcut-wrap launch architecture, per-source version detection, rate-limit caching, build/CI.
- **Antigravity** (Google DeepMind) — earlier native multi-launcher UI, ClickOnce deactivation, and GitHub Actions cloud compilation.
