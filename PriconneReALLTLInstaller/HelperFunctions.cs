using InstallerFunctions;
using LoggerFunctions;
using Microsoft.Win32;
using Newtonsoft.Json;
using PriconneReALLTLInstaller;
using PriconneReALLTLInstaller.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.Management;

namespace HelperFunctions
{   
    public class Helper
    {
        public event Action<string, string, bool> Log;
        public event Action<string> ErrorLog;

        [DllImport("gdi32.dll")]
        public static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
        public void PriconneFont(PrivateFontCollection priconnefont)
        {
            //Select  font from the resources.
            int fontLength = Resources.NunitoBold.Length;

            // create a buffer to read in to
            byte[] fontdata = Resources.NunitoBold;

            // create an unsafe memory block for the font data
            System.IntPtr data = Marshal.AllocCoTaskMem(fontLength);

            // copy the bytes to the unsafe memory block
            Marshal.Copy(fontdata, 0, data, fontLength);

            uint cFonts = 0;
            AddFontMemResourceEx(data, (uint)fontdata.Length, IntPtr.Zero, ref cFonts);

            // pass the font to the font collection
            priconnefont.AddMemoryFont(data, fontLength);

            Marshal.FreeCoTaskMem(data);

        }
        public void SetFontForAllControls(PrivateFontCollection priconnefont, Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is ToolStrip toolStrip)
                {
                    SetFontForToolStripItems(priconnefont, toolStrip.Items);
                }
                else if (control is RichTextBox richTextBox)
                {
                    richTextBox.Font = new Font(priconnefont.Families[0], richTextBox.Font.Size, richTextBox.Font.Style);
                }
                else if (control.ContextMenuStrip != null)
                {
                    SetFontForContextMenuItems(priconnefont, control.ContextMenuStrip.Items);
                }
                else
                {
                    control.Font = new Font(priconnefont.Families[0], control.Font.Size, control.Font.Style, GraphicsUnit.Point, 1);
                }

