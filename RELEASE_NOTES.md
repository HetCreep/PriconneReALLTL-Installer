**PriconneReALLTL Installer v3.1.9** — security hardening from a CodeRabbit Deep Scan of the whole repository. Three findings, one root cause: the ignore-file list in a settings file you **Import** is untrusted, but its entries reached the filesystem and a regex unchecked. Low severity (it needs you to import a file someone crafted), but worth updating.

## Security

- **Imported ignore patterns are now validated.** Only relative patterns are accepted: anything rooted, a network (UNC) path, a drive or `:` (alternate data stream), or a `..` segment is refused at import (the skipped entries are logged) and again at every use, so a list saved by an older version is covered too.
  - A network pattern could make the installer probe that path while expanding wildcards, which starts a Windows network-authentication exchange with the host named in it. Nothing is contacted now.
  - A `..` entry could name one language folder while resolving to another, slipping past the "only remove the active language" guard and deleting that other language's user-edited file. The guard now runs on the resolved path.
  - A pattern with many adjacent `*` could stall an install by catastrophic regex backtracking. Adjacent `*` now count as one, and a match is bounded to 250 ms.

<!-- NOTE: do NOT add a "## Verification" section here — release.yml auto-appends one with the
     build-computed SHA-256 hashes + the Authenticode note. A manual one here duplicates it. -->

## Compatibility

- Windows 10 (64-bit) / 11. Requires Princess Connect! Re:Dive installed via DMM Game Player. Runs on **.NET Framework 4.8** (preinstalled on Windows 10 1903+ and Windows 11).
- Translation sources: English ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)), ไทย ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)), Tiếng Việt ([NTP335/PriconneRe-VN](https://github.com/NTP335/PriconneRe-VN)); modloader pinned to ImaterialC.

## Upgrade Notes

- Drop-in over v3.1.8 — settings, token, and your installed patch are unaffected. The default ignore entries (the three `_Postprocessors` / `_Preprocessors` / `_Substitutions` rule files) and any ordinary relative entry you added keep working; an entry that was absolute or contained `..` is now ignored.

## Known Issues

- A Vietnamese install shows the game's images untranslated (Japanese) — the English image pack lives under the `en` language folder and does not load under `Language=vi`. Text is fully translated; image support is a possible future enhancement.
