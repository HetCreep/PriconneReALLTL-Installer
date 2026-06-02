**PriconneReALLTL Installer v3.0.0** — the first public release of the rebranded, multi-source successor to PriconneReTL-Installer. "ALL TL" because it now installs **any** supported translation patch, not just one. This is a new product line with its own identity (name, signing key, self-update), forked with thanks from [tynave/PriconneReTL-Installer](https://github.com/tynave/PriconneReTL-Installer) under MIT.

## Highlights & Fixes

- **Multiple translation sources, user-selectable**: switch between **English** ([ImaterialC/PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL)) and **ไทย** ([PeterkleCG/PriconneTH](https://github.com/PeterkleCG/PriconneTH)) right from the main window. Installed-version detection, the modloader pin, and `AutoTranslatorConfig.ini` (language + texture list) all follow the selected source automatically.
- **Per-user installer (no admin)**: a proper Inno Setup installer alongside the portable exe. It adds a Start Menu shortcut and a clean uninstall entry that also clears the app's local cache/settings. Self-update still works because it installs under your user profile.
- **Safer installs**: the downloaded patch zip is SHA-256 verified against GitHub's published digest before anything is touched, so a corrupt or interrupted download can never half-overwrite a working install.
- **Faster repeats**: the ~330 MB patch zip is cached locally, so reinstalling or switching back to a source you already downloaded skips the re-download.
- **Ref-counted uninstall**: with both EN and TH installed, uninstalling one leaves the other (and the shared modloader) fully working; uninstalling the last one removes everything.
- **Shortcut-wrapped launching**: wrap an existing launcher/account shortcut so launching it updates the patch first, then starts your game.
- **Weekly update check**: the installer self-update check now caches its result for 7 days (it's a mature tool, not a browser). Patch/modloader checks stay frequent. A new **Check for Updates Now** menu action runs an on-demand live check any time.
- **Quieter, more responsive UI**: every GitHub call runs off the UI thread, so a slow or rate-limited response never freezes the window. Installed files keep their real source-build dates.
- **Runs on .NET Framework 4.8** (up from 4.7.2) — preinstalled on Windows 10 1903+/11, so there's nothing extra to install.
- **Own neutral branding** — the installer now ships its own language-neutral "Priconne Re:ALLTL" logo instead of borrowing one translation source's artwork, so it stays fair to every supported language.

## Security & hardening

- GitHub token byte buffers are zeroed (`Array.Clear`) right after use (DPAPI, `CurrentUser` scope); logs neutralize CR/LF/tab to prevent log forging (OWASP A09) on top of fail-closed token redaction; **Dependabot** alerts + weekly dependency / GitHub-Actions update PRs are enabled.

## Build & Distribution

- Builds run entirely in GitHub Actions on every version tag. Local builds are not distributed.
- The latest release ships the **portable exe**, the **per-user installer**, and **`SHA256SUMS.txt`**. Older releases keep only their source code — their installer/portable assets are removed automatically so nobody downloads a stale build.

## Compatibility

- Windows 10 / 11.
- Requires Princess Connect! Re:Dive installed via DMM Game Player.
- A GitHub token is optional — version checks are cached, so the app works fine unauthenticated.

## Upgrade Notes

- Coming from the old PriconneReTL-Installer? This is a separate product with its own identity, so install it fresh. Your game's translation patch is detected in place — no need to reinstall the patch itself.
