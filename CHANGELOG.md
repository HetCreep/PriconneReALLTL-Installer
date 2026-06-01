# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- **Selective update with modloader fallback** — update the TL patch and the modloader per source, and automatically fall back to ImaterialC's modloader when the selected source's modloader is missing or behind.

## [2.4.0] — first release of the rebranded fork

PriconneReALLTL-Installer is a rebranded, fully detached fork of
[tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer)
("ReALLTL" = supports **all** translation patches), with its own identity so the two
installers never collide.

### Added
- **Selectable translation source (EN / TH).** Switch between
  [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) and
  [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) from the main screen.
  Every patch URL derives from the selected source — adding a language is a single registry entry.
- **Per-source installed-version detection** — each source declares its own `Version.txt` path + regex.
- **Shortcut-wrapped launching** — "wrap" an existing launcher shortcut (DMM, DMMGamePlayerFastLauncher,
  or a PriconneMultiAccountLauncher account) so pressing it updates the patch, then launches that target. Reversible.
- **PriconneMultiAccountLauncher integration** — detected via its Inno Setup uninstall key (HKCU/HKLM) with an `%APPDATA%` fallback.
- **Per-source plugin profiles** — fixup plugins are toggled via a `.dll` ⇄ `.dll.bak` rename so only the
  active source's set loads; plugins maintained in a standalone repo are fetched from that repo's own GitHub release on install.
- **GitHub version-check caching (~6h)** so an API token is optional. Tokens are stored DPAPI-encrypted and never logged.
- **Strong-name signing** and a GitHub Actions release workflow (build + publish on `v*` tag).

### Changed
- Rebranded namespace / assembly / exe / solution / project; new GUIDs and strong-name key; self-update repointed to `HetCreep/PriconneReALLTL-Installer`.
- Modloader baseline pinned to ImaterialC (the canonical IL2CPP interop), independent of the selected TL source.
- Window now fits and centers to the screen working area; "TL Source" selector made prominent and on its own line.

### Fixed
- Operations (Update / Reinstall / Uninstall / Launch) and the auto-update shortcut flow are **no longer disabled** when the modloader-latest check fails — that check is now a soft, non-blocking warning.
- Versions are normalized (leading `v`/`V` stripped) before comparison and display.
- Fresh installs decide update-vs-install correctly (no more spurious 404 on a clean target).
- Archive extraction hardened with a zip-slip (path-traversal) guard.

### Attribution
- MIT-licensed fork of tynave/PriconneReTL-Installer; original inspiration touanu/PriconeTL_Updater. Translation patches by ImaterialC (EN) and PeterkleCG (TH).

[Unreleased]: https://github.com/HetCreep/PriconneReALLTL-Installer/compare/v2.4.0...HEAD
[2.4.0]: https://github.com/HetCreep/PriconneReALLTL-Installer/releases/tag/v2.4.0
