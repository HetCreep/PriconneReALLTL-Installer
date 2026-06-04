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
        private string _lastLoggedPatchVer;   // de-dupe "Found ... installed!" logs (read many times/op + Load+Shown)
        private string _lastLoggedModVer;
        private DateTime? latestReleaseDate;   // selected source's release published_at — stamped onto installed files/dirs
        private static bool _clockSkewWarned = false;   // #16: warn at most once per session about a wrong system clock
        private string latestAssetDigest;      // GitHub asset SHA256 ("sha256:…") for verify-before-touch
        private string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());   // no file created (GetTempFileName throws when %TEMP% is full) — audit B3
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

                            // #25: a malformed entry can leave priconnePath null/empty (Newtonsoft
                            // dynamic returns null for a missing property) — never report a null path
                            // as valid, or every downstream Path.Combine throws ArgumentNullException.
                            if (string.IsNullOrWhiteSpace(priconnePath))
                            {
                                ErrorLog?.Invoke("Found the game entry but its install path is missing — reinstall Princess Connect Re:Dive via DMMGamePlayer.");
                                DisableStart?.Invoke();
                                return (priconnePath = "Not found", priconnePathValid = false, gameVersion = "Not found");
                            }
                            Log?.Invoke("Found Princess Connect Re:Dive in " + priconnePath, "info", false);
                            return (priconnePath, priconnePathValid = true, gameVersion);
                        }
                    }
                }
                ErrorLog?.Invoke("Cannot find the game path. Did you install Princess Connect Re:Dive from DMMGamePlayer?");
                DisableStart?.Invoke();
                return (priconnePath = "Not found", priconnePathValid = false, gameVersion = "Not found");
            }
            catch (FileNotFoundException)
            {
                ErrorLog?.Invoke("Cannot find the DMMGamePlayer config file. Do you have DMMGamePlayer installed?");
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
                    ErrorLog?.Invoke("Game path not valid; cannot determine the installed patch version.");
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
                    ErrorLog?.Invoke("Game path not valid; cannot determine the installed modloader version.");
                    return ("N/A", false);
                }

                string modloaderVersionFilePath = Path.Combine(priconnePath, "BepInEx", "interop", "version");

                if (!File.Exists(modloaderVersionFilePath))
                {
                    return ("None", false);
                }
                string rawVersionFile = File.ReadAllText(modloaderVersionFilePath);
                Match match = Regex.Match(rawVersionFile, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\b");   // #79: allow a 3-digit component (e.g. 6.0.100)

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
                    try
                    {
                        var cj = JObject.Parse(cachedPatch);
                        string d = (string)cj["d"];
                        string pStr = (string)cj["p"];
                        if (!string.IsNullOrEmpty(d) && !string.IsNullOrEmpty(pStr))   // trust the cache only when it carries BOTH the digest + published date; else re-fetch to capture them
                        {
                            latestAssetDigest = d;
                            if (DateTime.TryParse(pStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime cpub)) latestReleaseDate = cpub;
                            return (latestVersion = (string)cj["v"], true, assetLink = (string)cj["a"]);
                        }
                    }
                    catch { }
                }

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "PriconneReALLTLInstaller");
                    if (tokenvalid) client.Headers.Add("Authorization", $"Bearer {gitHubToken}");
                    string response = client.DownloadString(releaseUrl);
                    // #16: GitHub's response carries a server-time `Date` header. A system clock off by
                    // more than ~a day breaks the version cache (#15) and can stop the game/translation
                    // from working — warn once per session so the user corrects their Windows clock.
                    if (!_clockSkewWarned)
                    {
                        string httpDate = client.ResponseHeaders?["Date"];
                        if (!string.IsNullOrEmpty(httpDate) && DateTimeOffset.TryParse(httpDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var srv))
                        {
                            double skewH = Math.Abs((DateTimeOffset.UtcNow - srv).TotalHours);
                            if (skewH >= 24)
                            {
                                _clockSkewWarned = true;
                                Log?.Invoke($"⚠ Your system clock looks off by ~{Math.Round(skewH / 24)} day(s) vs GitHub's time. A wrong date/time breaks update checks and can stop the game/translation from working — please correct your Windows date & time.", "info", true);
                            }
                        }
                    }
                    dynamic releaseJson = JsonConvert.DeserializeObject(response);
                    if (releaseJson == null)
                    {
                        Log?.Invoke("Empty response from GitHub — skipping patch version check.", "info", false);
                        return (latestVersion = null, false, null);
                    }
                    string version = releaseJson.tag_name;
                    try { if (DateTime.TryParse((string)releaseJson.published_at, out DateTime pub)) latestReleaseDate = pub; } catch { }
                    // Pick the .zip patch asset (some sources also ship an .exe installer
                    // in the same release); fall back to the first asset if none match.
                    assetLink = null;
                    latestAssetDigest = null;
                    if (releaseJson.assets != null)   // #35: guard before iterating so a release with no assets array reaches the graceful "no asset yet" path below instead of throwing
                    foreach (var asset in releaseJson.assets)
                    {
                        string assetName = (string)asset.name;
                        if (!string.IsNullOrEmpty(assetName) && assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            assetLink = (string)asset.browser_download_url;
                            try { latestAssetDigest = (string)asset.digest; } catch { }   // "sha256:…" (newer GitHub); null on older releases
                            break;
                        }
                    }
                    if (assetLink == null)
                    {
                        if (releaseJson.assets == null || releaseJson.assets.Count == 0)
                        {
                            Log?.Invoke("Latest release has no downloadable asset yet — skipping.", "info", false);
                            return (latestVersion = null, false, null);
                        }
                        assetLink = releaseJson.assets[0].browser_download_url;
                    }
                    Helper.SetCachedVersion(cacheKey, new JObject { ["v"] = version, ["a"] = assetLink, ["d"] = latestAssetDigest ?? "", ["p"] = latestReleaseDate.HasValue ? latestReleaseDate.Value.ToString("o") : "" }.ToString(Newtonsoft.Json.Formatting.None));
                    return (latestVersion = version, true, assetLink);
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
                    try { var cj = JObject.Parse(stale); latestAssetDigest = (string)cj["d"]; if (DateTime.TryParse((string)cj["p"], null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime sp)) latestReleaseDate = sp; Log?.Invoke("Using last known patch version (GitHub unavailable / rate-limited).", "info", false); return (latestVersion = (string)cj["v"], true, assetLink = (string)cj["a"]); }   // #18: restore the digest+date too so the cached-zip verify uses the right expected hash on the stale path
                    catch { }
                }
                HttpWebResponse resp = webEx.Response as HttpWebResponse;
                string detail = resp != null ? $"HTTP {(int)resp.StatusCode}" : webEx.Message;
                Log?.Invoke($"Could not check the latest patch version ({detail}). Set a GitHub token to avoid rate limits.", "info", false);
                return (latestVersion = null, false, null);
            }
            catch (Exception ex)
            {
                Log?.Invoke("Could not check the latest patch version: " + ex.Message, "info", false);
                return (latestVersion = null, false, null);
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
                Log?.Invoke("Could not check the latest modloader version: " + ex.Message, "info", false);
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

                string cachedInst = Helper.GetCachedVersion("installer", ttlHours: Helper.InstallerCheckTtlHours);   // 7-day TTL — mature app, rare releases
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
                    // Self-update swaps in the PORTABLE exe — prefer the raw .exe asset whose name has no
                    // "Setup" (a release also ships a "...-Setup.exe" Inno installer for first-time installs;
                    // self-update must not grab that). Fall back to the first asset if none matches.
                    string chosenAsset = null;
                    foreach (var a in releaseJson.assets)
                    {
                        string n = (string)a.name ?? "";
                        if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && n.IndexOf("setup", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            chosenAsset = (string)a.browser_download_url;
                            break;
                        }
                    }
                    assetLink = chosenAsset ?? (string)releaseJson.assets[0].browser_download_url;
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
                // Also drop stale partial downloads from older versions, but KEEP the current target's
                // .part so an interrupted download of THIS version can still resume.
                foreach (string f in Directory.GetFiles(ZipCacheDir, src.Owner + "_*.part"))
                    if (!string.Equals(f, keepPath + ".part", StringComparison.OrdinalIgnoreCase)) File.Delete(f);
            }
            catch { }
        }

        // Verifies a downloaded/cached zip against GitHub's asset SHA256 digest ("sha256:…") so a
        // truncated/corrupt/tampered file is never extracted over the install. Returns true when the
        // digest is unavailable (older releases) or the check errors — can't verify, so don't block;
        // a real MISMATCH returns false. Structurally-corrupt zips still fail at ZipFile.OpenRead.
        private bool VerifyZipDigest(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(latestAssetDigest)) return true;
                int c = latestAssetDigest.IndexOf(':');
                string expected = (c >= 0 ? latestAssetDigest.Substring(c + 1) : latestAssetDigest).Trim();
                if (expected.Length == 0) return true;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                using (var fs = File.OpenRead(path))
                {
                    string actual = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
                    return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex) { Log?.Invoke("Could not verify the download hash: " + ex.Message, "info", false); return true; }
        }

        // Stamps the zip file's date to its SOURCE date — the release published_at if known (from the
        // API), else the zip's own newest entry time (no API). Called right after download / cache reuse.
        private void StampZipSourceDate(string zipPath)
        {
            try
            {
                DateTime? zd = latestReleaseDate;
                if (!zd.HasValue)
                {
                    DateTime mx = DateTime.MinValue;
                    using (var z = ZipFile.OpenRead(zipPath))
                        foreach (var e in z.Entries)
                            if (e.Name != "" && e.LastWriteTime.LocalDateTime > mx) mx = e.LastWriteTime.LocalDateTime;
                    if (mx > DateTime.MinValue) zd = mx;
                }
                if (zd.HasValue) File.SetLastWriteTime(zipPath, zd.Value);
            }
            catch { }
        }

        public async Task DownloadPatchFiles(string assetLink, string fileToSave = null)
        {
            // #74: the release ASSET download (objects.githubusercontent.com, public repos) needs NO
            // token, and setting Authorization on the HttpClient would forward the token to whatever host
            // a redirect lands on. So we do NOT decrypt/attach the token for the download — integrity is
            // guaranteed by the SHA-256 digest verify, not by auth. (API calls that need the token are elsewhere.)
            try
            {
                // Reuse the cached zip for this source+version if present (no re-download); otherwise
                // purge stale versions and download into the cache. fileToSave != null bypasses caching.
                string cachePath = (fileToSave == null) ? GetCachedZipPath() : null;
                if (cachePath != null && File.Exists(cachePath))
                {
                    if (VerifyZipDigest(cachePath))
                    {
                        tempFile = cachePath;
                        downloadSuccess = true;
                        StampZipSourceDate(cachePath);   // keep the cached zip's date = source date
                        Log?.Invoke($"Using the cached download: {Path.GetFileName(cachePath)} ({new FileInfo(cachePath).Length / (1024 * 1024)} MB) — skipping re-download.", "info", true);
                        return;
                    }
                    Log?.Invoke("Cached download failed the integrity check — re-downloading.", "info", true);
                    try { File.Delete(cachePath); } catch { }
                }
                if (cachePath != null) PurgeStaleCachedZips(cachePath);
                string target = cachePath ?? (fileToSave ?? tempFile);
                // #18: download to a temp ".part" file and promote it to the real target ONLY after it
                // verifies — so an interrupted/partial download can never masquerade as a complete cached
                // zip that a later run would reuse + extract over the install.
                string downloadTmp = target + ".part";

                Log?.Invoke("Downloading compressed files...", "info", true);
                ProgressPictureChange?.Invoke(Resources.pecorun);

                long resumeFrom = 0;
                try { if (File.Exists(downloadTmp)) resumeFrom = new FileInfo(downloadTmp).Length; } catch { }
                if (resumeFrom > 0) Log?.Invoke($"Resuming a previous download from {resumeFrom / (1024 * 1024)} MB...", "info", true);

                // Resumable download: KEEP the .part across attempts and resume via an HTTP Range request
                // (GitHub's asset host supports it), so a slow or flaky connection no longer restarts the
                // ~330MB download from zero on every drop. Integrity is still gated by the SHA-256 verify
                // below — a mis-resumed/corrupt .part fails the hash and is discarded, so resume never
                // weakens "verify before touch". A per-read stall guard + linear-backoff retry recover from
                // brief drops automatically; only a verify mismatch deletes the .part (forces a clean re-download).
                const int maxAttempts = 4;
                const int stallTimeoutMs = 60000;          // no bytes for 60s -> cancel this attempt, then retry/resume
                bool downloaded = false;
                Exception lastError = null;
                for (int attempt = 1; attempt <= maxAttempts && !downloaded; attempt++)
                {
                    long existing = 0;
                    try { if (File.Exists(downloadTmp)) existing = new FileInfo(downloadTmp).Length; } catch { existing = 0; }
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        using (var cts = new CancellationTokenSource())
                        {
                            client.Timeout = Timeout.InfiniteTimeSpan;   // the stall guard (cts) bounds time, not the 100s default
                            client.DefaultRequestHeaders.UserAgent.ParseAdd("PriconneReALLTLInstaller");
                            // #74: no Authorization header on the asset download (see the note at the top of this method).
                            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, assetLink);
                            if (existing > 0) req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existing, null);

                            cts.CancelAfter(stallTimeoutMs);
                            using (var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false))
                            {
                                FileMode mode;
                                if (existing > 0 && response.StatusCode == HttpStatusCode.PartialContent)
                                {
                                    mode = FileMode.Append;                       // 206 — server honored the Range; continue the file
                                }
                                else if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                                {
                                    try { File.Delete(downloadTmp); } catch { }   // 416 — our .part is stale/too long; discard and restart
                                    throw new IOException("Stale partial download discarded; restarting from zero.");
                                }
                                else
                                {
                                    response.EnsureSuccessStatusCode();           // 200 (Range ignored) / other 2xx — start over
                                    existing = 0;
                                    mode = FileMode.Create;
                                }

                                long? len = response.Content.Headers.ContentLength;   // on a 206 this is the REMAINING byte count
                                long totalBytes = len.HasValue ? len.Value + existing : -1;

                                using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                using (var fileStream = new FileStream(downloadTmp, mode, FileAccess.Write))
                                {
                                    byte[] buffer = new byte[81920];
                                    long downloadedBytes = existing;
                                    int bytesRead;
                                    while (true)
                                    {
                                        cts.CancelAfter(stallTimeoutMs);          // reset the stall window on every read
                                        bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cts.Token).ConfigureAwait(false);
                                        if (bytesRead <= 0) break;
                                        await fileStream.WriteAsync(buffer, 0, bytesRead, cts.Token).ConfigureAwait(false);
                                        downloadedBytes += bytesRead;
                                        DownloadProgress?.Invoke(downloadedBytes, totalBytes);
                                    }
                                }
                            }
                        }
                        downloaded = true;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;   // a drop / reset / stall — keep the .part so the next attempt resumes from where it stopped
                        if (attempt < maxAttempts)
                        {
                            Log?.Invoke($"Download interrupted — retrying ({attempt}/{maxAttempts - 1}), resuming where it left off...", "info", true);
                            await Task.Delay(1500 * attempt).ConfigureAwait(false);   // linear backoff
                        }
                    }
                }

                if (!downloaded)
                {
                    // Every attempt failed on the network. KEEP the .part (no delete) so running it again
                    // resumes from here — the whole point of resume. The install is left untouched.
                    ErrorLog?.Invoke("Download failed after several attempts (network unreachable or unstable). Your install was left untouched — run it again to resume where it stopped." + (lastError != null ? " (" + lastError.Message + ")" : ""));
                    downloadSuccess = false;
                    ProgressPictureChange?.Invoke(null);
                    return;
                }

                if (!VerifyZipDigest(downloadTmp))
                {
                    ErrorLog?.Invoke("Downloaded file failed the SHA256 integrity check — aborting before touching the install. Please try again.");
                    try { File.Delete(downloadTmp); } catch { }
                    downloadSuccess = false;
                    ProgressPictureChange?.Invoke(null);
                    return;
                }
                // Verified → promote the temp file to the real target (replace any stale file there).
                try { if (File.Exists(target)) File.Delete(target); } catch { }
                File.Move(downloadTmp, target);
                Log?.Invoke("Download completed and verified.", "info", true);
                downloadSuccess = true;
                if (fileToSave == null) StampZipSourceDate(target);   // #34: zips only — never open a downloaded .exe (self-update) as a zip
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
                string srcConfigText = null;                // the source's shipped AutoTranslatorConfig.ini (per-source key sync)
                bool extractHadError = false;               // any per-file extract failure -> not a clean success (audit B10)
                using (var zip = ZipFile.OpenRead(tempFile))
                {
                    Log?.Invoke("Extracting files to the game folder...", "add", true);
                    ProgressPictureChange?.Invoke(Resources.kokorun);

                    // Keep config files if Reinstall is selected or config files already present
                    string[] ignoreFiles = helper.SetIgnoreFiles(priconnePath, addconfig: helper.IsConfigPresent(priconnePath));

                    foreach (var entry in zip.Entries)
                    {
                        counter++;
                        string fileName = entry.FullName;

                        // Capture the source's shipped AutoTranslatorConfig.ini text (for per-source key
                        // sync) even when it's a kept config we won't extract.
                        if (entry.Name != "" && fileName.Replace('\\', '/').EndsWith("BepInEx/config/AutoTranslatorConfig.ini", StringComparison.OrdinalIgnoreCase))
                        {
                            try { using (var sr = new StreamReader(entry.Open())) srcConfigText = sr.ReadToEnd(); } catch { }
                        }

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

                        if (!Helper.IgnoreMatches(fileName, ignoreFiles))
                        {
                            // #37: extract to the guard-validated `fullDest` (computed above) — never
                            // re-derive the destination from the untrusted entry name, so the zip-slip
                            // check and the write act on the SAME resolved value (also resolves the
                            // CodeQL cs/zipslip alert: the sink now uses the sanitized path).
                            string destinationPath = Path.GetDirectoryName(fullDest);
                            if (!string.IsNullOrEmpty(destinationPath) && !Directory.Exists(destinationPath))
                                Directory.CreateDirectory(destinationPath);

                            bool ok = await Task.Run(() => ExtractZipEntry(entry, fullDest));
                            if (!ok) extractHadError = true;
                            else if (entry.Name != "") extractedFiles.Add(fileName.Replace('\\', '/'));
                        }
                    }
                }
                extractSuccess = !extractHadError;
                if (extractHadError) ErrorLog?.Invoke("Some files failed to extract — the install may be incomplete. Please run Reinstall.");
                Log?.Invoke($"Extracted {extractedFiles.Count} file(s).", "add", false);

                // Pull any external plugin DLLs this source needs from their own repos (e.g. TH's
                // PriconneALLTLFixup.dll from HetCreep/PriconneALLTLFixup), then toggle the
                // .dll <-> .dll.bak profile so only the active TL source's fixup plugins load.
                await DownloadSourcePlugins();
                helper.ApplyPluginProfile(priconnePath);

                // Record what this source installed (path -> owning sources) for smart uninstall.
                helper.WriteInstallManifest(priconnePath, extractedFiles);

                // Keep AutoTranslatorConfig.ini's Language=/DuplicateTextureNames= in sync with this
                // source — pulled from the source's own shipped config (read from the zip above).
                helper.ApplyConfigOverrides(priconnePath, srcConfigText);

                // Folders: each gets the newest date among its own files (source-accurate per folder).
                // Files keep their own zip-entry dates (ExtractToFile); the zip itself was stamped at download.
                StampReleaseFolderTimes(priconnePath, extractedFiles);
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error extracting all files: " + ex.Message);
                ProgressPictureChange?.Invoke(null);
                extractSuccess = false;
            }
        }
        // Sets each extracted FOLDER to the newest timestamp among the files it contains — a source-
        // accurate per-folder date (not the extraction moment, and not one uniform date for everything).
        // Files keep their own timestamps (ExtractToFile preserved the zip entry time = the real source
        // date of each file, e.g. a DLL's build date), so this matches the source per file AND per folder.
        private void StampReleaseFolderTimes(string root, List<string> relPaths)
        {
            var dirMax = new System.Collections.Generic.Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            foreach (string rel in relPaths)
            {
                try
                {
                    string full = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(full)) continue;
                    DateTime ft = File.GetLastWriteTime(full);                    // the file's own (zip entry) date
                    string d = Path.GetDirectoryName(full);
                    while (!string.IsNullOrEmpty(d) && d.Length > rootFull.Length) // propagate the max up every ancestor folder
                    {
                        if (!dirMax.TryGetValue(d, out DateTime cur) || ft > cur) dirMax[d] = ft;
                        d = Path.GetDirectoryName(d);
                    }
                }
                catch { }
            }
            // Silent: stamping folder timestamps is an internal cosmetic touch, not a user-facing step.
            foreach (var kv in dirMax) { try { if (Directory.Exists(kv.Key)) Directory.SetLastWriteTime(kv.Key, kv.Value); } catch { } }
        }

        public bool ExtractZipEntry(ZipArchiveEntry entry, string destinationPath)
        {
            try
            {
                if (entry.Name != "")
                {
                    var destinationDirectory = Path.GetDirectoryName(destinationPath);
                    Directory.CreateDirectory(destinationDirectory);

                    entry.ExtractToFile(destinationPath, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error extracting file: " + ex.Message);
                return false;   // surfaced so the op reports failure instead of a silent partial install (audit B10)
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

                string responseBody = null;
                System.Net.HttpStatusCode status;
                using (HttpResponseMessage response = await client.GetAsync(treeUrl))   // dispose the response (audit B12)
                {
                    status = response.StatusCode;
                    if (response.IsSuccessStatusCode) responseBody = await response.Content.ReadAsStringAsync();
                }
                if (responseBody == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to fetch tree for tag '{releaseTag}'. Status code: {status}");
                    ErrorLog?.Invoke($"Failed to fetch tree for tag '{releaseTag}'. Status code: {status}");
                    return null;
                }

                dynamic treeJson = JObject.Parse(responseBody);
                foreach (var item in treeJson.tree)
                {
                    string fileType = item.type;
                    string filePath = item.path;

                    if (fileType == "blob" && filePath.StartsWith("src/"))
                    {
                        string trimmedPath = filePath.Substring("src/".Length);
                        if (!Helper.IgnoreMatches(trimmedPath, ignoreFiles))
                        {
                            filePathsList.Add(trimmedPath);
                        }
                    }
                }

                return filePathsList.ToArray();
            }
        }
        public async Task RemovePatchFiles(bool uninstall, bool removeConfig, StringCollection configList, bool removeIgnored, StringCollection ignoredList)

        {
            if (downloadSuccess == false) 
            {
                extractSuccess = false;
                return;
            }


            // #21: resolve the file list to remove BEFORE entering Task.Run — the ProcessTree fallback
            // is async (a GitHub git-tree call), so await it properly here instead of blocking on
            // GetAwaiter().GetResult() inside the Task.Run (deadlock-brittle on .NET Framework).
            // Ref-counted uninstall: when a manifest covers this source, remove only the files it solely
            // owns — shared modloader/engine files stay for any other installed source. The manifest is
            // the source of truth for BOTH uninstall AND update/reinstall (no network call when present,
            // so it can't fail on a 403/rate-limit; renamed/moved/deleted files are still dropped).
            string[] currentFiles;
            bool refCounted = false;
            var plan = helper.ResolveManifestUninstall(priconnePath);
            if (plan != null) { currentFiles = plan.ToArray(); refCounted = true; }
            else
            {
                // No manifest (installed before manifests / source not tracked) → fall back to the
                // source's git tree at the INSTALLED version's tag.
                currentFiles = await ProcessTree(priconnePath, localVersion);
                if (currentFiles == null)
                {
                    removeSuccess = false;
                    ProgressPictureChange?.Invoke(null);
                    ErrorLog?.Invoke("Failed to get the list of files to remove — cannot continue (GitHub unreachable / rate-limited?).");
                    return;
                }
            }

            await Task.Run(() =>
            {
                try
                {
                    removeProgress = true;

                    if (refCounted)
                        Log?.Invoke($"{(uninstall ? "Uninstalling" : "Refreshing")} {Helper.GetCurrentPatchSource().ShortCode} (manifest, ref-counted): removing {currentFiles.Length} file(s) it solely owns; files shared with another installed source are kept{(uninstall ? "" : " (the extract re-applies this source's new version)")}.", "remove", true);
                    else
                        Log?.Invoke(uninstall ? "Removing patch files..." : "Removing old patch files...", "remove", true);
                    ProgressPictureChange?.Invoke(Resources.kyarun);

                    int counter = 0;
                    int removed = 0;
                    string gameRoot = Path.GetFullPath(priconnePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                    // #44: PRE-FLIGHT lock/writability check. The remove loop below is destructive and
                    // non-atomic — if a tracked file is locked (the game still loaded, antivirus / Windows
                    // Search indexer / cloud-sync scanning it, or an editor open on it) the loop would
                    // delete files UP TO the locked one, then throw → removeSuccess=false → the extract
                    // never re-applies them → a half-removed, broken install. So verify EVERY target is
                    // removable FIRST; if any is locked/read-only, abort before touching anything and the
                    // install stays 100% intact. (Extends "verify before touch" → "verify WRITABLE before
                    // touch"; same class as #38's locked-exe-on-uninstall.) Scope = the tracked file set
                    // (the confirmed BT4 trigger); the opt-in config/ignored removal keeps its own catch.
                    var lockedFiles = new List<string>();
                    foreach (var file in currentFiles)
                    {
                        string probe;
                        try { probe = Path.GetFullPath(Path.Combine(priconnePath, file)); } catch { continue; }
                        if (!probe.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!File.Exists(probe)) continue;
                        if (IsFileLocked(probe))
                        {
                            lockedFiles.Add(file);
                            if (lockedFiles.Count >= 5) break;   // enough to report; don't probe all 6000+
                        }
                    }
                    if (lockedFiles.Count > 0)
                    {
                        removeSuccess = false;
                        removeProgress = false;
                        ProgressPictureChange?.Invoke(null);
                        ErrorLog?.Invoke($"Cannot continue — {lockedFiles.Count}{(lockedFiles.Count >= 5 ? "+" : "")} file(s) are locked or read-only. Close the game, antivirus, cloud-sync (Drive/MEGA/Dropbox) and any editor, then retry. Your install was left untouched. First: {string.Join(", ", lockedFiles.Take(3))}");
                        helper.ClearPendingManifest();   // #61: nothing was deleted → leave the manifest exactly as it was
                        return;
                    }

                    foreach (var file in currentFiles)
                    {
                        counter++;
                        DownloadProgress?.Invoke(counter, currentFiles.Length);
                        string filePath = Path.Combine(priconnePath, file);
                        // Safety: only ever delete files INSIDE the game folder. Guards a tampered/edited
                        // manifest (or a bogus tree path) with "..\" or an absolute path from deleting
                        // anything outside the install.
                        string full;
                        try { full = Path.GetFullPath(filePath); } catch { continue; }
                        if (!full.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase)) continue;
                        if (File.Exists(full))
                        {
                            File.Delete(full);
                            removed++;
                            DeleteEmptyDirectories(Path.GetDirectoryName(full));
                        }
                    }
                    // Summary instead of a per-file log line (6244 file-IO log writes were the slow part —
                    // same fix as the extract path). The manifest records the exact files if detail is needed.
                    Log?.Invoke($"Removed {removed} file(s).", "remove", false);

                    // #61: the ref-counted delete loop succeeded → NOW persist the manifest mutation that
                    // ResolveManifestUninstall staged (no-op on the ProcessTree fallback path).
                    helper.CommitPendingManifest();

                    if (removeConfig) RemoveConfigOrIgnoredFiles("config", configList);

                    if (removeIgnored) RemoveConfigOrIgnoredFiles("ignored", ignoredList);

                    // #60: do NOT force removeSuccess=true here. It is already true when the main remove
                    // loop succeeds (reset per-op; the loop throws to the catch on failure, and the #44
                    // pre-flight returns before reaching this). Forcing it would clobber a removeConfig/
                    // removeIgnored failure (RemoveConfigOrIgnoredFiles sets removeSuccess=false) into a
                    // false "complete" while a locked Remove-Config/Ignored file is still on disk.
                    removeProgress = false;

                }
                catch (Exception ex)
                {
                    removeSuccess = false;
                    removeProgress = false;
                    helper.ClearPendingManifest();   // #61: a partial/failed delete must not commit the manifest mutation
                    ErrorLog?.Invoke("Error removing files: " + ex.Message);
                    ProgressPictureChange?.Invoke(null);
                }
            });
        }
        // #44: probe whether a file is locked or non-writable WITHOUT modifying it — open with
        // ReadWrite + FileShare.None (throws if another process holds the file) so the pre-flight check
        // can abort a destructive removal before it touches anything. UnauthorizedAccessException covers
        // a read-only attribute / restrictive ACL (File.Delete would fail on those too).
        private static bool IsFileLocked(string path)
        {
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                return false;
            }
            catch (IOException) { return true; }
            catch (UnauthorizedAccessException) { return true; }
        }
        // #11: OS-generated metadata Windows re-creates (folder view/icon, thumbnail caches). A dir
        // holding ONLY these is a cosmetic leftover, not user/source data, so it's safe to clear.
        private static readonly System.Collections.Generic.HashSet<string> OsMetadataFiles =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "desktop.ini", "thumbs.db", "ehthumbs.db", "ehthumbs_vista.db", ".DS_Store" };

        // True if the dir has no subdirectories and every file is OS-metadata junk, so it is
        // "effectively empty" even though EnumerateFileSystemEntries sees the hidden desktop.ini.
        private static bool IsEffectivelyEmpty(string dir)
        {
            if (Directory.EnumerateDirectories(dir).Any()) return false;
            foreach (string f in Directory.EnumerateFiles(dir))
                if (!OsMetadataFiles.Contains(Path.GetFileName(f))) return false;
            return true;
        }

        private void DeleteEmptyDirectories(string directoryPath)
        {
            // Walk up removing empty parents — iterative (not recursive) so a very deep tree can't
            // StackOverflow (audit B9). #11: also prune dirs left holding ONLY OS-metadata (a bare
            // desktop.ini kept "empty" folders like Texture\Banner\ around forever).
            string gameRoot = Path.GetFullPath(priconnePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string dir = directoryPath;
            while (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && IsEffectivelyEmpty(dir))
            {
                // Defensive bound: we now delete files (the metadata), so never climb to/above the game
                // root — stop at the first dir outside it (the game's own non-empty dirs also stop us).
                string full;
                try { full = Path.GetFullPath(dir); } catch { break; }
                if (!full.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase)) break;

                foreach (string junk in Directory.EnumerateFiles(dir).ToList())
                {
                    try { File.SetAttributes(junk, FileAttributes.Normal); File.Delete(junk); } catch { }
                }
                try { Directory.Delete(dir); } catch { break; }   // a metadata file was locked → stop, don't loop
                dir = Path.GetDirectoryName(dir);
            }
        }

        // #40-L1 Safe Reclaim: free disk by removing BepInEx's regenerable byproducts (LogOutput.log +
        // the assembly cache, both re-created on the next game launch) and pruning empty / OS-metadata-only
        // dirs under Translation. ZERO user-data risk — nothing user-edited or source-shipped is touched.
        // Path-guarded to the game folder. Returns bytes freed. Opt-in (Settings menu).
        public long ReclaimGameFolder()
        {
            long freed = 0;
            if (string.IsNullOrEmpty(priconnePath) || !Directory.Exists(priconnePath)) return 0;
            string gameRoot = Path.GetFullPath(priconnePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string bep = Path.Combine(priconnePath, "BepInEx");
            if (!Directory.Exists(bep)) return 0;

            // 1. Regenerable byproducts — BepInEx re-creates these on the next game launch.
            string log = Path.Combine(bep, "LogOutput.log");
            try { if (File.Exists(log) && Path.GetFullPath(log).StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase)) { freed += new FileInfo(log).Length; File.Delete(log); } } catch { }
            // SafeDeleteDirectory (NOT Directory.Delete(cache, true)): on .NET Framework the recursive
            // delete descends INTO a junction/symlink and deletes its TARGET — which could escape the game
            // folder. SafeDeleteDirectory removes a reparse point as a link (never traverses it) and
            // re-asserts the game-folder bound on every entry.
            string cache = Path.Combine(bep, "cache");
            try { if (Directory.Exists(cache)) freed += SafeDeleteDirectory(cache, gameRoot); } catch { }

            // 2. Prune empty / OS-metadata-only dirs under Translation, deepest first (reuses the #11
            //    IsEffectivelyEmpty rule so a folder left holding only a hidden desktop.ini also goes).
            string tl = Path.Combine(bep, "Translation");
            if (Directory.Exists(tl))
            {
                string[] dirs;
                try { dirs = Directory.GetDirectories(tl, "*", SearchOption.AllDirectories); } catch { dirs = new string[0]; }
                foreach (string d in dirs.OrderByDescending(x => x.Length))
                {
                    try
                    {
                        if (!Directory.Exists(d) || !Path.GetFullPath(d).StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!IsEffectivelyEmpty(d)) continue;
                        foreach (string junk in Directory.EnumerateFiles(d).ToList())
                        {
                            try { freed += new FileInfo(junk).Length; File.SetAttributes(junk, FileAttributes.Normal); File.Delete(junk); } catch { }
                        }
                        Directory.Delete(d);
                    }
                    catch { }
                }
            }
            return freed;
        }

        // Recursively delete a directory WITHOUT following reparse points (junctions/symlinks). On .NET
        // Framework, Directory.Delete(path, true) descends INTO a junction and deletes its TARGET's
        // contents — which could escape the game folder. Here a reparse point is removed as a link only
        // (never traversed), and the game-folder bound is re-asserted on every entry. Returns bytes freed.
        private static long SafeDeleteDirectory(string dir, string gameRoot)
        {
            long freed = 0;
            string full;
            try { full = Path.GetFullPath(dir); } catch { return 0; }
            if (!full.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase)) return 0;
            if (!Directory.Exists(dir)) return 0;
            // A reparse point (junction/symlink): remove the LINK only — do NOT recurse into its target.
            try { if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) { Directory.Delete(dir, false); return 0; } } catch { return 0; }
            try { foreach (string sub in Directory.EnumerateDirectories(dir)) freed += SafeDeleteDirectory(sub, gameRoot); } catch { }
            try
            {
                foreach (string f in Directory.EnumerateFiles(dir).ToList())
                {
                    try { freed += new FileInfo(f).Length; File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); } catch { }
                }
            }
            catch { }
            try { Directory.Delete(dir, false); } catch { }
            return freed;
        }
        // Expands an ignore entry to actual relative paths present under the game folder. A '*' matches
        // exactly one folder segment (BepInEx/Translation/*/Text/_Postprocessors.txt -> the file under
        // each language folder). A non-glob entry returns itself.
        private System.Collections.Generic.List<string> ExpandIgnoreGlob(string pattern)
        {
            var result = new System.Collections.Generic.List<string>();
            if (string.IsNullOrEmpty(pattern)) return result;
            string norm = pattern.Replace('\\', '/');
            int star = norm.IndexOf('*');
            if (star < 0) { result.Add(norm); return result; }                       // exact (config files etc.)

            string prefix = norm.Substring(0, star).TrimEnd('/');                    // BepInEx/Translation
            int slashAfter = norm.IndexOf('/', star);
            string suffix = slashAfter >= 0 ? norm.Substring(slashAfter + 1) : "";   // Text/_Postprocessors.txt
            string prefixDir = Path.Combine(priconnePath, prefix.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(prefixDir)) return result;
            string root = Path.GetFullPath(priconnePath).TrimEnd(Path.DirectorySeparatorChar);
            foreach (string sub in Directory.GetDirectories(prefixDir))              // each language folder
            {
                string candidate = Path.Combine(sub, suffix.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate)) result.Add(candidate.Substring(root.Length + 1).Replace('\\', '/'));
            }
            return result;
        }

        private void RemoveConfigOrIgnoredFiles(string type, StringCollection collection)
        {
            try
            {
                Log?.Invoke($"Removing {type} files...", "remove", false);
                // Path guard (#22): only ever delete INSIDE the game folder — a hand-edited ignore
                // entry with "..\" or an absolute path must never escape (parity with RemovePatchFiles).
                string gameRoot = Path.GetFullPath(priconnePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                // #10: for IGNORED files, scope to the ACTIVE source's language. The ignore globs
                // (Translation/*/...) expand across EVERY language folder, so without this an
                // "uninstall EN + Remove Ignored" would ALSO delete TH's user-edited XUnity data (data loss).
                string activeLangPrefix = null;
                if (type == "ignored")
                {
                    string[] vparts = (Helper.GetCurrentPatchSource().VersionFileRelPath ?? "").Replace('\\', '/').Split('/');
                    if (vparts.Length >= 3 && vparts[0].Equals("BepInEx", StringComparison.OrdinalIgnoreCase) && vparts[1].Equals("Translation", StringComparison.OrdinalIgnoreCase))
                        activeLangPrefix = "BepInEx/Translation/" + vparts[2] + "/";
                }
                foreach (var entry in collection)
                {
                    foreach (string rel in ExpandIgnoreGlob(entry == null ? "" : entry.ToString()))   // glob (e.g. */) -> actual files
                    {
                        string relNorm = rel.Replace('\\', '/');
                        // #10: skip ANOTHER language's ignored data — only remove the active source's language
                        if (activeLangPrefix != null
                            && relNorm.StartsWith("BepInEx/Translation/", StringComparison.OrdinalIgnoreCase)
                            && !relNorm.StartsWith(activeLangPrefix, StringComparison.OrdinalIgnoreCase))
                            continue;
                        string fullPath = Path.Combine(priconnePath, rel.Replace('/', Path.DirectorySeparatorChar));
                        string full;
                        try { full = Path.GetFullPath(fullPath); } catch { continue; }
                        if (!full.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase)) continue;
                        if (File.Exists(full))
                        {
                            File.Delete(full);
                            DeleteEmptyDirectories(Path.GetDirectoryName(full));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorLog?.Invoke($"Error removing {type} files: " + ex.Message);
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
            int versioncompare = Helper.CompareVersions(localVersion, latestVersion);

            StringCollection configFilesSelected = new StringCollection();
            StringCollection configFilesUnSelected = new StringCollection();

            // The list also holds ignored-file rows now (merged UI); the config SELECTION must count
            // only real config files — ignored files are governed by the removeIgnored flag.
            var configSet = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string cf in Settings.Default.configFiles) configSet.Add(cf);
            foreach (var item in configListBox.Items)
            {
                string s = item.ToString();
                if (!configSet.Contains(s)) continue;   // ignored-file row → not part of the config selection
                int index = configListBox.Items.IndexOf(item);
                if (configListBox.GetItemChecked(index)) configFilesSelected.Add(s);
                else configFilesUnSelected.Add(s);
            }

            // Ignored rows are user-selectable too — collect the checked ignored files (removeIgnored
            // then removes exactly these, not the whole ignore list).
            StringCollection ignoredFilesSelected = new StringCollection();
            var ignoredSet = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string igf in Helper.CurrentSourceIgnoreFiles()) ignoredSet.Add(igf);
            foreach (var it in configListBox.Items)
            {
                string s2 = it.ToString();
                if (!ignoredSet.Contains(s2)) continue;
                int idx2 = configListBox.Items.IndexOf(it);
                if (configListBox.GetItemChecked(idx2)) ignoredFilesSelected.Add(s2);
            }

            try
            {

                if (uninstall || reinstall)
                {
                    DialogResult result = helper.UninstallReinstallNotification(uninstall, reinstall, removeConfig, removeIgnored, configFilesSelected, configFilesUnSelected);

                    if (result == DialogResult.No) 
                    {
                        cancelledByUser = true;
                        Log?.Invoke("Operation cancelled!", "remove", true);
                        return;
                    };
                }

                ProcessStart?.Invoke();

                if (uninstall)
                {
                    processName = "Uninstall";
                    Log?.Invoke("Uninstalling translation patch...", "info", true);
                    await RemovePatchFiles(uninstall: uninstall, removeConfig: removeConfig, configList: configFilesSelected, removeIgnored: removeIgnored, ignoredList: ignoredFilesSelected);
                    return;
                }

                if (reinstall)
                {
                    processName = "Reinstall";
                    Log?.Invoke("Reinstalling translation patch...", "info", true);
                    await DownloadPatchFiles(assetLink);
                    await RemovePatchFiles(uninstall: uninstall, removeConfig: removeConfig, configList: configFilesSelected, removeIgnored: removeIgnored, ignoredList: ignoredFilesSelected);
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
                        await RemovePatchFiles(uninstall: uninstall, removeConfig: removeConfig, configList: configFilesSelected, removeIgnored: removeIgnored, ignoredList: ignoredFilesSelected);
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
                    if (!processSuccess) ErrorLog?.Invoke($"{processName} failed.");
                    else Log?.Invoke($"{processName} complete!", "success", true);
                }
                ProcessFinish?.Invoke();

                if (launch && !cancelledByUser && processSuccess)
                {
                    // Launch only on SUCCESS (#19): never start the game (and auto-exit) after a
                    // failed/partial operation — that would run a half-patched install and the 5s
                    // auto-exit would hide the error from the user.
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
                if (!install) await RemovePatchFiles(uninstall: false, removeConfig: false, configList: Settings.Default.configFiles, removeIgnored: false, ignoredList: new StringCollection());
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
                    ErrorLog?.Invoke(install ? "Install failed." : "Update failed.");
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
            // #20: async void — contain any exception so it can't surface as Application.ThreadException
            // and terminate the process (the other two Process* ops already have a top-level try/catch).
            try
            {
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string selectedFile = saveFileDialog.FileName;
                    Log?.Invoke("Downloading the latest PriconneReALLTLInstaller version...", "info", true);
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
            catch (Exception ex)
            {
                ErrorLog?.Invoke("Error during installer self-update: " + ex.Message);
            }
        }

        public bool StartDMMGamePlayer()
        {
            try
            {
                Log?.Invoke("Starting the game via DMMGamePlayer.", "info", true);
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