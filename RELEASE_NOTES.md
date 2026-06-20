**PriconneReALLTL Installer v3.1.0** — Tiếng Việt (Vietnamese) joins English and ไทย as a third translation source, with the install machinery extended for text-only patches. Xin chào các bạn người Việt! 🇻🇳

## Added

- **Vietnamese translation source** — [NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN) is now selectable from the main screen alongside English and ไทย. Per its author's request, a quality notice is shown when you select it (*"Đây là bản dịch bằng Gemini AI — sẽ có vài lỗi nhỏ và cách xưng hô chưa đúng. Cân nhắc trước khi tải."*); declining keeps your previous source. Coordinated with the author in [NTP335/PriconneRe-VN#1](https://github.com/NTP335/PriconneRe-VN/issues/1).
- **Engine-base chaining for text-only sources** — the VN patch ships only its translation text (~15 MB), so installing it automatically fetches the ImaterialC modloader engine (~330 MB) first as the base layer, then applies the Vietnamese text on top. Both downloads are SHA-256-verified **before** anything touches your install; the engine zip is cached and shared with a plain English install; uninstalling Vietnamese removes the base too unless English still owns it (ref-counted, as always).
- **Per-source notice dialog** — translation sources can declare a one-line disclaimer that appears before your selection persists.
- **README in Vietnamese** — [README.vi.md](https://github.com/HetCreep/PriconneReALLTL-Installer/blob/master/README.vi.md).

## Fixed

- **Text-only sources now get the right `Language=`** — a source whose zip ships no `AutoTranslatorConfig.ini` (VN) falls back to its own language code; previously the setting would have stayed on the engine base's `en` and the translation would never load.
- **A failed engine-base extract can no longer report success** — the extract-success flag is sticky across the chained base + text extractions, and the text layer is skipped when the base failed (the operation reports as failed; Reinstall repairs it).
- **The translation-source menu no longer leaks** — the transient context menu is disposed after it closes.
- **VN's fixup plugins** — per the author's own test, the Vietnamese text needs the English fixup DLLs (`PriconneTLFixup` / `PriconneSkillTLFixup`) for correct font sizing. VN installs on the ImaterialC engine base, which ships those DLLs active, so they load for VN as-is.

## Changed

- **Ignore-list defaults are now language-agnostic** — the default protected rule files (`_Substitutions` / `_Preprocessors` / `_Postprocessors`) use a `*` language glob out of the box, matching what the startup migration already produced for existing installs; a fresh install is protected under `vi`/`th` from the first run, and "reset to defaults" no longer briefly reverts to the `en`-only list.
- Source files with non-ASCII literals carry an explicit UTF-8 BOM instead of relying on compiler encoding detection.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.0.x — settings, token, and your installed patch are unaffected.
- The Vietnamese source is new in this release — switching to it walks you through the author's quality notice, then installs the engine base + the VN text in one operation.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
