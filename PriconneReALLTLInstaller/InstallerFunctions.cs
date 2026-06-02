using HelperFunctions;
using LoggerFunctions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PriconneReALLTLInstaller;
using PriconneReALLTLInstaller.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace InstallerFunctions
{
    public class Installer
    {
        Helper helper = new Helper();

        private string assetLink;
        private string priconnePath;
        private bool priconnePathValid;
        private string gameVersion;
        private string localVersion;
        private bool localVersionValid;
        private string latestVersion;
        private bool latestVersionValid;
        private string _lastLoggedPatchVer;   // de-dupe "Found ... installed!" logs (read many times/op + Load+Shown)
        private string _lastLoggedModVer;
        private string tempFile = Path.GetTempFileName();
        private bool removeSuccess = true;
        private bool downloadSuccess = true;
        private bool extractSuccess = true;
        private bool removeProgress = false;
        private bool cancelledByUser = false;
        public event Action<double, double> DownloadProgress;
        public event Action<Image> ProgressPictureChange;
        public event Action<string, string, bool> Log;
        public event Action<string> ErrorLog;
        public event Action DisableStart;
        public event Action ProcessStart;
        public event Action ProcessFinish;
        public event Action ProcessError;

        public (string priconnePath, bool priconnePathValid, string gameVersion) GetGamePath()
        {
            try
            {

                string cfgFileContent = File.ReadAllText(Settings.Default.DMMConfigPath);
                dynamic cfgJson = JsonConvert.DeserializeObject(cfgFileContent);

                if (cfgJson != null && cfgJson.contents != null)
                {
                    foreach (var content in cfgJson.contents)
                    {
                        if (content.productId == "priconner")
                        {
                            #if DEBUG
                            priconnePath = "C:\\Test";
                            #else
                            priconnePath = content.detail.path;
                            #endif

                            gameVersion = content.detail.version;

                            Log?.Invoke("Found Princess Connect Re:Dive in " + priconnePath, "info", false);
                            return (priconnePath, priconnePathValid = true, gameVersion);
                        }
                    }
                }
                ErrorLog?.Invoke("Cannot find the game path! Did you install Princess Connect Re:Dive from DMMGamePlayer?");
                DisableStart?.Invoke();
                return (priconnePath = "Not found", priconnePathValid = false, gameVersion = "Not found");
            }
            catch (FileNotFoundException)
            {
                ErrorLog?.Invoke("Cannot find the DMMGamePlayer config file! Do you have DMMGamePlayer installed?");
                DisableStart?.Invoke();
                return (priconnePath = "Not found", priconnePathValid = false, gameVersion = "Not found");
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error getting game path: " + ex.Message);
                DisableStart?.Invoke();
                return (priconnePath = "ERROR!", priconnePathValid = false, gameVersion = "ERROR!");
            }
        }
        public (string localVersion, bool localVersionValid) GetInstalledPatchVersion()
        {
            try
            {

                if (!priconnePathValid)
                {
                    ErrorLog?.Invoke("Game path not valid, cannot determine installed patch version!");
                    return (localVersion = "N/A", localVersionValid = false);
                }

                Helper.PatchSource src = Helper.GetCurrentPatchSource();
                string tlVersionFilePath = Path.Combine(priconnePath, src.VersionFileRelPath);

                if (!File.Exists(tlVersionFilePath))
                {
                    return (localVersion = "None", localVersionValid = false);
                }
                string rawVersionFile = File.ReadAllText(tlVersionFilePath);
                Match match = Regex.Match(rawVersionFile, src.VersionRegex);

                if (match == null || !match.Success)
                {
                    return (localVersion = "Invalid", localVersionValid = false);
                }
                localVersion = match.Value;
                if (_lastLoggedPatchVer != localVersion) { Log?.Invoke($"Found TL patch version {localVersion} installed!", "info", false); _lastLoggedPatchVer = localVersion; }
                return (localVersion, localVersionValid = true);

            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error getting installed patch version: " + ex.Message);
                return (localVersion = "ERROR!", localVersionValid = false);
            }
        }

        public (string, bool) GetInstalledModloaderVersion()
        {
            try
            {

                if (!priconnePathValid)
                {
                    ErrorLog?.Invoke("Game path not valid, cannot determine installed modloader version!");
                    return ("N/A", false);
                }

                string modloaderVersionFilePath = Path.Combine(priconnePath, "BepInEx", "interop", "version");

                if (!File.Exists(modloaderVersionFilePath))
                {
                    return ("None", false);
                }
                string rawVersionFile = File.ReadAllText(modloaderVersionFilePath);
                Match match = Regex.Match(rawVersionFile, @"\b\d{1,2}\.\d{1,2}\.\d{1,2}\b");

                if (match == null || !match.Success)
                {
                    return ("Invalid", false);
                }

                if (_lastLoggedModVer != match.Value) { Log?.Invoke($"Found modloader version {match.Value} installed!", "info", false); _lastLoggedModVer = match.Value; }
                return (match.Value, true);

            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error getting installed modloader version: " + ex.Message);
                return ("ERROR!", false);
            }
        }

        public (string latestVersion, bool latestVersionValid, string assetLink) GetLatestPatchRelease(string githubAPI)
        {
            
            try
            {
                string releaseUrl = githubAPI + "/releases/latest";
                string gitHubToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
                (bool tokenvalid, _) = Helper.ValidateGitHubToken(gitHubToken);

                string cacheKey = "patch:" + githubAPI;
                string cachedPatch = Helper.GetCachedVersion(cacheKey);
                if (cachedPatch != null)
                {
                    try { var cj = JObject.Parse(cachedPatch); return (latestVersion = (string)cj["v"], latestVersionValid = true, assetLink = (string)cj["a"]); }
                    catch { }
                }

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "PriconneReALLTLInstaller");
                    if (tokenvalid) client.Headers.Add("Authorization", $"Bearer {gitHubToken}");
                    string response = client.DownloadString(releaseUrl);
                    dynamic releaseJson = JsonConvert.DeserializeObject(response);
                    if (releaseJson == null)
                    {
                        Log?.Invoke("Empty response from GitHub — skipping patch version check.", "info", false);
                        return (latestVersion = null, latestVersionValid = false, null);
                    }
                    string version = releaseJson.tag_name;
                    // Pick the .zip patch asset (some sources also ship an .exe installer
                    // in the same release); fall back to the first asset if none match.
                    assetLink = null;
                    foreach (var asset in releaseJson.assets)
                    {
                        string assetName = (string)asset.name;
                        if (!string.IsNullOrEmpty(assetName) && assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            assetLink = (string)asset.browser_download_url;
                            break;
                        }
                    }
                    if (assetLink == null)
                    {
                        if (releaseJson.assets == null || releaseJson.assets.Count == 0)
                        {
                            Log?.Invoke("Latest release has no downloadable asset yet — skipping.", "info", false);
                            return (latestVersion = null, latestVersionValid = false, null);
                        }
                        assetLink = releaseJson.assets[0].browser_download_url;
                    }
                    Helper.SetCachedVersion(cacheKey, new JObject { ["v"] = version, ["a"] = assetLink }.ToString(Newtonsoft.Json.Formatting.None));
                    return (latestVersion = version, latestVersionValid = true, assetLink);
                }
            }
            catch (WebException webEx)
            {
                // GitHub error (often a 403 rate-limit without a token). Fall back to the last known
                // cached value so the app keeps working tokenless after any prior success — even mid
                // source-switch (allowStale ignores the bypass flag).
                string stale = Helper.GetCachedVersion("patch:" + githubAPI, allowStale: true);
                if (stale != null)
                {
                    try { var cj = JObject.Parse(stale); Log?.Invoke("Using last known patch version (GitHub unavailable / rate-limited).", "info", false); return (latestVersion = (string)cj["v"], latestVersionValid = true, assetLink = (string)cj["a"]); }
                    catch { }
                }
                HttpWebResponse resp = webEx.Response as HttpWebResponse;
                string detail = resp != null ? $"HTTP {(int)resp.StatusCode}" : webEx.Message;
                Log?.Invoke($"Could not check latest patch version ({detail}). Set a GitHub token to avoid rate limits.", "info", false);
                return (latestVersion = null, latestVersionValid = false, null);
            }
            catch (Exception ex)
            {
                Log?.Invoke("Could not check latest patch version: " + ex.Message, "info", false);
                return (latestVersion = null, latestVersionValid = false, null);
            }
        }

        public (string, string) GetLatestModloaderRelease()
        {
            string gitHubToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
            (bool tokenvalid, _) = Helper.ValidateGitHubToken(gitHubToken);
            try
            {
                string cachedMl = Helper.GetCachedVersion("modloader");
                if (cachedMl != null)
                {
                    try { var cj = JObject.Parse(cachedMl); return ((string)cj["v"], (string)cj["s"]); }
                    catch { }
                }

                // Modloader is authoritative from ImaterialC (the main, widely-used source),
                // independent of the selected TL source. Fetch ImaterialC's own latest release
                // and read its bundled BepInEx interop version.
                Helper.PatchSource ml = Helper.ModloaderSource;
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "PriconneReALLTLInstaller");
                    if (tokenvalid) client.Headers.Add("Authorization", $"Bearer {gitHubToken}");

                    string mlReleaseResponse = client.DownloadString(ml.ApiBase + "/releases/latest");
                    string mlTag = (string)JObject.Parse(mlReleaseResponse)["tag_name"];
                    if (string.IsNullOrEmpty(mlTag)) return (null, null);

                    // Read the bundled interop version directly at the release TAG via raw.githubusercontent
                    // (raw accepts a tag ref) — this skips the extra git/ref/tags API call, so the modloader
                    // check costs ONE rate-limited request instead of two. raw.githubusercontent doesn't
                    // count against the GitHub API rate limit, easing the tokenless 403s.
                    string fileUrl = $"{ml.RawBase}/{mlTag}/src/BepInEx/interop/version";
                    string fileVersion = client.DownloadString(fileUrl).Trim();

                    Helper.SetCachedVersion("modloader", new JObject { ["v"] = fileVersion, ["s"] = mlTag }.ToString(Newtonsoft.Json.Formatting.None));
                    return (fileVersion, mlTag);
                }
            }
            catch (WebException webEx)
            {
                // Fall back to the last known modloader version so a 403 rate-limit shows the cached
                // value instead of "N/A" — works tokenless after any prior success, even mid
                // source-switch (allowStale ignores the bypass flag).
                string stale = Helper.GetCachedVersion("modloader", allowStale: true);
                if (stale != null)
                {
                    try { var cj = JObject.Parse(stale); return ((string)cj["v"], (string)cj["s"]); }
                    catch { }
                }
                // Modloader-latest is informational (the patch zip already bundles the modloader),
                // so a fetch failure is a soft warning — not a red error, and it no longer blocks
                // operations. Common cause: GitHub rate limit / 403 without an API token.
                HttpWebResponse resp = webEx.Response as HttpWebResponse;
                bool rateLimited = resp != null && (int)resp.StatusCode == 403;
                string detail = resp != null ? $"HTTP {(int)resp.StatusCode}" : webEx.Message;
                Log?.Invoke(rateLimited
                    ? "Modloader 'Latest' check skipped — GitHub rate limit (no token). This is HARMLESS: nothing is broken, the installed modloader is untouched and all operations work. Set a GitHub token (optional) to show the latest number."
                    : $"Modloader 'Latest' check skipped ({detail}) — harmless; the installed modloader is unaffected and operations work.", "info", false);
                return (null, null);
            }
            catch (Exception ex)
            {
                Log?.Invoke("Could not check latest modloader version: " + ex.Message, "info", false);
                return (null, null);
            }


        }
        public (string version, string body, string assetLink, bool versionValid) GetLatestInstallerRelease()
        {
            string gitHubToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
            (bool tokenvalid, _) = Helper.ValidateGitHubToken(gitHubToken);
            try
            {
                string releaseUrl = "https://api.github.com/repos/HetCreep/PriconneReALLTL-Installer/releases/latest";

                string cachedInst = Helper.GetCachedVersion("installer");
                if (cachedInst != null)
                {
                    try { var cj = JObject.Parse(cachedInst); return ((string)cj["v"], (string)cj["b"], (string)cj["a"], true); }
                    catch { }
                }

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "PriconneReALLTLInstaller");
                    if (tokenvalid) client.Headers.Add("Authorization", $"Bearer {gitHubToken}");
                    string response = client.DownloadString(releaseUrl);
                    dynamic releaseJson = JsonConvert.DeserializeObject(response);
                    if (releaseJson == null)
                    {
                        Log?.Invoke("Empty response from GitHub — skipping installer update check.", "info", false);
                        return (null, null, null, false);
                    }
                    string version = releaseJson.tag_name;
                    string body = releaseJson.body;
                    if (releaseJson.assets == null || releaseJson.assets.Count == 0)
                    {
                        Log?.Invoke("No installer release asset found yet — skipping installer update.", "info", false);
                        return (null, null, null, false);
                    }
                    assetLink = releaseJson.assets[0].browser_download_url;
                    Helper.SetCachedVersion("installer", new JObject { ["v"] = version, ["b"] = body, ["a"] = assetLink }.ToString(Newtonsoft.Json.Formatting.None));
                    return (version, body, assetLink, true);
                }
            }
            catch (WebException webEx)
            {
                // A missing release (HTTP 404) just means there is no installer update to
                // fetch yet (e.g. a fresh fork before its first release) — treat it as
                // "no update", not an error, so the main UI doesn't show a red ERROR.
                if (webEx.Response is HttpWebResponse notFoundResp && notFoundResp.StatusCode == HttpStatusCode.NotFound)
                {
                    Log?.Invoke("No installer release found yet — skipping installer update check.", "info", false);
                    return (null, null, null, false);
                }
                // Check if the response contains JSON data (which happens in case of API errors)
                if (webEx.Response != null)
                {
                    using (var reader = new StreamReader(webEx.Response.GetResponseStream()))
                    {
                        string errorResponse = reader.ReadToEnd();
                        try
                        {
                            dynamic errorJson = JsonConvert.DeserializeObject(errorResponse);
                            string errorMessage = errorJson.message;
                            ErrorLog?.Invoke("Error getting installer release: " + errorMessage);
                        }
                        catch (Exception innerEx)
                        {
                            ErrorLog?.Invoke("Error reading API error message: " + innerEx.Message);
                        }
                    }
                }
                else
                {
                    ErrorLog?.Invoke("Error getting installer release: " + webEx.Message);
                }

                return (null, null, null, false);
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error getting installer release: " + ex.Message);
                return (null, null, null, false);
            }
        }

        // Local zip-download cache: the patch ships as one big (~330MB) bundled zip, so re-downloading
        // it on every reinstall is wasteful. We keep the last zip per source+version under TEMP and
        // reuse it until the release version changes (then the stale one is purged + replaced).
        private static string ZipCacheDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PriconneReALLTLInstaller", "zipcache");

        // Logs the current zip-cache contents — called at startup so every session's log shows what's
        // cached for reuse (the session log is truncated each launch, so a past hit line wouldn't persist).
        public void LogCacheStatus()
        {
            try
            {
                var files = Directory.Exists(ZipCacheDir) ? Directory.GetFiles(ZipCacheDir, "*.zip") : new string[0];
                if (files.Length == 0) { Log?.Invoke("Zip cache: empty — the next patch download will be cached for reuse.", "info", false); return; }
                foreach (string f in files)
                    Log?.Invoke($"Zip cache: {Path.GetFileName(f)} ({new FileInfo(f).Length / (1024 * 1024)} MB) ready for reuse.", "info", false);
            }
            catch { }
        }

        // Delete all cached patch zips (the user-facing "Clear download cache"); returns bytes freed.
        public long ClearZipCache()
        {
            long freed = 0;
            try
            {
                if (!Directory.Exists(ZipCacheDir)) return 0;
                foreach (string f in Directory.GetFiles(ZipCacheDir, "*.zip"))
                    try { long len = new FileInfo(f).Length; File.Delete(f); freed += len; } catch { }
            }
            catch { }
            return freed;
        }

        private string GetCachedZipPath()
        {
            try
            {
                if (string.IsNullOrEmpty(latestVersion)) return null;
                Helper.PatchSource src = Helper.GetCurrentPatchSource();
                Directory.CreateDirectory(ZipCacheDir);
                string ver = Helper.NormalizeVersion(latestVersion);
                foreach (char ch in Path.GetInvalidFileNameChars()) ver = ver.Replace(ch, '_');
                return Path.Combine(ZipCacheDir, $"{src.Owner}_{ver}.zip");
            }
            catch { return null; }
        }

        private void PurgeStaleCachedZips(string keepPath)
        {
            try
            {
                if (!Directory.Exists(ZipCacheDir)) return;
                Helper.PatchSource src = Helper.GetCurrentPatchSource();
                foreach (string f in Directory.GetFiles(ZipCacheDir, src.Owner + "_*.zip"))
                    if (!string.Equals(f, keepPath, StringComparison.OrdinalIgnoreCase)) File.Delete(f);
            }
            catch { }
        }

        public async Task DownloadPatchFiles(string assetLink, string fileToSave = null)
        {
            string gitHubToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
            (bool tokenvalid, _) = Helper.ValidateGitHubToken(gitHubToken);
            try
            {
                // Reuse the cached zip for this source+version if present (no re-download); otherwise
                // purge stale versions and download into the cache. fileToSave != null bypasses caching.
                string cachePath = (fileToSave == null) ? GetCachedZipPath() : null;
                if (cachePath != null && File.Exists(cachePath))
                {
                    tempFile = cachePath;
                    downloadSuccess = true;
                    Log?.Invoke($"Using the cached download: {Path.GetFileName(cachePath)} ({new FileInfo(cachePath).Length / (1024 * 1024)} MB) — skipping re-download.", "info", true);
                    return;
                }
                if (cachePath != null) PurgeStaleCachedZips(cachePath);
                string target = cachePath ?? (fileToSave ?? tempFile);

                Log?.Invoke("Downloading compressed files...", "info", true);
                ProgressPictureChange?.Invoke(Resources.pecorun);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("PriconneReALLTLInstaller");
                    if (tokenvalid) client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", gitHubToken);

                    using (var response = await client.GetAsync(assetLink, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();

                        long? totalBytesResponse = response.Content.Headers.ContentLength;
                        long totalBytes = totalBytesResponse ?? -1;

                        string fileName = Path.GetFileName(new Uri(assetLink).AbsolutePath);
                        string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(target, FileMode.Create, FileAccess.Write))
                        {
                            var buffer = new byte[4096];
                            long downloadedBytes = 0;
                            int bytesRead;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);

                                downloadedBytes += bytesRead;
                                DownloadProgress?.Invoke(downloadedBytes, totalBytes);
                            }
                        }
                    }
                }

                Log?.Invoke("Download completed.", "info", true);
                downloadSuccess = true;
                if (cachePath != null) tempFile = cachePath;   // extract from (and keep) the cached zip
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error downloading files: " + ex.Message);
                ProgressPictureChange?.Invoke(null);
                downloadSuccess = false;
            }
        }
        public async Task ExtractPatchFiles()

        {
            if (!removeSuccess || !downloadSuccess) return;

            try
            {
                int counter = 0;
                var extractedFiles = new List<string>();   // install manifest (files this source owns)
                using (var zip = ZipFile.OpenRead(tempFile))
                {
                    Log?.Invoke("Extracting files to game folder...", "add", true);
                    ProgressPictureChange?.Invoke(Resources.kokorun);

                    // Keep config files if Reinstall is selected or config files already present
                    string[] ignoreFiles = helper.SetIgnoreFiles(priconnePath, addconfig: helper.IsConfigPresent(priconnePath));

                    foreach (var entry in zip.Entries)
                    {
                        counter++;
                        string fileName = entry.FullName;

                        DownloadProgress?.Invoke(counter, zip.Entries.Count);   // progress bar; per-entry logging removed (5844 file-IO log writes caused jitter)

                        // Zip-slip guard: the resolved destination must stay inside the game
                        // folder. A crafted entry (e.g. "..\..\evil") must not escape priconnePath.
                        string gameRoot = Path.GetFullPath(priconnePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                        string fullDest = Path.GetFullPath(Path.Combine(priconnePath, fileName));
                        if (!fullDest.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            Log?.Invoke($"Skipped suspicious zip entry (path traversal): {fileName}", "error", false);
                            continue;
                        }

                        if (!ignoreFiles.Contains(fileName))
                        {
                            string destinationPath = Path.Combine(priconnePath, Path.GetDirectoryName(fileName));
                            if (!Directory.Exists(destinationPath))
                                Directory.CreateDirectory(destinationPath);

                            await Task.Run(() => ExtractZipEntry(entry, Path.Combine(priconnePath, fileName)));
                            if (entry.Name != "") extractedFiles.Add(fileName.Replace('\\', '/'));
                        }
                    }
                }
                extractSuccess = true;
                Log?.Invoke($"Extracted {extractedFiles.Count} file(s).", "add", false);

                // Pull any external plugin DLLs this source needs from their own repos (e.g. TH's
                // PriconneALLTLFixup.dll from HetCreep/PriconneALLTLFixup), then toggle the
                // .dll <-> .dll.bak profile so only the active TL source's fixup plugins load.
                await DownloadSourcePlugins();
                helper.ApplyPluginProfile(priconnePath);

                // Record what this source installed (path -> owning sources) for smart uninstall.
                helper.WriteInstallManifest(priconnePath, extractedFiles);

                // Keep AutoTranslatorConfig.ini's Language=/DuplicateTextureNames= in sync with this
                // source (the config is kept across installs, so a switch would leave stale values).
                helper.ApplyConfigOverrides(priconnePath);
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error extracting all files: " + ex.Message);
                ProgressPictureChange?.Invoke(null);
                extractSuccess = false;
            }
        }
        public void ExtractZipEntry(ZipArchiveEntry entry, string destinationPath)
        {
            try
            {
                if (entry.Name != "")
                {
                    var destinationDirectory = Path.GetDirectoryName(destinationPath);
                    Directory.CreateDirectory(destinationDirectory);

                    entry.ExtractToFile(destinationPath, true);
                }
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error extracting file: " + ex.Message);
            }
        }

        // Pull external plugin DLLs the selected source declares (PatchSource.PluginDownloads)
        // from their own GitHub releases into BepInEx/plugins. For plugins NOT bundled in the
        // patch zip (e.g. TH's PriconneALLTLFixup.dll). A repo with no release yet is skipped
        // softly, so the wiring can land before the first release is published.
        private async Task DownloadSourcePlugins()
        {
            if (!Helper.PluginProfileEnabled) return;   // per-source plugin profile temporarily off
            Helper.PatchSource src = Helper.GetCurrentPatchSource();
            if (src.PluginDownloads.Count == 0) return;

            string pluginsDir = Path.Combine(priconnePath, "BepInEx", "plugins");
            Directory.CreateDirectory(pluginsDir);

            string gitHubToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
            (bool tokenvalid, _) = Helper.ValidateGitHubToken(gitHubToken);

            foreach (Helper.PluginDownload pd in src.PluginDownloads)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "PriconneReALLTLInstaller");
                        if (tokenvalid) client.Headers.Add("Authorization", $"Bearer {gitHubToken}");

                        string releaseJson = client.DownloadString(pd.ApiBase + "/releases/latest");
                        JObject release = JObject.Parse(releaseJson);
                        JArray assets = release["assets"] as JArray;

                        string assetUrl = null;
                        if (assets != null)
                        {
                            // Prefer the asset matching the expected DLL name, else the first .dll.
                            foreach (JToken asset in assets)
                            {
                                if (string.Equals((string)asset["name"], pd.DllName, StringComparison.OrdinalIgnoreCase))
                                { assetUrl = (string)asset["browser_download_url"]; break; }
                            }
                            if (assetUrl == null)
                            {
                                foreach (JToken asset in assets)
                                {
                                    string name = (string)asset["name"];
                                    if (name != null && name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                    { assetUrl = (string)asset["browser_download_url"]; break; }
                                }
                            }
                        }

                        if (assetUrl == null)
                        {
                            Log?.Invoke($"Latest {pd.Owner}/{pd.Repo} release has no .dll asset — skipping {pd.DllName}.", "info", false);
                            continue;
                        }

                        string destPath = Path.Combine(pluginsDir, pd.DllName);
                        Log?.Invoke($"Downloading plugin {pd.DllName} from {pd.Owner}/{pd.Repo}...", "add", true);
                        await Task.Run(() => client.DownloadFile(assetUrl, destPath));
                        Log?.Invoke($"Installed plugin {pd.DllName}.", "add", false);
                    }
                }
                catch (WebException webEx)
                {
                    HttpWebResponse resp = webEx.Response as HttpWebResponse;
                    if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                        Log?.Invoke($"{pd.Owner}/{pd.Repo} has no release yet — {pd.DllName} will be pulled once it's published.", "info", false);
                    else
                        Log?.Invoke($"Could not download {pd.DllName} ({(resp != null ? "HTTP " + (int)resp.StatusCode : webEx.Message)}).", "info", false);
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Could not download {pd.DllName}: {ex.Message}", "info", false);
                }
            }
        }
        public async Task<string[]> ProcessTree(string priconnePath, string releaseTag)
        {
            string gitHubToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
            (bool tokenvalid, _) = Helper.ValidateGitHubToken(gitHubToken);
            string[] ignoreFiles = helper.SetIgnoreFiles(priconnePath, addconfig: true);
            List<string> filePathsList = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PriconneReALLTLInstaller");
                if (tokenvalid) client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", gitHubToken);

                string treeUrl = $"{Helper.GetCurrentPatchSource().ApiBase}/git/trees/{releaseTag}?recursive=1";

                HttpResponseMessage response = await client.GetAsync(treeUrl);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic treeJson = JObject.Parse(responseBody);

                    foreach (var item in treeJson.tree)
                    {
                        string fileType = item.type;
                        string filePath = item.path;

                        if (fileType == "blob" && filePath.StartsWith("src/"))
                        {
                            string trimmedPath = filePath.Substring("src/".Length);
                            if (!ignoreFiles.Contains(trimmedPath))
                            {
                                filePathsList.Add(trimmedPath);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to fetch tree for tag '{releaseTag}'. Status code: {response.StatusCode}");
                    ErrorLog?.Invoke($"Failed to fetch tree for tag '{releaseTag}'. Status code: {response.StatusCode}");
                    return null;
                }

                return filePathsList.ToArray();
            }
        }
        public async Task RemovePatchFiles(bool uninstall, bool removeConfig, StringCollection configList, bool removeIgnored)

        {
            if (downloadSuccess == false) 
            {
                extractSuccess = false;
                return;
            }


            await Task.Run(() =>
            {
                try
                {
                    removeProgress = true;

                    // Ref-counted uninstall: when uninstalling AND a manifest covers this source, remove
                    // only the files it solely owns — shared modloader/engine files stay for any other
                    // installed source (uninstalling TH from EN+TH leaves EN working). Falls back to the
                    // release-tree removal when there's no manifest (e.g. installed before manifests).
                    string[] currentFiles = null;
                    bool refCounted = false;
                    if (uninstall)
                    {
                        var plan = helper.ResolveManifestUninstall(priconnePath);
                        if (plan != null) { currentFiles = plan.ToArray(); refCounted = true; }
                    }
                    if (!refCounted)
                    {
                        currentFiles = ProcessTree(priconnePath, localVersion).GetAwaiter().GetResult();
                        if (currentFiles == null)
                        {
                            removeSuccess = false;
                            throw new Exception("Failed to get list of files to remove! Cannot continue.");
                        }
                    }

                    if (refCounted)
                        Log?.Invoke($"Uninstalling {Helper.GetCurrentPatchSource().ShortName} (ref-counted): removing {currentFiles.Length} file(s) it solely owns; files shared with another installed source are kept.", "remove", true);
                    else
                        Log?.Invoke(uninstall ? "Removing patch files..." : "Removing old patch files...", "remove", true);
                    ProgressPictureChange?.Invoke(Resources.kyarun);

                    int counter = 0;

                    foreach (var file in currentFiles)
                    {
                        counter++;
                        string filePath = Path.Combine(priconnePath, file);
                        string directory = Path.GetDirectoryName(filePath);

                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            Log?.Invoke($"Removed file: {file}", "remove", false);

                            DeleteEmptyDirectories(directory);

                        }
                        double percentage = ((double)counter / currentFiles.Length) * 100;
                        DownloadProgress?.Invoke(counter, currentFiles.Length);

                    }

                    if (removeConfig) RemoveConfigOrIgnoredFiles("config", configList);

                    if (removeIgnored) RemoveConfigOrIgnoredFiles("ignored", Settings.Default.ignoreFiles);

                    // if (removeInterops) RemoveInterops();

                    removeSuccess = true;
                    removeProgress = false;

                }
                catch (Exception ex)
                {
                    removeSuccess = false;
                    removeProgress = false;
                    ErrorLog?.Invoke("Error removing files: " + ex.Message);
                    ProgressPictureChange?.Invoke(null);
                }
            });
        }
        private void DeleteEmptyDirectories(string directoryPath)
        {
            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath);
                Log?.Invoke($"Removed directory: {directoryPath}", "remove", false);

                string parentDirectory = Path.GetDirectoryName(directoryPath);
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    DeleteEmptyDirectories(parentDirectory);
                }
            }
        }
        private void RemoveConfigOrIgnoredFiles(string type, StringCollection collection)
        {
            try
            {
                Log?.Invoke($"Removing {type} files...", "remove", false);
                foreach (var file in collection)
                {
                    string fullPath = Path.Combine(priconnePath, file);
                    string directory = Path.GetDirectoryName(fullPath);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        Log?.Invoke($"Removed {type} file: {file}", "remove", false);
                        DeleteEmptyDirectories(directory);
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke($"Error removing {type} files: " + ex.Message);
                removeSuccess = false;
            }

        }
        private void RemoveInterops() // obsolete, just kept code in case it'll ever be needed again
        {
            try
            {
                string interopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BepInEx");
                if (Directory.Exists(interopPath))
                {
                    Directory.Delete(interopPath, true);
                    Log?.Invoke($"Removed interop assemblies from {interopPath}", "remove", false);
                }
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error removing interop assemblies: " + ex.Message);
                removeSuccess = false;
            }

        }

        public async void ProcessOperation(string assetLink, bool install, bool uninstall, bool reinstall, bool launch, bool removeConfig, CheckedListBox configListBox, bool removeIgnored)
        {
            // Reset per-operation status flags. They persist across operations otherwise, so a
            // failure in a previous run would make this one silently skip extraction (ExtractPatchFiles
            // early-returns on !removeSuccess/!downloadSuccess) and/or report a false success.
            removeSuccess = true; downloadSuccess = true; extractSuccess = true; cancelledByUser = false;
            string processName = null;
            int versioncompare = Helper.NormalizeVersion(localVersion).CompareTo(Helper.NormalizeVersion(latestVersion));

            StringCollection configFilesSelected = new StringCollection();
            StringCollection configFilesUnSelected = new StringCollection();

            foreach (var item in configListBox.Items)
            {
                int index = configListBox.Items.IndexOf(item);
                if (configListBox.GetItemChecked(index)) configFilesSelected.Add(item.ToString());
                else configFilesUnSelected.Add(item.ToString());
            }

            try
            {

                if (uninstall || reinstall)
                {
                    DialogResult result = helper.UninstallReinstallNotification(uninstall, reinstall, removeConfig, removeIgnored, configFilesSelected, configFilesUnSelected);

                    if (result == DialogResult.No) 
                    {
                        cancelledByUser = true;
                        Log?.Invoke($"Operation cancelled!", "remove", true);
                        return;
                    };
                }

                ProcessStart?.Invoke();

                if (uninstall)
                {
                    processName = "Uninstall";
                    Log?.Invoke("Uninstalling translation patch...", "info", true);
                    await RemovePatchFiles(uninstall: uninstall, removeConfig: removeConfig, configList: configFilesSelected, removeIgnored: removeIgnored);
                    return;
                }

                if (reinstall)
                {
                    processName = "Reinstall";
                    Log?.Invoke("Reinstalling translation patch...", "info", true);
                    await DownloadPatchFiles(assetLink);
                    await RemovePatchFiles(uninstall: uninstall, removeConfig: removeConfig, configList: configFilesSelected, removeIgnored: removeIgnored);
                    await ExtractPatchFiles();
                    return;
                }

                if (install)
                {
                    // Update (remove old files via the patch repo's file tree, then extract) ONLY
                    // when a valid same-source version is installed and it differs from latest.
                    // Fresh/invalid/cross-source (localVersionValid == false) -> plain Install (no
                    // remove) — avoids ProcessTree fetching a tag that doesn't exist in the source
                    // (e.g. installed="None" vs a TH semver tag would 404 the recursive tree call).
                    if (localVersionValid && versioncompare != 0)
                    {
                        processName = "Update";
                        Log?.Invoke("Updating translation patch...", "info", true);
                        await DownloadPatchFiles(assetLink);
                        await RemovePatchFiles(uninstall: uninstall, removeConfig: removeConfig, configList: configFilesSelected, removeIgnored: removeIgnored);
                        await ExtractPatchFiles();
                        return;
                    }

                    processName = "Install";
                    Log?.Invoke("Downloading and installing translation patch...", "info", true);
                    await DownloadPatchFiles(assetLink);
                    await ExtractPatchFiles();
                    return;
                }

            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error completing process: " + ex.Message);
            }

            finally
            {
                bool processSuccess = removeSuccess && downloadSuccess && extractSuccess;

                if (processName != null) 
                {
                    if (!processSuccess) ErrorLog?.Invoke($"{processName} failed"); 
                    else Log?.Invoke($"{processName} complete!", "success", true);
                }
                ProcessFinish?.Invoke();

                if (launch && !cancelledByUser)
                {
                    // Arch B: in-GUI "Launch Game" launches vanilla DMM directly (the universal
                    // launcher). Per-launcher/account launching is via wrapped shortcuts.
                    bool result = StartDMMGamePlayer();

                    if (result)
                    {
                        await Task.Delay(5000);
                        Application.Exit();
                    }

                }
                cancelledByUser = false;
            }
        }
        public async void ProcessAutoUpdateOperation(bool install, string assetLink)
        {
            try
            {
                ProcessStart?.Invoke();

                await DownloadPatchFiles(assetLink);
                if (!install) await RemovePatchFiles(uninstall: false, removeConfig: false, configList: Settings.Default.configFiles, removeIgnored: false);
                await ExtractPatchFiles();
                return;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error completing process: " + ex.Message);
            }

            finally
            {
                bool processSuccess = removeSuccess && downloadSuccess && extractSuccess;

                if (!processSuccess)
                {
                    ErrorLog?.Invoke(install ? "Install failed!" : "Update failed!");
                    ProcessError?.Invoke();
                }
                else
                {
                    Log?.Invoke(install ? "Install complete!" : "Update complete!", "success", true);
                    ProcessFinish?.Invoke();
                }
            }
        }
        public async void ProcessInstallerUpdateOperation(string installerAssetLink, SaveFileDialog saveFileDialog, Form form)
        {
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string selectedFile = saveFileDialog.FileName;
                Log?.Invoke("Downloading latest PriconneReALLTLInstaller version..", "info", true);
                await DownloadPatchFiles(installerAssetLink, selectedFile);

                if (downloadSuccess)
                {
                    Log?.Invoke($"New PriconneReALLTLInstaller version successfully downloaded to: {selectedFile}", "info", false);
                    DialogResult result2 = MessageBox.Show($"New installer version successfully downloaded to:\n{selectedFile}\n\nWould you like to close the application?", "Download successful!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (result2 == DialogResult.Yes) Application.Exit();
                    else form.Close();
                }

                else
                {
                    MessageBox.Show("Error downloading the new installer version!\n\nCheck log for details!", "Download failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public bool StartDMMFastLauncher()
        {
            try
            {
                string dmmFastLauncherPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DMMGamePlayerFastLauncher");
                string dmmFastLauncherExe = Path.Combine(dmmFastLauncherPath, "DMMGamePlayerFastLauncher.exe");

                if (File.Exists(dmmFastLauncherExe))
                {
                    if (!helper.IsFastLauncherShortcutValid()) {
                        Log?.Invoke("Cannot start game! DMMGamePlayerFastLauncher shortcut invalid!", "error", true);
                        return false; 
                    }

                    // Use first valid shortcut from the list
                    string fastLauncherLink = helper.GetFastLauncherLinks().FirstOrDefault(l => System.IO.File.Exists(l));

                    Log?.Invoke("Starting game via DMMGamePlayerFastLauncher.", "info", true);
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = fastLauncherLink,
                    };
                    Process.Start(startInfo);
                    return true;
                }
                Log?.Invoke("Cannot start game! DMMGamePlayerFastLauncher not found!", "error", true);
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error starting DMMGamePlayerFastLauncher: " + ex.Message);
                return false;
            }
        }
        public bool StartPriconneMultiLauncher()
        {
            try
            {
                string priconneLauncherExe = helper.GetPriconneMultiLauncherExePath();

                if (File.Exists(priconneLauncherExe))
                {
                    // Prefer the first valid custom shortcut from the list (e.g. with special arguments);
                    // fall back to the raw exe if none are set.
                    string firstValidLink = helper.GetFastLauncherLinks().FirstOrDefault(l => System.IO.File.Exists(l));
                    string targetFile = !string.IsNullOrEmpty(firstValidLink) ? firstValidLink : priconneLauncherExe;

                    Log?.Invoke("Starting game via PriconneMultiAccountLauncher.", "info", true);
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = targetFile,
                    };
                    Process.Start(startInfo);
                    return true;
                }
                Log?.Invoke("Cannot start game! PriconneMultiAccountLauncher not found!", "error", true);
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error starting PriconneMultiAccountLauncher: " + ex.Message);
                return false;
            }
        }
        public bool StartDMMGamePlayer()
        {
            try
            {
                Log?.Invoke("Starting game via DMMGamePlayer.", "info", true);
                Process.Start("dmmgameplayer://play/GCL/priconner/cl/win");
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error starting DMMGamePlayer: " + ex.Message);
                return false;
            }
        }

        public void HandleFormClosing(Form form, FormClosingEventArgs e)
        {
            try
            {
                if (removeProgress)
                {
                    helper.CannotExitNotification(e, "file removal");
                }
                else if (File.Exists(tempFile) && !tempFile.StartsWith(ZipCacheDir, StringComparison.OrdinalIgnoreCase)) File.Delete(tempFile);   // keep cached zips for reuse across sessions

                Settings.Default.Save();
            }
            catch (IOException)
            {
                helper.CannotExitNotification(e, "file download / extraction");
            }
        }

    }
}