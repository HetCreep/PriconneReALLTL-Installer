> This file defines install / update / uninstall safety standards for PriconneReALLTL-Installer. Domain rules are authoritative; on conflict they win over AGENTS.md.

# Install / Update / Uninstall Safety

The installer's core job is to deploy, update, and remove translation-patch files in the user's game folder **without ever corrupting a working install or destroying user data**. These rules are mandatory for any change to the download / extract / remove code paths. This is the most installer-specific rule set — read it before touching `InstallerFunctions`.

## Verify before you touch

- Verify the downloaded patch zip's **SHA-256 against GitHub's published asset digest** BEFORE removing or extracting anything. Mismatch → delete the zip and abort (post-download: `downloadSuccess=false`; cached: re-download). The on-disk install must stay **untouched** on a failed verify.
- No digest available (older release) → don't hard-block, but still let `ZipFile.OpenRead` reject a structurally corrupt archive.
- Never run a remove step before a **verified** download is in hand — this closes the "remove succeeded, then extract failed" window that would leave the user with nothing.

## Confine every write and delete to the game folder

- Resolve every target to a full path and confirm it stays **inside the game directory** before writing or deleting; reject `..\`, absolute, or escaping entries.
  - **Extraction:** the zip-slip guard skips any entry whose resolved path escapes the game folder.
  - **Removal:** the same guard means a tampered or malformed manifest can never delete outside the game folder.

## Never destroy user data

- User-edited config (e.g. `AutoTranslatorConfig.ini`) and ignored paths are protected via the ignore set; they are not overwritten or removed unless the user explicitly opts in (Remove Config / Remove Ignored).
- Updating one source must **not** delete another installed source's files (see the ref-counted manifest).
- Update / reinstall re-extracts **published patch files only** — it never touches the game's own binaries.

## Ref-counted manifest (per-source uninstall)

- Track installed files in `BepInEx\.priconnerealltl-manifest.json` = `{files:{relpath:[ownerShortNames]}}`; write/merge it after every successful extract.
- Uninstall removes the **selected** source from each owned file's owner-list and deletes only files whose owner-list **empties**; shared engine files stay while another source still needs them. The manifest is deleted when the last source goes.
- The manifest is **advisory**: set `Hidden`; on corrupt/missing → fall back to the ProcessTree removal, then rewrite a fresh manifest (self-heal). Its loss is never fatal and never causes data loss.

## A partial failure is a failure

- A per-file extract failure sets `extractSuccess=false`, excludes that file from the manifest, and surfaces a "run Reinstall" message — **never report success on a partial extract.**
- **Reinstall is the repair path:** it re-downloads (or reuses the verified cache) and re-extracts everything (full clean), so a broken/partial install self-heals — no separate Repair button needed.

## Idempotent + cache-safe

- Reinstalling the same version is idempotent (identical end state).
- The patch zip is cached in `%LOCALAPPDATA%` by source+version; a cached zip is **SHA-256-verified before reuse** (mismatch → purge + re-download). Never reuse an unverified cached archive.

## Timestamps (cosmetic, best-effort)

- Stamp the cached zip with its source date (release `published_at`, else the zip's newest entry) at download; stamp installed game folders with their newest contained file's date after extract. Never block an install on a stamp failure.

## Anti-patterns

| Pattern | Reason |
|---|---|
| Remove/extract without the game-folder path guard | out-of-folder deletion / zip-slip |
| Reusing a cached zip without re-verifying its digest | tampered / truncated archive |
| Reporting success after a per-file extract error | leaves a broken install looking healthy |
| Deleting shared engine files while another source still needs them | breaks the other language |
| Overwriting user config / ignored edits without opt-in | data loss |
| A remove step before the download is verified | can leave the user with no working install |

## Reference

- [release-verification.md](release-verification.md) — the user-facing verification side (SHA256SUMS)
- [native-windows-api.md](native-windows-api.md) — registry / shortcut / process specifics
- [legal-boundary.md](legal-boundary.md) — files-only; never the game's own binary
