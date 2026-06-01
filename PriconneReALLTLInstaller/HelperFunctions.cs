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
            string[] ignoreFiles = new string[Settings.Default.ignoreFiles.Count];
            Settings.Default.ignoreFiles.CopyTo(ignoreFiles, 0);

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

                    Console.WriteLine($"Detected process path: {exePath}");

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
                    Console.WriteLine($"Cannot access process {process.ProcessName}: {ex.Message}");
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
        public bool IsFastLauncherInstalled()
        {
            try
            {
                string dmmFastLauncherPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DMMGamePlayerFastLauncher");
                string dmmFastLauncherExe = Path.Combine(dmmFastLauncherPath, "DMMGamePlayerFastLauncher.exe");

                if (File.Exists(dmmFastLauncherExe)) return true; else return false;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error checking DMMGamePlayerFastlauncher: " + ex.Message);
                return false;
            }
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
                    Console.WriteLine($"PMAL registry probe failed ({probe.hive}/{probe.view}): {ex.Message}");
                }
            }

            return null;
        }

        public bool IsPriconneMultiLauncherInstalled()
        {
            try
            {
                return File.Exists(GetPriconneMultiLauncherExePath());
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error checking PriconneMultiAccountLauncher: " + ex.Message);
                return false;
            }
        }
        public bool IsFastLauncherShortcutValid()
        {
            var links = GetFastLauncherLinks();
            // Consider valid if at least one stored link actually exists on disk
            bool anyValid = links.Any(l => File.Exists(l));
            if (!anyValid)
            {
                MessageBox.Show(Settings.Default.cannotStartDMMFastLauncherError, "Cannot Start Game", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
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

        // Enable a plugin: restore "<dll>.bak" -> "<dll>" so BepInEx loads it again.
        private void RestorePluginDll(string pluginsDir, string dll)
        {
            string dllPath = Path.Combine(pluginsDir, dll);
            if (File.Exists(dllPath)) return;                  // already enabled
            string bakPath = dllPath + ".bak";
            if (!File.Exists(bakPath)) return;                 // nothing to restore
            File.Move(bakPath, dllPath);
            Log?.Invoke($"Enabled plugin for this TL source: {dll}", "info", false);
        }

        /// <summary>Canonicalizes a version/tag for comparison: trims and strips a leading
        /// "v"/"V" so "V2.1.3", "v2.1.3" and "2.1.3" all compare equal. EN date strings
        /// (e.g. "20260531") are unaffected. For comparison/display-matching ONLY — the raw
        /// tag is still needed for GitHub API calls (git/ref/tags/{tag}).</summary>
        public static string NormalizeVersion(string v) =>
            string.IsNullOrEmpty(v) ? "" : v.Trim().TrimStart('v', 'V');

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
        public static bool BypassVersionCache = false;

        private sealed class CacheEntry { public string Val { get; set; } public DateTime Ts { get; set; } }

        /// <summary>Cached JSON value for key if fresh (&lt; TTL) and not bypassed; else null.</summary>
        public static string GetCachedVersion(string key, bool allowStale = false)
        {
            // allowStale: ignore the TTL AND the bypass flag — used as a fallback when a live fetch
            // fails (e.g. HTTP 403 rate-limit) so the app keeps showing the last known value tokenless
            // instead of "N/A", even during a source-switch (which sets BypassVersionCache).
            if (BypassVersionCache && !allowStale) return null;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(Settings.Default.versionCacheJson ?? "");
                if (dict != null && dict.TryGetValue(key, out var e) && e != null
                    && (allowStale || (DateTime.UtcNow - e.Ts).TotalHours < VersionCacheTtlHours))
                    return e.Val;
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

        public void PopulatePatchSourceComboBox(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            foreach (var source in PatchSources) comboBox.Items.Add(source.DisplayName);
        }

        public void PopulateLauncherComboBox(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add("DMMGamePlayer");
            comboBox.Items.Add("DMMGamePlayerFastLauncher");
            comboBox.Items.Add("PriconneMultiAccountLauncher");

            if (IsFastLauncherInstalled())
            {
                Log?.Invoke("Found DMMGamePlayerFastLauncher!", "info", false);
            } else Log?.Invoke("DMMGamePlayerFastLauncher not installed!", "info", false);

            if (IsPriconneMultiLauncherInstalled())
            {
                Log?.Invoke("Found PriconneMultiAccountLauncher!", "info", false);
            } else Log?.Invoke("PriconneMultiAccountLauncher not installed!", "info", false);
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
                foreach (PluginDownload pd in GetCurrentPatchSource().PluginDownloads) owned.Add("BepInEx/plugins/" + pd.DllName);

                foreach (string rel in owned)
                {
                    string key = (rel ?? "").Replace('\\', '/');
                    if (key.Length == 0) continue;
                    if (!files.TryGetValue(key, out var owners)) { owners = new System.Collections.Generic.List<string>(); files[key] = owners; }
                    if (!owners.Contains(owner)) owners.Add(owner);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
                File.WriteAllText(manifestPath, JsonConvert.SerializeObject(new InstallManifest { files = files }, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex) { Log?.Invoke("Could not write install manifest: " + ex.Message, "info", false); }
        }

        private sealed class InstallManifest
        {
            public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> files { get; set; }
        }

        public void PopulateConfigChecklistbox(CheckedListBox checkedListBox)
        {
            checkedListBox.Items.Clear();
            checkedListBox.Items.AddRange(Settings.Default.configFiles.Cast<object>().ToArray());

            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                checkedListBox.SetItemChecked(i, true);
            }
        }
        // Fills the "Remove Ignored Patch Files" detail list with the user's ignored paths
        // (mirrors PopulateConfigChecklistbox). All checked = all will be removed.
        public void PopulateIgnoredChecklistbox(CheckedListBox checkedListBox)
        {
            checkedListBox.Items.Clear();
            if (Settings.Default.ignoreFiles != null)
                checkedListBox.Items.AddRange(Settings.Default.ignoreFiles.Cast<object>().ToArray());

            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                checkedListBox.SetItemChecked(i, true);
            }
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

                Console.WriteLine("Shortcut created successfully!");

                MessageBox.Show("Shortcut created!\n\nPlease note that the shortcut points to the PriconneReALLTL-Installer! If you move or remove the installer, you have to recreate the shortcut!", "Shortcut created!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Console.WriteLine("Operation canceled by the user.");
            }
        }

        public void CheckForInstallerUpdate(string version, string body, string installerAssetLink, bool versionValid)
        {
            int versioncompare = NormalizeVersion(String.Format(Application.ProductVersion)).CompareTo(NormalizeVersion(version));

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
        private static string _validatedToken;
        private static string _validatedUser;
        public static (bool, string) ValidateGitHubToken(string token)
        {
            string username = null;

            if (string.IsNullOrWhiteSpace(token))
                return (false, null);

            if (token == _validatedToken)
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
                    _validatedToken = token;   // cache valid tokens only
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
                Console.WriteLine($"Error checking rate limit: {ex.Message}");
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

            using (var reader = new XmlTextReader(new System.IO.StringReader(serializedValue)))
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
            byte[] protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        public static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                byte[] bytes = Convert.FromBase64String(encryptedText);
                byte[] unprotectedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(unprotectedBytes);
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
                    selectedLauncher = Settings.Default.selectedLauncher,
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
                    Settings.Default.selectedLauncher = importedSettings.selectedLauncher;
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
    public int selectedLauncher {  get; set; }
    public System.Collections.Specialized.StringCollection ignoreFiles { get; set; }
    public string fastLauncherLink { get; set; }   // legacy — kept for backward compat
    public System.Collections.Specialized.StringCollection fastLauncherLinks { get; set; }
    public string LastKnownVersion { get; set; }
    public bool checkForInstallerUpdates { get; set; }
    public bool showLogChecked { get; set; }
}