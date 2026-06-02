using HelperFunctions;
using InstallerFunctions;
using LoggerFunctions;
using PriconneReALLTLInstaller.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace PriconneReALLTLInstaller
{
    public partial class MainForm : BaseForm
    {
        private string patchgithubAPI = Helper.GetCurrentPatchSource().ApiBase;
        private string assetLink;
        private string priconnePath;
        private bool priconnePathValid;
        private string gameVersion;
        private string latestVersion;
        private bool latestVersionValid;
        private string localVersion;
        private bool localVersionValid;
        private string localModLoaderVersion;
        private bool localModLoaderVersionValid;
        private string latestModLoaderVersion;
        private bool modLoaderOutdated;
        private string modLoaderTooltip;
        private string commitSha;
        private int versioncompare;
        private CheckBox[] exclusiveCheckboxes;
        private CheckBox[] operationCheckboxes;
        private CheckBox[] optionCheckboxes;
        private Button[] menuButtons;
        private System.Windows.Forms.LinkLabel patchSourceLinkLabel;

        public MainForm()
        {
            // Get the current assembly version
            Version currentVersion = Assembly.GetEntryAssembly().GetName().Version;

            // Get the last known assembly version from settings
            // Fresh installs have an empty LastKnownVersion; new Version("") would throw before the
            // form even initializes. Fall back to 0.0.0.0 so the upgrade check runs instead of crashing.
            if (!Version.TryParse(Properties.Settings.Default.LastKnownVersion, out Version lastKnownVersion))
                lastKnownVersion = new Version(0, 0, 0, 0);

            // Compare the current version with the last known version
            if (currentVersion > lastKnownVersion)
            {
                // Upgrade is required
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.LastKnownVersion = currentVersion.ToString();
                Properties.Settings.Default.Save();
            }

            Helper.SetDefaultDMMConfigPath();
            Helper.EnsureDMMConfigPathValid();
            Helper.MigrateIgnoreDefaults();   // one-time: en/ ignore defaults -> lang-agnostic */ glob

            InitializeComponent();

            installer.Log += OnLog;
            installer.ErrorLog += OnErrorLog;
            helper.Log += OnLog;
            helper.ErrorLog += OnErrorLog;
            installer.DisableStart += OnDisableStart;
            installer.DownloadProgress += OnDownloadProgress;
            installer.ProcessStart += OnProcessStart;
            installer.ProcessFinish += OnProcessFinish;

            SubscribeToCheckBoxes(this.Controls);

            var buttonImageMappings = new List<(Button button, Image normal, Image hover, EventHandler extraMouseEnterEvent, EventHandler extraMouseLeaveEvent)>
            {
                (startButton, Resources.start_idle, Resources.start_hover_lit, StartButtonExtraLogic, null),
                (exitButton, Resources.door_closed, Resources.door_open, MenuButtonEnterExtraLogic, MenuButtonLeaveExtraLogic),
                (minimizeButton, Resources.arrow_blue, Resources.arrow_yellow, MenuButtonEnterExtraLogic, MenuButtonLeaveExtraLogic),
                (settingsButton, Resources.scroll_closed_res2, Resources.scroll_open, MenuButtonEnterExtraLogic, MenuButtonLeaveExtraLogic),
                (aboutButton, Resources.i_bubble, Resources.q_bubble, MenuButtonEnterExtraLogic, MenuButtonLeaveExtraLogic),
                (auButton, Resources.crystal_normal_res, Resources.crystal_lit, MenuButtonEnterExtraLogic, MenuButtonLeaveExtraLogic)
            };

            RegisterButtonImagesBulk(buttonImageMappings);

            RegisterMouseDrag(new List<Control> { pictureBox1, operationsPanel, optionsPanel, gameInfoPanel, patchInfoPanel });

            this.Layout += OnOperationLabelChange;
            operationLabel.TextChanged += OnOperationLabelChange;


            logger = new Logger("ReALLTLInstaller.log", outputTextBox, toolStripStatusLabel1);
            logger.StartSession();

        }

        // Functions

        private void StartButtonExtraLogic(object sender, EventArgs e)
        {
            if (sender is Button button && !button.Enabled)
            {
                button.BackgroundImage = Resources.start_idle; // Ensure it does not change when disabled
            }
        }

        private void MenuButtonEnterExtraLogic(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                var menuButtonLabels = new List<(Button menuButton, string name)>
        {
            (exitButton, "Exit Application"),
            (minimizeButton, "Minimize Application"),
            (aboutButton, "Help / About"),
            (auButton, "Create AutoUpdater Shortcut"),
            (settingsButton, "Settings")
        };

                foreach (var (menuButton, name) in menuButtonLabels)
                {
                    if (button == menuButton)
                    {
                        menuButtonLabel.Visible = true;
                        menuButtonLabel.Text = name;
                    }
                }
            }
        }

        private void MenuButtonLeaveExtraLogic(object sender, EventArgs e)
        {
            menuButtonLabel.Visible = false;
        }

        void SubscribeToCheckBoxes(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is CheckBox checkbox)
                {
                    // Console.WriteLine(checkbox.Name);
                    checkbox.CheckedChanged += OnCheckedChange;
                    checkbox.EnabledChanged += OnEnabledChange;
                }
                else if (control.HasChildren)
                {
                    // Recursively search for checkboxes in child controls
                    SubscribeToCheckBoxes(control.Controls);
                }
            }
        }

        void DisableCheckboxes(CheckBox[] checkboxes)
        {
            foreach (CheckBox checkBox in checkboxes)
            {
                checkBox.Enabled = false;
            }
        }

        void EnableCheckboxes(CheckBox[] checkboxes)
        {
            foreach (CheckBox checkBox in checkboxes)
            {
                checkBox.Enabled = true;
            }
        }

        // Keep the window within the user's screen working area and centered, even when the
        // layout grows (Show Logs) or on high-DPI displays where the form scales up.
        private void FitAndCenter(int desiredHeight)
        {
            System.Drawing.Rectangle wa = Screen.FromControl(this).WorkingArea;
            this.Height = Math.Min(desiredHeight, wa.Height);
            this.Left = wa.Left + Math.Max(0, (wa.Width - this.Width) / 2);
            this.Top = wa.Top + Math.Max(0, (wa.Height - this.Height) / 2);
        }

        // After Show Logs resizes the window (FitAndCenter may have clamped it to the screen), size
        // the log box to fill the gap between its top and the status bar — so the last line isn't cut
        // off — and Refresh() to repaint the newly revealed area (which can flash black otherwise).
        private void FitLogBox()
        {
            int bottom = this.ClientSize.Height - statusStrip1.Height - 8;
            int h = bottom - outputTextBox.Top;
            if (h > 60) outputTextBox.Height = h;
            outputTextBox.Refresh();
        }

        private async void InitializeUI()
        {
            Icon = Resources.jewel;
            this.StartPosition = FormStartPosition.CenterScreen;
            optionsPanel.Height = 87;
            FitAndCenter(610);   // base height includes the +30 "TL Source" line added by SetupPatchSourceSelector

            SetupPatchSourceSelector();
            SetupIgnoredList();

            versionLinkLabel.Text = $"v{String.Format(Application.ProductVersion)}";

            removeConfigCheckBox.Enabled = false;
            removeIgnoredCheckBox.Enabled = false;

            (priconnePath, priconnePathValid, gameVersion) = installer.GetGamePath();
            gamePathLinkLabel.Text = priconnePath.Length < 55 ? "Game Path: " + priconnePath : "Game Path: " + priconnePath.Substring(0, 52) + "...";
            gameVersionLabel.Text = "Game Version: " + gameVersion;

            helper.LogFastLauncherShortcut();

            launchCheckBox.Enabled = priconnePathValid;
            launchCheckBox.Checked = Settings.Default.launchState;
            launchCheckBox.Text = " Launch Game (DMM)";   // Arch B: GUI launch = vanilla DMM; per-account launch via wrapped shortcuts
            operationsPanel.Height = launchCheckBox.Checked ? 184 : 154;
            showLogCheckBox.Checked = Settings.Default.showLogChecked;

            // Latest-version checks hit GitHub. Show placeholders now; the actual fetch runs off
            // the UI thread in LoadLatestVersionInfoAsync so the window never freezes on a slow or
            // rate-limited request.
            latestVersionLinkLabel.Text = "Checking…";
            latestModloaderVersionLabel.Text = "Checking…";

            exclusiveCheckboxes = new CheckBox[] { installCheckBox, reinstallCheckBox, uninstallCheckBox };
            operationCheckboxes = new CheckBox[] { installCheckBox, reinstallCheckBox, uninstallCheckBox, launchCheckBox };
            optionCheckboxes = new CheckBox[] { removeConfigCheckBox, removeIgnoredCheckBox };
            menuButtons = new Button[] { exitButton, minimizeButton, aboutButton, auButton, settingsButton };

            foreach (CheckBox checkBox in exclusiveCheckboxes)
            {
                checkBox.CheckedChanged += ExclusiveCheckbox_CheckedChanged;
            }

            foreach (CheckBox checkBox in operationCheckboxes)
            {
                checkBox.CheckedChanged += OperationCheckbox_CheckedChanged;
            }

            var clearCacheMenuItem = new ToolStripMenuItem("Clear Download Cache");
            if (settingsMenuStrip.Items.Count > 0)   // match the Designer items' font/colour so it doesn't look greyed/different
            {
                clearCacheMenuItem.Font = settingsMenuStrip.Items[0].Font;
                clearCacheMenuItem.ForeColor = settingsMenuStrip.Items[0].ForeColor;
            }
            clearCacheMenuItem.Click += (s, e) => ClearDownloadCache();
            settingsMenuStrip.Items.Add(clearCacheMenuItem);

            // A dedicated "Check for Updates Now" action. The toggle above only auto-checks on startup
            // and stays silent when already up to date; this one always runs on demand, bypasses the
            // 6h version cache for a live result, and reports the outcome either way.
            var checkNowMenuItem = new ToolStripMenuItem("Check for Updates Now");
            checkNowMenuItem.Font = checkForInstallerUpdatesToolStripMenuItem.Font;            // match the Designer items so it isn't greyed
            checkNowMenuItem.ForeColor = checkForInstallerUpdatesToolStripMenuItem.ForeColor;
            checkNowMenuItem.Click += checkForUpdatesNow_Click;
            int toggleIdx = helpMenuStrip.Items.IndexOf(checkForInstallerUpdatesToolStripMenuItem);
            helpMenuStrip.Items.Insert(toggleIdx + 1, checkNowMenuItem);                       // right under the startup toggle

            installer.LogCacheStatus();   // show what's in the zip cache this session
            await LoadLatestVersionInfoAsync(bypassCache: false);
        }

        // Fetches latest patch + modloader versions OFF the UI thread, then refreshes the version
        // UI. Used on startup and on TL-source switch (bypassCache forces a live re-fetch). The
        // window stays responsive throughout — a slow or rate-limited (HTTP 403) response no longer
        // freezes the app the way the old synchronous calls did.
        private int versionLoadSeq = 0;
        private async Task LoadLatestVersionInfoAsync(bool bypassCache)
        {
            // Rapid source-switching fires several of these at once. Tag each run; apply only the
            // LATEST one's result so stale fetches from earlier switches can't overwrite the labels
            // out of order ("Installed/Latest lagging behind" + half-shown "Checking").
            int seq = ++versionLoadSeq;
            // Only the LATEST values come from GitHub → show "Checking…" for those while fetching.
            latestVersionLinkLabel.Text = "Checking…";
            latestModloaderVersionLabel.Text = "Checking…";
            // TL Installed is per-source → flash "Checking…" during the switch as a visible transition;
            // UpdateUI() resolves it to the freshly-read local version once the latest fetch returns.
            localVersionLabel.Text = "Checking…";
            // Modloader Installed is shared (same across sources) — show it now; it won't change on switch.
            (localModLoaderVersion, localModLoaderVersionValid) = installer.GetInstalledModloaderVersion();
            localModloaderVersionLabel.Text = localModLoaderVersion;
            patchgithubAPI = Helper.GetCurrentPatchSource().ApiBase;
            string api = patchgithubAPI;
            string token = Helper.DecryptString(Settings.Default.GithubAPIKey);
            if (bypassCache) Helper.BypassVersionCache = true;

            var result = await Task.Run(() =>
            {
                Helper.ValidateGitHubToken(token);   // pre-warm token validation off the UI thread
                var p = installer.GetLatestPatchRelease(api);
                var m = installer.GetLatestModloaderRelease();
                return (patch: p, ml: m);
            });

            if (seq != versionLoadSeq) return;        // superseded by a newer switch — discard stale result
            if (bypassCache) Helper.BypassVersionCache = false;

            (latestVersion, latestVersionValid, assetLink) = result.patch;
            latestVersionLinkLabel.Text = latestVersionValid ? Helper.NormalizeVersion(latestVersion) : "ERROR!";

            (latestModLoaderVersion, commitSha) = result.ml;
            latestModloaderVersionLabel.Text = latestModLoaderVersion != null ? latestModLoaderVersion : "N/A";
            if (commitSha != null) toolTip.SetToolTip(latestModloaderVersionLabel, $"Commit SHA: {commitSha}");

            UpdateUI();

            if (versioncompare == 0 && !modLoaderOutdated && latestModLoaderVersion != null)
                logger.Log("You already have the latest translation patch version installed!", "success", true);

            startButton.Enabled = (!latestVersionValid) ? false : helper.isAnyChecked(operationCheckboxes);
        }

        private void UpdateUI()
        {
            string githubAPIToken = Helper.DecryptString(Settings.Default.GithubAPIKey);
            (bool tokenvalid, _) = Helper.ValidateGitHubToken(githubAPIToken);
            if (!string.IsNullOrEmpty(githubAPIToken) && !tokenvalid) logger.Log("Github API token invalid or expired! Please check and reset it!", "error");

            (localVersion, localVersionValid) = installer.GetInstalledPatchVersion();
            (localModLoaderVersion, localModLoaderVersionValid)= installer.GetInstalledModloaderVersion();

            installCheckBox.Text = localVersionValid ? " Update" : " Install";

            localVersionLabel.Text = Helper.NormalizeVersion(localVersion);
            localModloaderVersionLabel.Text = localModLoaderVersion;

            versioncompare = Helper.NormalizeVersion(localVersion).CompareTo(Helper.NormalizeVersion(latestVersion));
            if ((!localVersionValid || versioncompare != 0) && priconnePathValid)
            {
                installCheckBox.Enabled = true;
                installCheckBox.Checked = true;
            }
            else installCheckBox.Enabled = false;


            if (localVersionValid && localModLoaderVersionValid)
            {
                if (latestModLoaderVersion != null) (modLoaderOutdated, modLoaderTooltip) = helper.CompareGameandModloaderVersions(gameVersion, localModLoaderVersion, latestModLoaderVersion);
                if (modLoaderOutdated)
                {
                    logger.Log($"Modloader check failed!", "error", true);
                    logger.Log($"{modLoaderTooltip}", "error", false);
                    // Per-source modloader fallback (warn + switch): the authoritative modloader is
                    // ImaterialC's. If the selected source ships a stale one, point the user at the
                    // lightweight fix — switch to English, Update, switch back — instead of forcing a
                    // ~330MB engine re-download.
                    if (Helper.GetCurrentPatchSource().Owner != Helper.ModloaderSource.Owner)
                        logger.Log("This TL source's bundled modloader is behind ImaterialC's. Switch TL Source to English, run Update to install the current modloader, then switch back.", "info", false);
                }
                modExPicture.Visible = modLoaderOutdated;
                toolTip.SetToolTip(modExPicture, modLoaderTooltip);
            }

            newPatchPictureBox.Visible = Helper.NormalizeVersion(localVersion) == Helper.NormalizeVersion(latestVersion) ? false : true;

            SetUninstallandReinstallCheckBox(localVersionValid);
            UpdateModeDescription();

            LayoutOptionLists();   // populate + size the single (merged) options list per the current Remove options

            // Re-enable the Remove options when an option-supporting operation (Reinstall/Uninstall) is
            // still checked — otherwise switching source while one was checked left them stuck disabled
            // until the user re-toggled the operation.
            bool optionsOp = reinstallCheckBox.Checked || uninstallCheckBox.Checked;
            removeConfigCheckBox.Enabled = optionsOp;
            removeIgnoredCheckBox.Enabled = optionsOp;

            if (!latestVersionValid)
            {
                foreach (CheckBox checkBox in operationCheckboxes)
                {
                    checkBox.Enabled = false;
                }
                removeConfigCheckBox.Enabled = false;
                removeIgnoredCheckBox.Enabled = false;
                startButton.Enabled = false;
                return;
            }
        }

        private void UpdateModeDescription()
        {
            bool exclusiveCheckboxChecked = helper.isAnyChecked(exclusiveCheckboxes);
           
            var modes = new List<(bool condition, string mode, string description)>
                {
                    (installCheckBox.Checked && !localVersionValid, Settings.Default.installMode, Settings.Default.installModeDescription),
                    (installCheckBox.Checked && localVersionValid, Settings.Default.updateMode, Settings.Default.updateModeDescription),
                    (reinstallCheckBox.Checked, Settings.Default.reinstallMode, Settings.Default.reinstallModeDescription),
                    (uninstallCheckBox.Checked, Settings.Default.uninstallMode, Settings.Default.uninstallModeDescription),
                    (!priconnePathValid || !latestVersionValid, Settings.Default.disabledMode, Settings.Default.disabledModeDescription),
                    (launchCheckBox.Checked && !exclusiveCheckboxChecked, Settings.Default.launchMode, Settings.Default.launchModeDescrption)
                };
  
            foreach (var (condition, mode, description) in modes)
            {
                if (condition)
                {
                    if (launchCheckBox.Checked && exclusiveCheckboxChecked)
                    {
                        operationLabel.Text = $"Current Operation: {mode} + {Settings.Default.launchMode}";
                        toolTip.SetToolTip(operationToolTipPicture, description + "\n\n" + Settings.Default.launchModeDescrption);
                        return;
                    }
                    operationLabel.Text = $"Current Operation: {mode}";
                    toolTip.SetToolTip(operationToolTipPicture, description);
                    return;
                }
            }

            operationLabel.Text = $"Current Operation: {Settings.Default.noOperationMode}";
            toolTip.SetToolTip(operationToolTipPicture, Settings.Default.noOperationModeDescription);
            return;
        }

        // Events
        public void OnLog(string message, string color, bool writeToToolStrip = false)
        {
            outputTextBox.Invoke((Action)(() =>
            {
                logger.Log(message, color, writeToToolStrip);
            }));
        }

        private void OnErrorLog(string message)
        {
            outputTextBox.Invoke((Action)(() =>
            {
                logger.Error(message);
            }));

        }

        public void OnDisableStart()
        {
            startButton.Enabled = false;
            startButton.BackgroundImage = Resources.start_disabled;
        }

        public void OnDownloadProgress(double currentValue, double maxValue)
        {
            double percentage = ((double)currentValue / (double)maxValue) * 100;
            statusStrip1.Invoke((Action)(() =>
            {
                toolStripProgressBar1.Value = (int)percentage;
                toolStripStatusLabel3.Text = $"{Math.Truncate(percentage)}%";
            }));
        }

        public void SetUninstallandReinstallCheckBox(bool enabledState)
        {
            reinstallCheckBox.Enabled = enabledState;
            uninstallCheckBox.Enabled = enabledState;
        }

        public void OnCheckedChange(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                checkBox.Image = checkBox.Checked ? Resources.check_checked_24x24_2 : Resources.check_empty_24x24_2;

                if (checkBox == showLogCheckBox)
                {
                    FitAndCenter(checkBox.Checked ? 890 : 610);
                    Settings.Default.showLogChecked = checkBox.Checked;
                    if (checkBox.Checked) FitLogBox();
                }
            }
            UpdateModeDescription();
        }

        public void OnEnabledChange(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                checkBox.Checked = false;
                checkBox.Image = checkBox.Enabled ? Resources.check_empty_24x24_2 : Resources._lock;
            }
                
        }

        private void ExclusiveCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox clickedCheckbox = (CheckBox)sender;

            // If the clicked checkbox is checked, uncheck all other checkboxes.
            if (clickedCheckbox.Checked)
            {
                foreach (CheckBox checkbox in exclusiveCheckboxes)
                {
                    if (checkbox != clickedCheckbox)
                    {
                        checkbox.Checked = false;
                    }
                }
            }
            if (clickedCheckbox == reinstallCheckBox || clickedCheckbox == uninstallCheckBox)
            {
                removeConfigCheckBox.Enabled = clickedCheckbox.Checked;
                removeIgnoredCheckBox.Enabled = clickedCheckbox.Checked;
            }
        }

        private void OperationCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            startButton.Enabled = helper.isAnyChecked(operationCheckboxes);
        }

        private void OnOperationLabelChange(object sender, EventArgs e)
        {
            operationToolTipPicture.Location = new Point(operationLabel.Right + 5, operationToolTipPicture.Top);
        }

        private void SetupIgnoredList()
        {
            configListBox.CheckOnClick = true;          // single click toggles; config AND ignored rows are user-selectable
            configListBox.DisplayMember = "Display";    // show the (middle-ellipsised) Display; ToString() keeps the full path for ops
            removeIgnoredCheckBox.CheckedChanged += removeIgnoredCheckBox_CheckedChanged;
        }

        // An options row: stores the full Path but DISPLAYS a middle-ellipsised form (CheckedListBox
        // can't owner-draw, so we pre-shorten the text). ToString() returns the full Path so the
        // install/remove ops still get the real path.
        private sealed class OptionRow
        {
            public string Path { get; }
            public string Display { get; }
            public OptionRow(string path, string display) { Path = path; Display = display; }
            public override string ToString() => Path;
        }

        // Middle path-ellipsis (BepInEx/…/_Postprocessors.txt) so long paths fit the options box.
        private string MiddleEllipsis(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            int avail = configListBox.Width - 26;   // minus checkbox + padding
            if (avail < 40 || System.Windows.Forms.TextRenderer.MeasureText(path, configListBox.Font).Width <= avail) return path;
            string norm = path.Replace('\\', '/');
            int first = norm.IndexOf('/'), last = norm.LastIndexOf('/');
            if (first < 0 || last <= first) return path;
            return norm.Substring(0, first) + "/…/" + norm.Substring(last + 1);
        }

        // Fills the single options list with config rows (toggleable) and/or ignored rows (display-only),
        // per which Remove option is checked. Preserves the user's config check states across the
        // re-populations triggered by toggling the other option.
        private void PopulateOptionsList()
        {
            var keptUnchecked = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < configListBox.Items.Count; i++)
                if (!configListBox.GetItemChecked(i)) keptUnchecked.Add(configListBox.Items[i].ToString());

            configListBox.Items.Clear();
            if (removeConfigCheckBox.Checked && Settings.Default.configFiles != null)
                foreach (var c in Settings.Default.configFiles) { string p = c.ToString(); configListBox.Items.Add(new OptionRow(p, MiddleEllipsis(p)), !keptUnchecked.Contains(p)); }
            if (removeIgnoredCheckBox.Checked)
                foreach (string i in Helper.CurrentSourceIgnoreFiles()) configListBox.Items.Add(new OptionRow(i, MiddleEllipsis(i)), !keptUnchecked.Contains(i));
        }

        // Populates + shows the SINGLE options list (config + ignored rows merged → one scrollbar).
        private void LayoutOptionLists()
        {
            PopulateOptionsList();
            bool show = removeConfigCheckBox.Checked || removeIgnoredCheckBox.Checked;
            configListBox.Visible = show;
            if (show)
            {
                int ih = configListBox.ItemHeight > 0 ? configListBox.ItemHeight : 16;
                int h = Math.Min(ih * Math.Max(1, configListBox.Items.Count) + 4, 66);   // fit content; one scrollbar if it overflows
                configListBox.SetBounds(15, 85, 290, h);
                optionsPanel.Height = 154;
            }
            else optionsPanel.Height = 87;
        }

        private void removeConfigCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            LayoutOptionLists();
        }

        private void removeIgnoredCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            LayoutOptionLists();
        }

        private void OnProcessStart()
        {
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel3.Text = "";
            outputTextBox.Clear();
            startButton.Enabled = false;
            auButton.Enabled = false;
            settingsButton.Enabled = false;
            helpMenuStrip.Items["checkForInstallerUpdatesToolStripMenuItem"].Enabled = false;
            helpMenuStrip.Items["githubAPIRateLimitInfoToolStripMenuItem"].Enabled = false;
            startButton.BackgroundImage = Resources.start_working;
            logger.Log("Starting selected operation(s)...", "info");
            DisableCheckboxes(operationCheckboxes);
            DisableCheckboxes(optionCheckboxes);
        }

        private void OnProcessFinish()
        {
            EnableCheckboxes(operationCheckboxes);
            EnableCheckboxes(optionCheckboxes);
            startButton.Enabled = false;
            auButton.Enabled = true; 
            settingsButton.Enabled= true;
            helpMenuStrip.Items["checkForInstallerUpdatesToolStripMenuItem"].Enabled = true;
            helpMenuStrip.Items["githubAPIRateLimitInfoToolStripMenuItem"].Enabled = true;
            startButton.BackgroundImage = Resources.start_complete;
            reinstallCheckBox.Checked = false;
            uninstallCheckBox.Checked = false;
            UpdateUI();
        }
        private void startButton_Click_1(object sender, EventArgs e)
        {

            if(!Helper.IsGameRunning(priconnePath)) 
                installer.ProcessOperation(assetLink, installCheckBox.Checked, uninstallCheckBox.Checked, reinstallCheckBox.Checked, launchCheckBox.Checked, removeConfigCheckBox.Checked, configListBox, removeIgnoredCheckBox.Checked);
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeUI();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            installer.HandleFormClosing(this, e);
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void priconnePathLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (priconnePathValid)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("explorer.exe");
                startInfo.Arguments = priconnePath;
                Process.Start(startInfo);
            }
        }
        private void versionLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/HetCreep/PriconneReALLTL-Installer/releases/latest");
        }
        private void latestReleaseLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (latestVersion != null) Process.Start(Helper.GetCurrentPatchSource().ReleasesPage);
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            helpMenuStrip.Show(aboutButton, new System.Drawing.Point(0, aboutButton.Height));
        }

        private void helpMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/HetCreep/PriconneReALLTL-Installer/wiki");
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            // Required unofficial / no-affiliation / ToS-ban / AS-IS notice (see .claude/rules/ecc/domain/legal-boundary.md).
            string disclaimer =
                "\n\nUNOFFICIAL — not affiliated with, endorsed by, or associated with Cygames, DMM, or tynave.\n"
                + "\"Princess Connect! Re:Dive\" and related names/assets belong to their respective owners (used for identification only).\n"
                + "Installing translation mods (via the BepInEx mod loader) may violate the game's Terms of Service and could put your account at risk of suspension/ban.\n"
                + "Use at your own risk. Provided AS-IS, without warranty or liability.";
            MessageBox.Show($"[PriconneReALLTL Installer version: {String.Format(Application.ProductVersion)}]\n"
            + Settings.Default.aboutText + disclaimer, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void startButton_EnabledChanged(object sender, EventArgs e)
        {
            startButton.BackgroundImage = startButton.Enabled ? Resources.start_idle : Resources.start_disabled;
        }

        private void launchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            operationsPanel.Height = launchCheckBox.Checked ? 184 : 154; 
            Settings.Default.launchState = launchCheckBox.Checked;
        }

        private void modeLabel_TextChanged(object sender, EventArgs e)
        {
            operationToolTipPicture.Location = new Point(operationLabel.Right + 10, operationToolTipPicture.Top);
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            settingsMenuStrip.Show(settingsButton, new System.Drawing.Point(0, settingsButton.Height));
        }

        // Clears the cached patch downloads (zipcache, ~330 MB/zip). Frees disk; they re-download on the
        // next install/update. Settings + the installed patch are untouched. Wired into the Settings menu.
        private void ClearDownloadCache()
        {
            var r = MessageBox.Show("Delete cached patch downloads?\n\nThey'll be re-downloaded the next time you install or update. Your settings and the installed patch are not affected.", "Clear download cache", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            long freed = installer.ClearZipCache();
            logger.Log($"Cleared download cache — freed {freed / (1024 * 1024)} MB.", "success", true);
        }

        private void editIgnoredFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IgnoreForm ignoreForm = new IgnoreForm(priconnePath);
            ignoreForm.ShowDialog();
        }

        private void auButton_Click(object sender, EventArgs e)
        {
            helper.CreateAutoUpdaterShortcut(priconnePath);
        }

        private void settingsButton_EnabledChanged(object sender, EventArgs e)
        {
            settingsButton.BackgroundImage = settingsButton.Enabled ? Resources.scroll_closed_res2 : Resources.scroll_disabled;
        }

        private void auButton_EnabledChanged(object sender, EventArgs e)
        {
            auButton.BackgroundImage = auButton.Enabled ? Resources.crystal_normal_res : Resources.crystal_disabled;
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            // Arch B: this link now opens the "Launch Shortcuts" manager (wrap/restore).
            currentLauncherLinkLabel.Text = "Manage launch shortcuts";
            checkForInstallerUpdatesToolStripMenuItem.Checked = Settings.Default.checkForInstallerUpdates;
        }
        private async void MainForm_Shown(object sender, EventArgs e)
        {
            if (Settings.Default.checkForInstallerUpdates)
            {
                try
                {
                    // Off the UI thread so the self-update check never freezes the window on show.
                    var r = await Task.Run(() => installer.GetLatestInstallerRelease());
                    helper.CheckForInstallerUpdate(r.version, r.body, r.assetLink, r.versionValid);
                }
                catch (Exception ex)
                {
                    logger.Error("Error checking for installer update: " + ex.Message);
                }
            }
        }

        private void launcherSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LauncherForm LauncherForm = new LauncherForm();
            LauncherForm.ShowDialog();
        }

        private void importExportSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IEForm iEForm = new IEForm(priconnePath);
            iEForm.ShowDialog();
        }

        private void currentLauncherLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LauncherForm LauncherForm = new LauncherForm();
            LauncherForm.ShowDialog();
        }

        // ─── Translation patch source selector (created in code; no Designer edit) ───
        private void SetupPatchSourceSelector()
        {
            if (patchSourceLinkLabel != null) return; // create once

            // Give "TL Source:" its own line between the panel title ("TL Patch Information")
            // and "TL Patch Versions:" — shift the existing version content down once.
            const int shift = 30;
            foreach (System.Windows.Forms.Control c in patchInfoPanel.Controls)
                if (c != patchInfoLabel) c.Top += shift;
            patchInfoPanel.Height += shift;

            // The patch panel just grew by `shift`; push everything below it (the Operations /
            // Options panels and the start/log area) down by the same amount so nothing overlaps.
            // The form's base height already accounts for this (FitAndCenter 602 / 882).
            int belowTop = operationsPanel.Top;
            foreach (System.Windows.Forms.Control c in this.Controls)
                if (c.Top >= belowTop) c.Top += shift;

            patchSourceLinkLabel = new System.Windows.Forms.LinkLabel
            {
                AutoSize = true,
                BackColor = System.Drawing.Color.Transparent,
                Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline,
                LinkColor = System.Drawing.Color.MediumVioletRed,
                VisitedLinkColor = System.Drawing.Color.MediumVioletRed,
                Location = new System.Drawing.Point(8, patchInfoLabel.Bottom + 3),
                Name = "patchSourceLinkLabel",
                TabStop = true
            };
            toolTip.SetToolTip(patchSourceLinkLabel, "Click to choose the translation patch source.");
            patchSourceLinkLabel.LinkClicked += patchSourceLinkLabel_LinkClicked;
            patchInfoPanel.Controls.Add(patchSourceLinkLabel);
            patchSourceLinkLabel.BringToFront();
            RefreshPatchSourceLabel();
        }

        private void RefreshPatchSourceLabel()
        {
            if (patchSourceLinkLabel != null)
                patchSourceLinkLabel.Text = "TL Source: " + Helper.GetCurrentPatchSource().ShortName + "  ▼ change";
        }

        private void patchSourceLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var menu = new ContextMenuStrip();
            var sources = Helper.PatchSources;
            for (int i = 0; i < sources.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(sources[i].DisplayName)
                {
                    Checked = (index == Settings.Default.selectedPatchSource)
                };
                item.Click += (s, ev) => SelectPatchSource(index);
                menu.Items.Add(item);
            }
            menu.Show(patchSourceLinkLabel, new System.Drawing.Point(0, patchSourceLinkLabel.Height));
        }

        private async void SelectPatchSource(int index)
        {
            if (index == Settings.Default.selectedPatchSource)
            {
                RefreshPatchSourceLabel();
                return;
            }
            Settings.Default.selectedPatchSource = index;
            Settings.Default.Save();
            RefreshPatchSourceLabel();
            // Switching source changes the context — clear any pending operation/option selection so the
            // user re-picks fresh (avoids a stale checked operation carrying over + the options confusion).
            foreach (CheckBox cb in operationCheckboxes) cb.Checked = false;
            foreach (CheckBox cb in optionCheckboxes) cb.Checked = false;
            // Source switch: re-fetch latest off the UI thread (bypass the cache for a live read).
            await LoadLatestVersionInfoAsync(bypassCache: true);
            // Toggle plugin DLLs (.dll <-> .dll.bak) to match the newly selected source right
            // away, so an already-installed patch reflects the active source without reinstalling.
            helper.ApplyPluginProfile(priconnePath);
        }

        private async void checkForInstallerUpdatesToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.checkForInstallerUpdates = checkForInstallerUpdatesToolStripMenuItem.Checked;
            Settings.Default.Save();
            if (checkForInstallerUpdatesToolStripMenuItem.Checked)
            {
                try
                {
                    // Off the UI thread so toggling this never freezes the window on a slow/403 GitHub response.
                    (string version, string body, string installerAssetlink, bool versionValid) =
                        await Task.Run(() => installer.GetLatestInstallerRelease());
                    helper.CheckForInstallerUpdate(version, body, installerAssetlink, versionValid);
                }
                catch (Exception ex)
                {
                    logger.Error("Error checking for installer update: " + ex.Message);
                }
            }
        }

        // On-demand "Check for Updates Now" (separate from the startup-only auto-check toggle). Always
        // runs, bypasses the 6h version cache for a live read, and reports the result either way —
        // newer → the self-update dialog, up to date / unreachable → a MessageBox.
        private async void checkForUpdatesNow_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Log("Checking for installer updates...", "info", true);
                Helper.BypassVersionCache = true;   // force a live fetch, ignore the 6h cache
                (string version, string body, string installerAssetlink, bool versionValid) =
                    await Task.Run(() => installer.GetLatestInstallerRelease());
                Helper.BypassVersionCache = false;

                if (!versionValid)
                {
                    MessageBox.Show("Couldn't check for updates — GitHub is unreachable or rate-limited. Try again later.", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int cmp = Helper.NormalizeVersion(Application.ProductVersion).CompareTo(Helper.NormalizeVersion(version));
                if (cmp < 0)
                {
                    helper.CheckForInstallerUpdate(version, body, installerAssetlink, versionValid);   // shows the SelfUpdateForm
                }
                else
                {
                    MessageBox.Show($"You're on the latest version ({Helper.NormalizeVersion(Application.ProductVersion)}).", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Helper.BypassVersionCache = false;
                logger.Error("Error checking for installer update: " + ex.Message);
                MessageBox.Show("Error checking for updates: " + ex.Message, "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void githubAPIRateLimitInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // CheckGithubRateLimit hits the network — run it off the UI thread so the menu click
                // never freezes the window on a slow/rate-limited response.
                (int remaining, DateTime resetTime, TimeSpan timeUntilReset, string username) =
                    await Task.Run(() => Helper.CheckGithubRateLimit());
                string auth = username == null ? "No" : $"Yes (Username: {username})";
                MessageBox.Show($"GitHub API rate limit info:\n\nAuthenticated: {auth}\nRemaining API calls: {remaining}\nResets at: {resetTime}, in: {timeUntilReset:mm\\:ss}", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("Error checking GitHub rate limit: " + ex.Message);
            }
        }

        private void gitHubAPISettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GithubForm GitHubForm = new GithubForm();
            GitHubForm.ShowDialog();
        }
    }
}


