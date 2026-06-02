# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

_Nothing yet._

## [3.0.0] — first public release

PriconneReALLTL-Installer is a rebranded, fully detached fork of
[tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer)
("ReALLTL" = supports **all** translation patches), with its own identity so the two
installers never collide. This is the first version published as a GitHub Release
(the earlier 2.x line was internal, dev-only).

### Added
- **Selectable translation source (EN / TH).** Switch between
  [ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL) (English) and
  [PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH) (ไทย) from the main screen.
  Every patch URL derives from the selected source — adding a language is a single registry entry.
- **Per-source installed-version detection** — each source declares its own `Version.txt` path + regex.
- **Per-user Inno Setup installer** alongside the portable exe — no admin, Start Menu shortcut, and a clean
  uninstall that also clears the app's local cache/settings. Self-update keeps working (per-user install).
- **SHA-256 verify-before-touch** — the downloaded patch zip is verified against GitHub's published digest
  before any file is removed or extracted, so a corrupt/interrupted download can't half-overwrite a working install.
- **Local zip cache** — the ~330 MB patch zip is reused on repeat installs / source switches (no re-download).
- **Ref-counted uninstall (install manifest)** — with EN+TH installed, uninstalling one keeps the other and the
  shared modloader working; uninstalling the last source removes everything.
- **Per-source `AutoTranslatorConfig.ini` sync** — language + texture list follow the active source automatically.
- **Shortcut-wrapped launching** — "wrap" an existing launcher shortcut (DMM, DMMGamePlayerFastLauncher,
  or a PriconneMultiAccountLauncher account) so pressing it updates the patch, then launches that target. Reversible.
- **PriconneMultiAccountLauncher integration** — detected via its Inno Setup uninstall key (HKCU/HKLM) with an `%APPDATA%` fallback.
- **GitHub version-check caching** so an API token is optional; the installer self-update check caches for 7 days.
  Tokens are stored DPAPI-encrypted and never logged. A **Check for Updates Now** menu action runs an on-demand live check.
- **Strong-name signing** and a GitHub Actions release workflow: builds the portable exe + installer, attaches
  `SHA256SUMS.txt`, and keeps installer assets only on the latest release.

### Changed
- Rebranded namespace / assembly / exe / solution / project; new GUIDs and strong-name key; self-update repointed to `HetCreep/PriconneReALLTL-Installer`.
- Modloader baseline pinned to ImaterialC (the canonical IL2CPP interop), independent of the selected TL source.
- All GitHub calls run off the UI thread — a slow or rate-limited response no longer freezes the window.
- Installed files keep their real source-build timestamps; folders take their newest contained file's date.
- Window fits and centers to the screen working area; "TL Source" selector made prominent and on its own line.

### Fixed
- Operations (Update / Reinstall / Uninstall / Launch) and the auto-update shortcut flow are **no longer disabled**
  when the modloader-latest check fails — that check is now a soft, non-blocking warning.
- Version checks fall back to the last cached value on a GitHub error (403/rate-limit) instead of showing "N/A".
- Versions are normalized (leading `v`/`V` stripped) before comparison and display.
- Fresh installs decide update-vs-install correctly (no more spurious 404 on a clean target).
- Archive extraction hardened with a zip-slip (path-traversal) guard; removal is path-guarded to the game folder.

### Attribution
- MIT-licensed fork of tynave/PriconneReTL-Installer; original inspiration touanu/PriconeTL_Updater. Translation patches by ImaterialC (EN) and PeterkleCG (TH).

[Unreleased]: https://github.com/HetCreep/PriconneReALLTL-Installer/compare/v3.0.0...HEAD
[3.0.0]: https://github.com/HetCreep/PriconneReALLTL-Installer/releases/tag/v3.0.0