                // Check if the control has child controls
                if (control.Controls.Count > 0)
                {
                    // If it has child controls, call SetFontForAllControls recursively
                    SetFontForAllControls(priconnefont, control.Controls);
                }
            }
        }
        public void SetFontForToolStripItems(PrivateFontCollection priconnefont, ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                // Check if the item is a ToolStripLabel
                if (item is ToolStripLabel toolStripLabel)
                {
                    toolStripLabel.Font = new Font(priconnefont.Families[0], toolStripLabel.Font.Size, FontStyle.Bold, GraphicsUnit.Point, 1);
                }
                else if (item is ToolStripDropDownItem dropDownItem)
                {
                    // If the item is a drop-down item (e.g., ToolStripDropDownButton, ToolStripMenuItem), handle its subitems recursively
                    SetFontForToolStripItems(priconnefont, dropDownItem.DropDownItems);
                }
            }
        }
        public void SetFontForContextMenuItems(PrivateFontCollection priconnefont, ToolStripItemCollection contextMenuItems)
        {
            foreach (ToolStripItem item in contextMenuItems)
            {
                // Set font for each ToolStripItem in the context menu
                item.Font = new Font(priconnefont.Families[0], item.Font.Size, item.Font.Style, GraphicsUnit.Point, 1);

                // If the item is a drop-down item (like a sub-menu), handle its items recursively
                if (item is ToolStripDropDownItem dropDownItem)
                {
                    SetFontForContextMenuItems(priconnefont, dropDownItem.DropDownItems);
                }
            }
        }
        public bool isAnyChecked(CheckBox[] checkboxes)
        {
            if (checkboxes != null)
            {
                foreach (CheckBox checkbox in checkboxes) if (checkbox.Checked) return true;
            }
            return false;
        }
        public string[] SetIgnoreFiles(string priconnePath, bool addconfig)
        {
            string[] ignoreFiles = CurrentSourceIgnoreFiles().ToArray();   // resolved to the current source's lang (en/th)

            if (addconfig)
            {
                List<string> ignoreFilesList = new List<string>(ignoreFiles);

                foreach (var configFile in Settings.Default.configFiles)
                {
                    if (File.Exists(Path.Combine(priconnePath, configFile)))
                    {
                        ignoreFilesList.Add(configFile);
                    }
                }

                ignoreFiles = ignoreFilesList.ToArray();
            }

            return ignoreFiles;
        }
        public bool IsConfigPresent(string priconnePath)
        {
            bool isConfigPresent = false;
            foreach (var configFile in Settings.Default.configFiles)
            {
                if (File.Exists(Path.Combine(priconnePath, configFile)))
                {
                    isConfigPresent = true;
                }
            }

            if (isConfigPresent) Log?.Invoke("Found config file(s). Adding them to the list of ignored/excluded files.", "error", false);
            return isConfigPresent;
        }
        public void CannotExitNotification(FormClosingEventArgs e, string type)
        {
            MessageBox.Show($"There is currently a {type} process in progress.\nPlease wait for the operation to complete.", "Cannot Exit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
        public DialogResult UninstallReinstallNotification(bool uninstall, bool reinstall, bool removeConfig, bool removeIgnored, StringCollection configFilesSelected, StringCollection configFilesUnselected)
        {
            StringBuilder configFilesSelectedBuilder = new StringBuilder();
            StringBuilder configFilesUnselectedBuilder = new StringBuilder();

            foreach (string item in configFilesSelected) configFilesSelectedBuilder.AppendLine(item);
            foreach (string item in configFilesUnselected) configFilesUnselectedBuilder.AppendLine(item);

            string configFilesSelectedString = configFilesSelectedBuilder.ToString();
            string configFilesUnselectedString = configFilesUnselectedBuilder.ToString();

            string operationType = uninstall ? "uninstall" : "reinstall";
            string ignoreNofitication = $"The files set in the ignore list WILL{(removeIgnored ? "" : " NOT")} BE removed.";
            string noConfigRemove = "The config files WILL NOT BE removed.";
            string configSelectedNotification = $"The following config file(s) WILL BE deleted: {configFilesSelectedString}";
            string configUnselectedNotification = $"The following config file(s) WILL NOT BE deleted: {configFilesUnselectedString}";

            string notificationText = $"Are you sure you want to {operationType} the translation patch?\n\n{ignoreNofitication}";

            if (!removeConfig || configFilesSelectedString.Length == 0) notificationText += $"\n\n{noConfigRemove}";
            else
            {
                if (removeConfig && configFilesSelectedString.Length != 0) notificationText += $"\n\n{configSelectedNotification}";
                if (removeConfig && configFilesUnselectedString.Length != 0) notificationText += $"\n\n{configUnselectedNotification}";
            }

            DialogResult result = MessageBox.Show(notificationText, "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            return result;
        }

        public static bool IsGameRunning(string priconnePath)
        {
            string processName = "PrincessConnectReDive";
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (var process in processes)
            {
                try
                {
                    string exePath = GetExecutablePath(process);
                    string folder = Path.GetDirectoryName(exePath);

                    System.Diagnostics.Debug.WriteLine($"Detected process path: {exePath}");

                    if (string.Equals(
                            Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar),
                            Path.GetFullPath(priconnePath).TrimEnd(Path.DirectorySeparatorChar),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show($"The game is currently running.\nPlease exit the game before performing any operations.", "Cannot Start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Log if a process cannot be accessed
                    System.Diagnostics.Debug.WriteLine($"Cannot access process {process.ProcessName}: {ex.Message}");
                    MessageBox.Show($"Cannot access game process to check if it's running.\nTry running the installer in admin mode.", "Cannot Start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return true;
                }
            }

            return false;
        }

        private static string GetExecutablePath(Process process)
        {
            if (Environment.OSVersion.Version.Major >= 6) // Vista+
            {
                return GetExecutablePathAboveVista(process.Id);
            }

            return process.MainModule.FileName;
        }

        private static string GetExecutablePathAboveVista(int processId)
        {
            var buffer = new StringBuilder(1024);
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, processId);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    int size = buffer.Capacity;
                    if (QueryFullProcessImageName(hProcess, 0, buffer, out size))
                    {
                        return buffer.ToString();
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, out int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            QueryLimitedInformation = 0x1000
        }
        // ─── PriconneMultiAccountLauncher integration ─────────────────────────────
        // Product name + exe of the launcher this installer integrates with.
        // Source of truth: HetCreep/PriconneMultiAccountLauncher setup.iss.
        private const string PmalFolderName = "PriconneMultiAccountLauncher";
        private const string PmalExeName = "PriconneMultiAccountLauncher.exe";
        // Inno Setup uninstall registry subkey: AppId + "_is1". AppId is fixed in
        // the launcher's setup.iss ({ECD76E8C-1446-453B-BCB6-80C4CCD5FE53}).
        private const string PmalUninstallSubKey =
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{ECD76E8C-1446-453B-BCB6-80C4CCD5FE53}_is1";

        /// <summary>
        /// Resolves the full path to PriconneMultiAccountLauncher.exe.
        /// Detection order: the launcher's own Inno Setup uninstall key
        /// (InstallLocation) under HKCU, then HKLM (32- and 64-bit views, because
        /// the Inno installer runs 32-bit and writes to WOW6432Node on x64),
        /// then a fixed fallback of %APPDATA%\PriconneMultiAccountLauncher.
        /// The returned path is a best candidate; callers must File.Exists-check it.
        /// </summary>
        public string GetPriconneMultiLauncherExePath()
        {
            string installDir = ReadPmalInstallLocationFromRegistry();

            if (string.IsNullOrEmpty(installDir))
            {
                installDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    PmalFolderName);
            }

            return Path.Combine(installDir, PmalExeName);
        }

        private string ReadPmalInstallLocationFromRegistry()
        {
            // Default install is per-user (setup.iss PrivilegesRequired=lowest) -> HKCU.
            // An elevated install lands in HKLM; the 32-bit Inno installer writes the
            // key under WOW6432Node on 64-bit Windows, so probe both HKLM views.
            var probes = new (RegistryHive hive, RegistryView view)[]
            {
                (RegistryHive.CurrentUser, RegistryView.Registry64),
                (RegistryHive.LocalMachine, RegistryView.Registry32),
                (RegistryHive.LocalMachine, RegistryView.Registry64),
            };

            foreach (var probe in probes)
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(probe.hive, probe.view))
                    using (var key = baseKey.OpenSubKey(PmalUninstallSubKey))
                    {
                        string location = key?.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(location) &&
                            File.Exists(Path.Combine(location, PmalExeName)))
                        {
                            return location;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Registry read is best-effort; fall through to the next probe / fallback.
                    System.Diagnostics.Debug.WriteLine($"PMAL registry probe failed ({probe.hive}/{probe.view}): {ex.Message}");
                }
            }

            return null;
        }

        public void LogFastLauncherShortcut()
        {
            var links = GetFastLauncherLinks();
            if (links.Count == 0) Log?.Invoke("No launch shortcut set yet — wrap a launcher shortcut to enable one-click update + play.", "info", false);
            else Log?.Invoke("Launch shortcuts (update + play): " + string.Join(", ", links), "info", false);
        }
        /// <summary>Returns the full list of configured shortcut paths (merging legacy single-link if needed).</summary>
        public System.Collections.Generic.List<string> GetFastLauncherLinks()
        {
            var col = Settings.Default.fastLauncherLinks;
            var list = col == null
                ? new System.Collections.Generic.List<string>()
                : col.Cast<string>().ToList();
            // Include legacy single link if not yet migrated
            string legacy = Settings.Default.fastLauncherLink;
            if (!string.IsNullOrEmpty(legacy) && !list.Contains(legacy))
                list.Add(legacy);
            return list;
        }
        // ─── Translation patch source selection (EN/TH) ──────────────────────────
        // The installer can target multiple translation repositories. The chosen
        // index is persisted in Settings.selectedPatchSource (user-scoped). Every
        // patch URL (release API, modloader ref/raw, releases page) derives from
        // the selected source — never hardcode a repo elsewhere.
        public sealed class PatchSource
        {
            public string DisplayName { get; }
            public string ShortName { get; }
            public string Owner { get; }
            public string Repo { get; }
            // Installed-version detection per source: where the version file lives (relative
            // to the game folder) + a regex to extract the version from it. EN ships an
            // 8-digit date in en/Text/Version.txt; TH a semver in th/Text/Version.txt.
            public string VersionFileRelPath { get; }
            public string VersionRegex { get; }
            // Target language code (en/th) derived from the version-file path's Translation\<lang>\
            // segment — used to keep AutoTranslatorConfig.ini's Language= in sync with the source.
            public string Lang
            {
                get
                {
                    string[] parts = (VersionFileRelPath ?? "").Replace('/', '\\').Split('\\');
                    int i = System.Array.FindIndex(parts, p => string.Equals(p, "Translation", System.StringComparison.OrdinalIgnoreCase));
                    return (i >= 0 && i + 1 < parts.Length) ? parts[i + 1] : "en";
                }
            }
            // Per-source plugin profile, applied in BepInEx/plugins via a ".bak" toggle:
            // EnablePlugins are restored (.dll.bak -> .dll), DisablePlugins are shelved
            // (.dll -> .dll.bak). Lets EN-only fixup DLLs (PriconneSkillTLFixup/PriconneTLFixup)
            // and a TH-only fixup DLL (PriconneALLTLFixup) coexist on disk while only the active
            // source's set loads. The DLLs themselves still ship via the patch/modloader.
            public System.Collections.Generic.IReadOnlyList<string> EnablePlugins { get; }
            public System.Collections.Generic.IReadOnlyList<string> DisablePlugins { get; }
            // External plugin DLLs this source pulls from their OWN repos (not bundled in the
            // patch zip). e.g. TH pulls PriconneALLTLFixup.dll from HetCreep/PriconneALLTLFixup.
            // EN declares none — its fixups (PriconneSkillTLFixup/PriconneTLFixup) ship inside the
            // ImaterialC patch. A repo with no release yet is skipped softly (wired ahead of release).
            public System.Collections.Generic.IReadOnlyList<PluginDownload> PluginDownloads { get; }
            public PatchSource(string displayName, string shortName, string owner, string repo, string versionFileRelPath, string versionRegex,
                string[] enablePlugins = null, string[] disablePlugins = null, PluginDownload[] pluginDownloads = null)
            {
                DisplayName = displayName;
                ShortName = shortName;
                Owner = owner;
                Repo = repo;
                VersionFileRelPath = versionFileRelPath;
                VersionRegex = versionRegex;
                EnablePlugins = enablePlugins ?? new string[0];
                DisablePlugins = disablePlugins ?? new string[0];
                PluginDownloads = pluginDownloads ?? new PluginDownload[0];
            }
            public string ApiBase => $"https://api.github.com/repos/{Owner}/{Repo}";
            public string RawBase => $"https://raw.githubusercontent.com/{Owner}/{Repo}";
            public string ReleasesPage => $"https://github.com/{Owner}/{Repo}/releases/latest";
        }

        /// <summary>An external plugin DLL a source fetches from its own GitHub release (separate
        /// from the patch zip). The latest release's matching ".dll" asset is downloaded into
        /// BepInEx/plugins. Used for plugins maintained in a standalone repo.</summary>
        public sealed class PluginDownload
        {
            public string Owner { get; }
            public string Repo { get; }
            public string DllName { get; }
            public PluginDownload(string owner, string repo, string dllName)
            {
                Owner = owner;
                Repo = repo;
                DllName = dllName;
            }
            public string ApiBase => $"https://api.github.com/repos/{Owner}/{Repo}";
            public string ReleasesPage => $"https://github.com/{Owner}/{Repo}/releases/latest";
        }

        public static readonly System.Collections.Generic.IReadOnlyList<PatchSource> PatchSources =
            new System.Collections.Generic.List<PatchSource>
            {
                new PatchSource("English  (ImaterialC / PriconneRe-TL)", "English", "ImaterialC", "PriconneRe-TL",
                    @"BepInEx\Translation\en\Text\Version.txt", @"\d{8}[a-z]?",
                    enablePlugins: new[] { "PriconneSkillTLFixup.dll", "PriconneTLFixup.dll" },
                    disablePlugins: new[] { "PriconneALLTLFixup.dll" }),
                new PatchSource("Thai  (PeterkleCG / PriconneTH)", "Thai", "PeterkleCG", "PriconneTH",
                    @"BepInEx\Translation\th\Text\Version.txt", @"v?\d+\.\d+(?:\.\d+)?",
                    enablePlugins: new[] { "PriconneALLTLFixup.dll" },
                    disablePlugins: new[] { "PriconneSkillTLFixup.dll", "PriconneTLFixup.dll" },
                    pluginDownloads: new[] { new PluginDownload("HetCreep", "PriconneALLTLFixup", "PriconneALLTLFixup.dll") }),
            };

        /// <summary>Currently selected translation patch source (falls back to index 0 / English).</summary>
        public static PatchSource GetCurrentPatchSource()
        {
            int idx = Settings.Default.selectedPatchSource;
            if (idx < 0 || idx >= PatchSources.Count) idx = 0;
            return PatchSources[idx];
        }

        // The ignore patterns (lang-agnostic). Stored as globs like BepInEx/Translation/*/Text/
        // _Postprocessors.txt so the rule files are protected under ANY language folder (en/th/…).
        public static System.Collections.Generic.List<string> CurrentSourceIgnoreFiles()
        {
            var result = new System.Collections.Generic.List<string>();
            if (Settings.Default.ignoreFiles != null) foreach (string p in Settings.Default.ignoreFiles) result.Add(p);
            return result;
        }

        // True if relPath matches any ignore pattern. A pattern may use '*' = exactly one path segment
        // (BepInEx/Translation/*/Text/_Postprocessors.txt → matches en, th, …). Patterns without '*'
        // match exactly (e.g. the config files). Case-insensitive; '/'-normalised.
        public static bool IgnoreMatches(string relPath, System.Collections.Generic.IEnumerable<string> patterns)
        {
            if (string.IsNullOrEmpty(relPath) || patterns == null) return false;
            string p = relPath.Replace('\\', '/');
            foreach (string pat in patterns)
            {
                if (string.IsNullOrEmpty(pat)) continue;
                string g = pat.Replace('\\', '/');
                if (g.IndexOf('*') < 0)
                {
                    if (string.Equals(g, p, System.StringComparison.OrdinalIgnoreCase)) return true;
                }
                else
                {
                    string rx = "^" + System.Text.RegularExpressions.Regex.Escape(g).Replace("\\*", "[^/]+") + "$";
                    if (System.Text.RegularExpressions.Regex.IsMatch(p, rx, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return true;
                }
            }
            return false;
        }

        // One-time migration: the ignore defaults used to hardcode en/. Rewrite the three bundled
        // rule-file entries to the lang-agnostic */ glob so they protect any language. Leaves custom
        // entries (and already-migrated * entries) untouched.
        public static void MigrateIgnoreDefaults()
        {
            var ig = Settings.Default.ignoreFiles;
            if (ig == null) return;
            string[] names = { "_Postprocessors.txt", "_Preprocessors.txt", "_Substitutions.txt" };
            bool changed = false;
            for (int i = 0; i < ig.Count; i++)
            {
                string v = (ig[i] ?? "").Replace('\\', '/');
                foreach (string n in names)
                    if (v.Equals("BepInEx/Translation/en/Text/" + n, System.StringComparison.OrdinalIgnoreCase))
                    { ig[i] = "BepInEx/Translation/*/Text/" + n; changed = true; }
            }
            if (changed) Settings.Default.Save();
        }

        // Game UI textures (event logos, story thumbnails, buttons, …) the TL replaces that appear more
        // than once, so XUnity AutoTranslator must disambiguate them. Same for every source (these are
        // the game's textures), so applied universally; Language= is the only per-source key.
        private const string DuplicateTextureNamesValue = "event_logo;event_logo_00000;Btn_SubContents;Btn_EvQuest;Btn_EvGacha;Btn_EvStory;obj_texture;quest_boss_01;quest_boss_02;Rvl_EvQuest_BOSS;event_icon_storynumber_01;event_icon_storynumber_02;event_icon_storynumber_03;event_icon_storynumber_04;event_icon_storynumber_05;event_icon_storynumber_ed;event_icon_storynumber_ep;event_icon_storynumber_op;abyss_logo;clanbattle_logo;Invasion_logo_1001;Invasion_logo_1002;story_thumb_000;story_thumb_001;story_thumb_002;story_thumb_003;story_thumb_004;story_thumb_005;story_thumb_006;story_thumb_007;story_thumb_008;story_thumb_009;story_thumb_010;story_thumb_011;story_thumb_012;story_thumb_013;story_thumb_014;story_thumb_015;Btn_Mission;Btn_SubContents_lock;Btn_SubContents_lockBack;step_image_1;step_image_2;step_image_3;step_image_4";

        // Keys synced into the local AutoTranslatorConfig.ini from the SOURCE's own shipped config.
        private static readonly string[] SyncedConfigKeys = { "Language", "DuplicateTextureNames" };

        // Keeps AutoTranslatorConfig.ini's source-specific keys in sync after install/update. The config
        // is shared + kept, so a source switch would otherwise leave the previous source's values. Pulls
        // the synced keys' VALUES from the SOURCE's own shipped config (sourceConfigText, read from the
        // patch zip) — so e.g. EN editing DuplicateTextureNames is followed automatically, no hardcoding;
        // falls back to the bundled list only if the source config omits it. Replaces ONLY those keys'
        // values in the local file, preserving every other line incl. the user's per-machine settings.
        public void ApplyConfigOverrides(string priconnePath, string sourceConfigText)
        {
            try
            {
                if (string.IsNullOrEmpty(priconnePath)) return;
                string cfg = Path.Combine(priconnePath, "BepInEx", "config", "AutoTranslatorConfig.ini");
                if (!File.Exists(cfg)) return;

                var want = new System.Collections.Generic.HashSet<string>(SyncedConfigKeys, StringComparer.OrdinalIgnoreCase);
                var values = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(sourceConfigText))
                {
                    foreach (string raw in sourceConfigText.Replace("\r\n", "\n").Split('\n'))
                    {
                        int e = raw.IndexOf('=');
                        if (e <= 0) continue;
                        string k = raw.Substring(0, e).Trim();
                        if (want.Contains(k) && !values.ContainsKey(k)) values[k] = raw.Substring(e + 1).TrimEnd('\r');
                    }
                }
                // Fall back to the bundled texture list only if the source's config didn't carry it.
                if (!values.ContainsKey("DuplicateTextureNames")) values["DuplicateTextureNames"] = DuplicateTextureNamesValue;
                if (values.Count == 0) return;

                string[] lines = File.ReadAllLines(cfg);
                bool changed = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    int eq = lines[i].IndexOf('=');
                    if (eq <= 0) continue;
                    string key = lines[i].Substring(0, eq).Trim();
                    if (values.TryGetValue(key, out string val))
                    {
                        string newLine = lines[i].Substring(0, eq + 1) + val;   // keep the key text, replace the value
                        if (lines[i] != newLine) { lines[i] = newLine; changed = true; }
                    }
                }
                if (changed)
                {
                    File.WriteAllLines(cfg, lines);
                    Log?.Invoke($"Synced AutoTranslatorConfig.ini to {GetCurrentPatchSource().ShortName} (from the source's shipped config).", "info", false);
                }
            }
            catch (Exception ex) { Log?.Invoke("Could not update AutoTranslatorConfig.ini: " + ex.Message, "error", false); }
        }

        // Master switch for the per-source plugin profile. OFF for now — the feature isn't ready for
        // real use until the TH PriconneALLTLFixup plugin ships. When false, ApplyPluginProfile and
        // DownloadSourcePlugins are no-ops (and any plugin a prior run shelved is restored, so nothing
        // is left disabled). Flip to true to turn the feature back on — all the code stays in place.
        public static readonly bool PluginProfileEnabled = false;

        /// <summary>
        /// Applies the currently selected source's plugin profile in BepInEx/plugins by toggling
        /// the ".bak" suffix: DisablePlugins are shelved (.dll -> .dll.bak), EnablePlugins are
        /// restored (.dll.bak -> .dll). Idempotent and safe to call on launch, after install, and
        /// on source switch. Plugins not present on disk are simply skipped.
        /// </summary>
        public void ApplyPluginProfile(string priconnePath)
        {
            try
            {
                if (string.IsNullOrEmpty(priconnePath)) return;
                string pluginsDir = Path.Combine(priconnePath, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsDir)) return;

                PatchSource src = GetCurrentPatchSource();
                if (!PluginProfileEnabled)
                {
                    // Feature off: don't shelve anything; SILENTLY restore any plugin a prior run shelved
                    // (.dll.bak -> .dll) so none is left disabled. The patch's plugins load as shipped.
                    // Silent because, with the profile off, this is a one-time cleanup — not the profile
                    // doing per-source work (a logged "Enabled plugin for this TL source" would mislead).
                    foreach (string dll in src.EnablePlugins) RestorePluginDll(pluginsDir, dll, silent: true);
                    foreach (string dll in src.DisablePlugins) RestorePluginDll(pluginsDir, dll, silent: true);
                    return;
                }
                foreach (string dll in src.DisablePlugins) ShelvePluginDll(pluginsDir, dll);
                foreach (string dll in src.EnablePlugins) RestorePluginDll(pluginsDir, dll);
            }
            catch (Exception ex)
            {
                Log?.Invoke("Could not apply plugin profile: " + ex.Message, "error", false);
            }
        }

        // Disable a plugin: rename "<dll>" -> "<dll>.bak" so BepInEx no longer loads it.
        private void ShelvePluginDll(string pluginsDir, string dll)
        {
            string dllPath = Path.Combine(pluginsDir, dll);
            if (!File.Exists(dllPath)) return;                 // already disabled / not installed
            string bakPath = dllPath + ".bak";
            if (File.Exists(bakPath)) File.Delete(bakPath);    // drop a stale backup so Move succeeds
            File.Move(dllPath, bakPath);
            Log?.Invoke($"Disabled plugin (not used by this TL source): {dll}", "info", false);
        }

        // Enable a plugin: restore "<dll>.bak" -> "<dll>" so BepInEx loads it again. silent=true skips
        // the log (used by the feature-off cleanup, which shouldn't look like the profile is active).
        private void RestorePluginDll(string pluginsDir, string dll, bool silent = false)
        {
            string dllPath = Path.Combine(pluginsDir, dll);
            if (File.Exists(dllPath)) return;                  // already enabled
            string bakPath = dllPath + ".bak";
            if (!File.Exists(bakPath)) return;                 // nothing to restore
            File.Move(bakPath, dllPath);
            if (!silent) Log?.Invoke($"Enabled plugin for this TL source: {dll}", "info", false);
        }

        /// <summary>Canonicalizes a version/tag for comparison: trims and strips a leading
        /// "v"/"V" so "V2.1.3", "v2.1.3" and "2.1.3" all compare equal. EN date strings
        /// (e.g. "20260531") are unaffected. For comparison/display-matching ONLY — the raw
        /// tag is still needed for GitHub API calls (git/ref/tags/{tag}).</summary>
        public static string NormalizeVersion(string v) =>
            string.IsNullOrEmpty(v) ? "" : v.Trim().TrimStart('v', 'V');

        /// <summary>Compares two version/tag strings by NUMERIC precedence, not lexicographically (#17).
        /// Semver ("2.1.10" &gt; "2.1.9", "3.0.10" &gt; "3.0.9") parses via <see cref="Version"/>; EN
        /// 8-digit date tags ("20260531", optional trailing letter) compare by their numeric run.
        /// Returns &lt;0 / 0 / &gt;0 like CompareTo. Ordinal string compare is the last resort only.</summary>
        public static int CompareVersions(string a, string b)
        {
            string na = NormalizeVersion(a), nb = NormalizeVersion(b);
            if (na == nb) return 0;
            if (Version.TryParse(na, out var va) && Version.TryParse(nb, out var vb))
                return va.CompareTo(vb);
            if (TrySplitNumericTag(na, out long da, out string sa) && TrySplitNumericTag(nb, out long db, out string sb))
            {
                int c = da.CompareTo(db);
                return c != 0 ? c : string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
            }
            return string.Compare(na, nb, StringComparison.OrdinalIgnoreCase);
        }

        // Splits a leading digit run (EN date "20260531") from an optional trailing suffix
        // ("20260531a") so the numeric part dominates and the letter only breaks ties.
        private static bool TrySplitNumericTag(string s, out long num, out string suffix)
        {
            num = 0; suffix = "";
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            if (i == 0) return false;
            suffix = s.Substring(i);
            return long.TryParse(s.Substring(0, i), out num);
        }

        /// <summary>Full path under the app's local data dir (%LOCALAPPDATA%\PriconneReALLTLInstaller)
        /// for a log/data file — created on demand. Logs go HERE, not the install dir, so the Inno
        /// uninstaller's [UninstallDelete] of this dir removes them on uninstall (no leftover log in
        /// the program folder — #6). Same root as the version cache + zip cache.</summary>
        public static string LogPath(string fileName)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PriconneReALLTLInstaller");
            try { Directory.CreateDirectory(dir); } catch { }
            return Path.Combine(dir, fileName);
        }

        /// <summary>The authoritative modloader source — ALWAYS ImaterialC (the main, widely-used
        /// patch), regardless of the selected TL source. Its bundled BepInEx interop is the baseline
        /// (a TL source like PeterkleCG may ship its own copy, but ImaterialC's is treated as canonical).</summary>
        public static PatchSource ModloaderSource =>
            PatchSources.FirstOrDefault(s => s.Owner == "ImaterialC") ?? PatchSources[0];

        // ─── Version-check cache (rate-limit friendly) ────────────────────────────
        // Persisted in Settings.versionCacheJson. Each GetLatest* result is cached by key
        // for VersionCacheTtlHours so repeated app launches don't re-hit the GitHub API.
        // Switching TL source / manual refresh sets BypassVersionCache to force a fresh fetch.
        private const double VersionCacheTtlHours = 6.0;
        // The installer self-update check is far less time-sensitive than the patch/modloader checks:
        // the app is mature, so releases are rare bug-fixes, not a steady stream. Cache its result for
        // a week → a launch hits GitHub for it at most ~once/7 days instead of every 6h. The manual
        // "Check for Updates Now" still bypasses this for a live read. Tune here if needed.
        public const double InstallerCheckTtlHours = 24.0 * 7;
        // #23: AsyncLocal so each async UI flow's bypass intent is isolated — overlapping flows
        // (a source-switch fetch + "Check for Updates Now") no longer race on a shared process-wide
        // flag. The value set before an `await Task.Run(...)` flows INTO that Task.Run via the captured
        // ExecutionContext, but stays invisible to a concurrent flow's context. Callers are unchanged.
        private static readonly System.Threading.AsyncLocal<bool> _bypassVersionCache = new System.Threading.AsyncLocal<bool>();
        public static bool BypassVersionCache { get => _bypassVersionCache.Value; set => _bypassVersionCache.Value = value; }

        private sealed class CacheEntry { public string Val { get; set; } public DateTime Ts { get; set; } }

        /// <summary>Cached JSON value for key if fresh (&lt; TTL) and not bypassed; else null. ttlHours overrides the default TTL for that key.</summary>
        public static string GetCachedVersion(string key, bool allowStale = false, double? ttlHours = null)
        {
            // allowStale: ignore the TTL AND the bypass flag — used as a fallback when a live fetch
            // fails (e.g. HTTP 403 rate-limit) so the app keeps showing the last known value tokenless
            // instead of "N/A", even during a source-switch (which sets BypassVersionCache).
            if (BypassVersionCache && !allowStale) return null;
            double ttl = ttlHours ?? VersionCacheTtlHours;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(Settings.Default.versionCacheJson ?? "");
                if (dict != null && dict.TryGetValue(key, out var e) && e != null)
                {
                    // Clock-skew guard (#15): a system clock set BACKWARD after a cache write makes
                    // (UtcNow - Ts) negative, which the old `< ttl` read as "fresh" → the cache froze
                    // on a stale value and never re-fetched (the user couldn't get a newer patch until
                    // they corrected the clock). Require age in [0, ttl): a negative age (clock moved
                    // back) or a huge age (clock far ahead) now falls through to a live re-fetch.
                    // allowStale still wins (offline / rate-limited fallback keeps the last value).
                    double ageHours = (DateTime.UtcNow - e.Ts).TotalHours;
                    if (allowStale || (ageHours >= 0 && ageHours < ttl))
                        return e.Val;
                }
            }
            catch { }
            return null;
        }

        public static void SetCachedVersion(string key, string value)
        {
            try
            {
                Dictionary<string, CacheEntry> dict = null;
                try { dict = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(Settings.Default.versionCacheJson ?? ""); }
                catch { }
                if (dict == null) dict = new Dictionary<string, CacheEntry>();
                dict[key] = new CacheEntry { Val = value, Ts = DateTime.UtcNow };
                Settings.Default.versionCacheJson = JsonConvert.SerializeObject(dict);
                Settings.Default.Save();
            }
            catch { }
        }

        // Smart-uninstall foundation: record which source(s) own each installed patch file
        // (path -> owner list) in BepInEx\.priconnerealltl-manifest.json. Written after each successful
        // extract; merged across sources so a shared file (e.g. the modloader engine) ends up owned by
        // EVERY source that installed it. A later stage uses this for ref-counted per-source uninstall.
        public void WriteInstallManifest(string priconnePath, System.Collections.Generic.List<string> extractedRelPaths)
        {
            try
            {
                if (string.IsNullOrEmpty(priconnePath) || !Directory.Exists(priconnePath)) return;
                string owner = GetCurrentPatchSource().ShortName;
                string manifestPath = Path.Combine(priconnePath, "BepInEx", ".priconnerealltl-manifest.json");

                var files = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(manifestPath))
                {
                    try
                    {
                        var existing = JsonConvert.DeserializeObject<InstallManifest>(File.ReadAllText(manifestPath));
                        if (existing?.files != null)
                            foreach (var kv in existing.files) files[kv.Key] = kv.Value ?? new System.Collections.Generic.List<string>();
                    }
                    catch { }
                }

                var owned = new System.Collections.Generic.List<string>(extractedRelPaths ?? new System.Collections.Generic.List<string>());
                if (PluginProfileEnabled)
                    foreach (PluginDownload pd in GetCurrentPatchSource().PluginDownloads) owned.Add("BepInEx/plugins/" + pd.DllName);

                foreach (string rel in owned)
                {
                    string key = (rel ?? "").Replace('\\', '/');
                    if (key.Length == 0) continue;
                    if (!files.TryGetValue(key, out var owners)) { owners = new System.Collections.Generic.List<string>(); files[key] = owners; }
                    if (!owners.Contains(owner)) owners.Add(owner);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
                if (File.Exists(manifestPath)) File.SetAttributes(manifestPath, FileAttributes.Normal);   // clear Hidden so the overwrite succeeds
                File.WriteAllText(manifestPath, JsonConvert.SerializeObject(new InstallManifest { files = files }, Newtonsoft.Json.Formatting.Indented));
                // Hide it so users don't stumble on (or delete) it in Explorer. If deleted anyway, it's
                // never fatal — smart-clean falls back to a full clean and rewrites it on the next op.
                try { File.SetAttributes(manifestPath, FileAttributes.Hidden); } catch { }
            }
            catch (Exception ex) { Log?.Invoke("Could not write install manifest: " + ex.Message, "info", false); }
        }

        // Ref-counted uninstall plan for the CURRENT source. Returns the relative paths to delete
        // (files no source owns anymore) and rewrites the manifest to drop this source — deleting the
        // manifest entirely when no source remains. Files still owned by another source are KEPT (so
        // uninstalling TH from an EN+TH install leaves EN + the shared modloader engine intact).
        // Config/ignored files are left to the removeConfig/removeIgnored options, not ref-counted here.
        // Returns null when there's no usable manifest for this source → caller falls back to the tree.
        public System.Collections.Generic.List<string> ResolveManifestUninstall(string priconnePath)
        {
            try
            {
                if (string.IsNullOrEmpty(priconnePath)) return null;
                string manifestPath = Path.Combine(priconnePath, "BepInEx", ".priconnerealltl-manifest.json");
                if (!File.Exists(manifestPath)) return null;

                InstallManifest m;
                try { m = JsonConvert.DeserializeObject<InstallManifest>(File.ReadAllText(manifestPath)); }
                catch { return null; }
                if (m?.files == null || m.files.Count == 0) return null;

                string owner = GetCurrentPatchSource().ShortName;
                if (!m.files.Values.Any(o => o != null && o.Contains(owner))) return null;   // source not tracked → fall back

                var skip = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (Settings.Default.configFiles != null) foreach (string c in Settings.Default.configFiles) skip.Add(c.Replace('\\', '/'));
                // (ignored files are skipped on extract → never in the manifest, so no need to add them)

                var toDelete = new System.Collections.Generic.List<string>();
                var remaining = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in m.files)
                {
                    string rel = kv.Key;
                    var owners = kv.Value ?? new System.Collections.Generic.List<string>();
                    if (skip.Contains(rel)) { remaining[rel] = owners; continue; }   // config/ignored: option-managed, untouched
                    if (owners.Contains(owner))
                    {
                        owners.Remove(owner);
                        if (owners.Count == 0) toDelete.Add(rel);     // no source left → delete
                        else remaining[rel] = owners;                 // still shared → keep file + entry
                    }
                    else remaining[rel] = owners;                     // not this source's → keep
                }

                // Drop the manifest if nothing remains (last source removed); otherwise persist the rest.
                try
                {
                    if (File.Exists(manifestPath)) File.SetAttributes(manifestPath, FileAttributes.Normal);
                    if (remaining.Count == 0) File.Delete(manifestPath);
                    else
                    {
                        File.WriteAllText(manifestPath, JsonConvert.SerializeObject(new InstallManifest { files = remaining }, Newtonsoft.Json.Formatting.Indented));
                        try { File.SetAttributes(manifestPath, FileAttributes.Hidden); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    // #27: if the manifest couldn't be updated, the on-disk manifest is now STALE vs the
                    // delete plan — proceeding would let a later uninstall double-remove shared files.
                    // Bail to the safe ProcessTree fallback (return null) instead of returning the plan.
                    Log?.Invoke("Could not update the install manifest — falling back to full removal: " + ex.Message, "info", false);
                    return null;
                }

                return toDelete;
            }
            catch { return null; }
        }

        private sealed class InstallManifest
        {
            public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> files { get; set; }
        }

        // ─── Launch-shortcut wrapping (Arch B) ────────────────────────────────────
        // Pressing a wrapped shortcut runs an AutoUpdate (patch update) then launches the
        // shortcut's ORIGINAL target. The original target/args/workdir are base64-encoded
        // into the new Arguments so the wrap is self-contained + reversible.
        private static string B64(string s) =>
            string.IsNullOrEmpty(s) ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

        private static string DecodeFlag(string[] parts, string flag)
        {
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == flag)
                {
                    try { return Encoding.UTF8.GetString(Convert.FromBase64String(parts[i + 1])); }
                    catch { return null; }
                }
            }
            return null;
        }

        /// <summary>True if the .lnk already routes through this installer (TargetPath == our exe).</summary>
        public bool IsWrappedShortcut(string lnkPath)
        {
            try
            {
                if (!File.Exists(lnkPath)) return false;
                var wsh = new IWshRuntimeLibrary.WshShell();
                var sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(lnkPath);
                string installerExe = Assembly.GetExecutingAssembly().Location;
                return string.Equals(sc.TargetPath, installerExe, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        /// <summary>Rewrites an existing launcher .lnk to route through the installer
        /// (update → launch original). Keeps the original icon. Idempotent.</summary>
        public bool WrapShortcut(string lnkPath)
        {
            try
            {
                if (!File.Exists(lnkPath)) { ErrorLog?.Invoke($"Shortcut not found: {lnkPath}"); return false; }

                string installerExe = Assembly.GetExecutingAssembly().Location;
                string installerDir = Path.GetDirectoryName(installerExe);

                var wsh = new IWshRuntimeLibrary.WshShell();
                var sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(lnkPath);

                if (string.Equals(sc.TargetPath, installerExe, StringComparison.OrdinalIgnoreCase))
                {
                    Log?.Invoke($"Shortcut already routes through the installer: {Path.GetFileName(lnkPath)}", "info", false);
                    return true;
                }

                string origTarget = sc.TargetPath;
                if (string.IsNullOrEmpty(origTarget)) { ErrorLog?.Invoke($"Shortcut has no target to wrap: {lnkPath}"); return false; }
                string origArgs = sc.Arguments;
                string origDir = sc.WorkingDirectory;

                sc.TargetPath = installerExe;
                sc.WorkingDirectory = installerDir;
                sc.Arguments = $"autoupdate --launch {B64(origTarget)}"
                             + (string.IsNullOrEmpty(origArgs) ? "" : $" --targs {B64(origArgs)}")
                             + (string.IsNullOrEmpty(origDir) ? "" : $" --tdir {B64(origDir)}");
                // IconLocation left untouched -> the shortcut still looks the same.
                sc.Save();
                Log?.Invoke($"Wrapped shortcut (update+launch): {Path.GetFileName(lnkPath)} → {Path.GetFileName(origTarget)}", "success", false);
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke($"Error wrapping shortcut: {ex.Message}");
                return false;
            }
        }

        /// <summary>Reverses WrapShortcut using the base64 payload in the .lnk Arguments.</summary>
        public bool RestoreShortcut(string lnkPath)
        {
            try
            {
                if (!File.Exists(lnkPath)) { ErrorLog?.Invoke($"Shortcut not found: {lnkPath}"); return false; }

                var wsh = new IWshRuntimeLibrary.WshShell();
                var sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(lnkPath);

                string[] parts = (sc.Arguments ?? "").Split(' ');
                string target = DecodeFlag(parts, "--launch");
                if (string.IsNullOrEmpty(target))
                {
                    Log?.Invoke($"Not a wrapped shortcut (nothing to restore): {Path.GetFileName(lnkPath)}", "info", false);
                    return false;
                }
                sc.TargetPath = target;
                sc.Arguments = DecodeFlag(parts, "--targs") ?? "";
                string dir = DecodeFlag(parts, "--tdir");
                if (!string.IsNullOrEmpty(dir)) sc.WorkingDirectory = dir;
                sc.Save();
                Log?.Invoke($"Restored shortcut to its original launcher: {Path.GetFileName(lnkPath)}", "success", false);
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke($"Error restoring shortcut: {ex.Message}");
                return false;
            }
        }

        public void CreateAutoUpdaterShortcut(string priconnePath)
        {
            DialogResult messageboxResult = MessageBox.Show("The AutoUpdater is a modified version of the PriconneReALLTL-Installer, which automatically performs an update and launches the game after with the selected launcher." +
                "\n\nWould you like to create an AutoUpdate shortcut?", "Create shortcut?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (messageboxResult == DialogResult.No) return;

            string currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string executableName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string targetPath = Path.Combine(currentDirectory, executableName);

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveFileDialog.FileName = "PriconneReALLTL-Installer AutoUpdater";
            saveFileDialog.Filter = "Shortcut files (*.lnk)|*.lnk";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                var wshShell = new IWshRuntimeLibrary.WshShell();
                string shortcutPath = saveFileDialog.FileName;
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.Description = "PriconneReALLTL-Installer AutoUpdater";
                shortcut.WorkingDirectory = currentDirectory;
                shortcut.IconLocation = Path.Combine(priconnePath, "PrincessConnectReDive.exe");
                shortcut.Arguments = "autoupdate";

                shortcut.Save();

                System.Diagnostics.Debug.WriteLine("Shortcut created successfully!");

                MessageBox.Show("Shortcut created!\n\nPlease note that the shortcut points to the PriconneReALLTL-Installer! If you move or remove the installer, you have to recreate the shortcut!", "Shortcut created!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Operation canceled by the user.");
            }
        }

        public void CheckForInstallerUpdate(string version, string body, string installerAssetLink, bool versionValid)
        {
            int versioncompare = CompareVersions(Application.ProductVersion, version);

            if (versionValid && versioncompare < 0)
            {
                SelfUpdateForm SelfUpdateForm = new SelfUpdateForm(version, body, installerAssetLink);
                SelfUpdateForm.ShowDialog();
            }
        }
        public static bool IsFileInSubfolder(string folderPath, string filePath)
        {
            folderPath = Path.GetFullPath(folderPath); // Ensure the folder path is full.
            filePath = Path.GetFullPath(filePath);     // Ensure the file path is full.

            // Check if the file path starts with the folder path.
            return filePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase);
        }
        // Session cache for the last validated token (valid results only — failures aren't cached,
        // so a transient error is retried). Cuts the repeated /user calls that each GetLatest* and
        // every UI refresh would otherwise make on the UI thread.
        private static string _validatedTokenHash;   // #28: SHA-256 of the last validated token — NEVER hold the plaintext in a long-lived static field (credential-vault)
        private static string _validatedUser;
        private static string TokenHash(string t)
        {
            if (string.IsNullOrEmpty(t)) return null;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(t))).Replace("-", "");
        }
        public static (bool, string) ValidateGitHubToken(string token)
        {
            string username = null;

            if (string.IsNullOrWhiteSpace(token))
                return (false, null);

            if (_validatedTokenHash != null && TokenHash(token) == _validatedTokenHash)   // #28: compare by hash, not the plaintext token
                return (true, _validatedUser);

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "PriconneReALLTLInstaller");
                    client.Headers.Add("Authorization", "token " + token);

                    string response = client.DownloadString("https://api.github.com/user");
                    dynamic userJson = JsonConvert.DeserializeObject(response);

                    username = userJson.login;
                    _validatedTokenHash = TokenHash(token);   // #28: cache the HASH of valid tokens only (not the plaintext)
                    _validatedUser = username;
                    return (true, username); // Token is valid
                }
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null)
                {
                    using (var reader = new StreamReader(webEx.Response.GetResponseStream()))
                    {
                        string errorResponse = reader.ReadToEnd();
                        try
                        {
                            dynamic errorJson = JsonConvert.DeserializeObject(errorResponse);
                            string message = errorJson.message?.ToString();

                            if (message?.Contains("Bad credentials") == true)
                                return (false, null);
                        }
                        catch { }
                    }
                }
                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }
        public static (int remaining, DateTime resetTime, TimeSpan timeUntilReset, string username) CheckGithubRateLimit()
        {
            try
            {
                string gitHubToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
                (bool tokenvalid, string username) = Helper.ValidateGitHubToken(gitHubToken);

                string rateUrl = "https://api.github.com/rate_limit";
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "PriconneReALLTLInstaller");
                    if (tokenvalid) client.Headers.Add("Authorization", $"Bearer {gitHubToken}");
                    string response = client.DownloadString(rateUrl);
                    dynamic json = JsonConvert.DeserializeObject(response);

                    int remaining = json.rate.remaining;
                    long resetUnix = json.rate.reset;
                    DateTime resetTime = DateTimeOffset.FromUnixTimeSeconds(resetUnix).ToLocalTime().DateTime;
                    TimeSpan timeUntilReset = resetTime - DateTime.Now;

                    return (remaining, resetTime, timeUntilReset, username);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking rate limit: {ex.Message}");
                return (-1, DateTime.MinValue, TimeSpan.Zero, null); // Fallback values on error
            }
        }

        public (bool,string) CompareGameandModloaderVersions(string gameVersion, string modloaderLocalVersion, string modLoaderLatestRelease)
        {
            // Guard non-numeric strings ("Not found", "ERROR!", "N/A", raw GitHub content with
            // whitespace) — a raw new Version(...) would throw ArgumentException/FormatException and
            // crash the caller (MainForm.UpdateUI / AutoUpdateForm.UpdateUI).
            if (!Version.TryParse((gameVersion ?? "").Trim(), out Version a)
                || !Version.TryParse((modloaderLocalVersion ?? "").Trim(), out Version b)
                || !Version.TryParse((modLoaderLatestRelease ?? "").Trim(), out Version c))
                return (false, null);

            if (a < b && a < c)
            {
                return (true, "Your game version is lower than the modloader version.\nPlease update your game!");
            }
            else if (a > b && a == c)
            {
                return (true, "Your modloader version is outdated.\nPlease update your TL patch!");
            }
            else if (a > b && a > c && b == c)
            {
                return (true, "Your modloader version is the latest available, but still lower than the game version.\nPlease wait for an updated TL patch release with the up-to-date modloader!");
            }

            return (false, null);
        }
        public static StringCollection DeserializeStringCollection(string serializedValue)
        {
            var stringCollection = new StringCollection();
            var serializer = new XmlSerializer(stringCollection.GetType());

            // #30: harden XML deserialization against XXE — disable DTD + external-entity resolution
            // (this is reachable from the user-chosen ImportSettings file).
            var xmlSettings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null };
            using (var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(serializedValue), xmlSettings))
            {
                if (serializer.CanDeserialize(reader))
                {
                    stringCollection = (StringCollection)serializer.Deserialize(reader);
                }
            }

            return stringCollection;
        }
        public static string GetRelativePath(string fromPath, string toPath)
        {
            fromPath = fromPath.Replace("\\", "/");
            toPath = toPath.Replace("\\", "/");

            if (!toPath.StartsWith(fromPath, StringComparison.OrdinalIgnoreCase))
            {
                // If toPath is not under fromPath, return the full toPath.
                return toPath;
            }

            int fromPathLength = fromPath.Length;
            if (fromPathLength < toPath.Length)
            {
                // Exclude the common portion and the path separator if it exists
                string relativePath = toPath.Substring(fromPathLength).TrimStart('/');
                return relativePath;
            }

            // If fromPath is the same as toPath, return an empty string
            return string.Empty;
        }

        public static void SetDefaultDMMConfigPath()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.DMMConfigPath))
            {
                Settings.Default.DMMConfigPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "dmmgameplayer5", "dmmgame.cnf"
                );
                Settings.Default.Save();
            }
        }

        public static void EnsureDMMConfigPathValid()
        {
            string path = Settings.Default.DMMConfigPath;

            if (!File.Exists(path))
            {
                var result = MessageBox.Show(
                    $"The DMMGamePlayer config file cannot be found at: {Settings.Default.DMMConfigPath}\n\n" +
                    "Would you like to locate it manually?",
                    "Config File Missing",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Title = "Select DMMGamePlayer Config File";
                        openFileDialog.Filter = "DMM Config File (dmmgame.cnf)|dmmgame.cnf|All Files (*.*)|*.*";
                        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            Settings.Default.DMMConfigPath = openFileDialog.FileName;
                            Settings.Default.Save();
                        }
                    }
                }
            }
        }

        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            try
            {
                byte[] protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(protectedBytes);
            }
            finally
            {
                // Zero the plaintext secret buffer after use (credential-vault.md DPAPI rule).
                Array.Clear(bytes, 0, bytes.Length);
            }
        }

        public static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                byte[] bytes = Convert.FromBase64String(encryptedText);
                byte[] unprotectedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                try
                {
                    return Encoding.UTF8.GetString(unprotectedBytes);
                }
                finally
                {
                    // Zero the decrypted secret buffer after use (credential-vault.md DPAPI rule).
                    Array.Clear(unprotectedBytes, 0, unprotectedBytes.Length);
                }
            }
            catch
            {
                // Corrupt / foreign DPAPI blob or bad base64 (e.g. settings copied from another
                // Windows account or hand-edited) — treat as "no token" instead of crashing every caller.
                return null;
            }
        }
        public void ExportSettings(string filePath)
        {
            try
            {
                var userSettings = new UserSettings
                {
                    launchState = Settings.Default.launchState,
                    ignoreFiles = Settings.Default.ignoreFiles,
                    fastLauncherLink = Settings.Default.fastLauncherLink,
                    fastLauncherLinks = Settings.Default.fastLauncherLinks,
                    LastKnownVersion = Settings.Default.LastKnownVersion,
                    checkForInstallerUpdates = Settings.Default.checkForInstallerUpdates,
                    showLogChecked = Settings.Default.showLogChecked
                };

                // Serialize user settings to XML
                XmlSerializer serializer = new XmlSerializer(typeof(UserSettings));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, userSettings);
                }
            }
            catch (Exception)
            {
                throw;
            }
            
        }
        public void ImportSettings(string filePath)
        {
            try
            {
                // Deserialize settings from file
                XmlSerializer serializer = new XmlSerializer(typeof(UserSettings));
                using (StreamReader reader = new StreamReader(filePath))
                {
                    var importedSettings = (UserSettings)serializer.Deserialize(reader);

                    // Update application settings with imported settings
                    Settings.Default.launchState = importedSettings.launchState;
                    Settings.Default.ignoreFiles = importedSettings.ignoreFiles;
                    Settings.Default.fastLauncherLink = importedSettings.fastLauncherLink;
                    if (importedSettings.fastLauncherLinks != null)
                        Settings.Default.fastLauncherLinks = importedSettings.fastLauncherLinks;
                    Settings.Default.LastKnownVersion = importedSettings.LastKnownVersion;
                    Settings.Default.checkForInstallerUpdates = importedSettings.checkForInstallerUpdates;
                    Settings.Default.showLogChecked = importedSettings.showLogChecked;

                    // Save changes to application settings
                    Settings.Default.Save();
                }
            }
            catch (Exception)
            {
                throw;
            }
           
        }
    }
}

[Serializable]
public class UserSettings
{
    public bool launchState { get; set; }
    public System.Collections.Specialized.StringCollection ignoreFiles { get; set; }
    public string fastLauncherLink { get; set; }   // legacy — kept for backward compat
    public System.Collections.Specialized.StringCollection fastLauncherLinks { get; set; }
    public string LastKnownVersion { get; set; }
    public bool checkForInstallerUpdates { get; set; }
    public bool showLogChecked { get; set; }
}