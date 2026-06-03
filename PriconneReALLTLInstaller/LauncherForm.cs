using LoggerFunctions;
using PriconneReALLTLInstaller.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PriconneReALLTLInstaller
{
    public partial class LauncherForm : BaseForm
    {
        public LauncherForm()
        {
            InitializeComponent();

            RegisterMouseDrag(new List<Control> { panel1, panel2 });

            var buttonImageMappings = new List<(Button button, Image normal, Image hover, EventHandler extraMouseEnterEvent, EventHandler extraMouseLeaveEvent)>
            {
                (backButton, Resources.back_arrow, Resources.back_arrow_lit, null, null),
                (shortcutAddButton, Resources.shortcutadd_button, Resources.shortcutadd_button_lit, null, null),
                (shortcutRemoveButton, Resources.shortcutremove_button, Resources.shortcutremove_button_lit, null, null),
            };

            RegisterButtonImagesBulk(buttonImageMappings);
        }

        // ─── Migration helper: move legacy single-string setting into the list ───
        private void MigrateLegacyLink()
        {
            string legacy = Settings.Default.fastLauncherLink;
            if (!string.IsNullOrEmpty(legacy))
            {
                var links = GetLinks();
                if (!links.Contains(legacy))
                {
                    links.Add(legacy);
                    SaveLinks(links);
                }
                // Clear legacy value so we don't migrate again
                Settings.Default.fastLauncherLink = "";
                Settings.Default.Save();
            }
        }

        // ─── Helpers: read/write list from Settings ───────────────────────────────
        private List<string> GetLinks()
        {
            var col = Settings.Default.fastLauncherLinks;
            if (col == null) return new List<string>();
            return col.Cast<string>().ToList();
        }

        private void SaveLinks(List<string> links)
        {
            var col = new StringCollection();
            col.AddRange(links.ToArray());
            Settings.Default.fastLauncherLinks = col;
            Settings.Default.Save();
        }

        // ─── UI Initialization ────────────────────────────────────────────────────
        private void InitializeUI()
        {
            // Arch B: launcher selection is retired — launching is via wrapped shortcuts.
            // Hide the whole launcher-select panel (panel1, which holds the combobox) and
            // pull the shortcut-manager panel (panel2) up to fill the gap, then shrink the
            // form so the page isn't a big blank area on top.
            panel1.Visible = false;
            int gap = panel2.Top - panel1.Top;
            panel2.Top = panel1.Top;
            this.Height -= gap;

            setFastlauncherLinkLabel.Text = "Wrap a launcher shortcut so it updates the patch, then launches the game:";
            shortcutListLabel.Text = "Managed shortcuts (update + launch):";
        }

        private void UpdateUI()
        {
            shortcutAddButton.Enabled = true;
            RefreshListBox();
            shortcutRemoveButton.Enabled = shortcutListBox.Enabled && shortcutListBox.SelectedIndex >= 0;
        }

        private void RefreshListBox()
        {
            shortcutListBox.Items.Clear();
            var links = GetLinks();
            if (links.Count == 0)
            {
                shortcutListBox.Items.Add("(No shortcuts set)");
                shortcutListBox.Enabled = false;
            }
            else
            {
                // #49: the managed list stores absolute .lnk paths, so a shortcut moved or deleted
                // (e.g. dragged Desktop → Start Menu) leaves a stale entry. Flag a missing file so the
                // user can spot it and Remove it to clean the list. The suffix is display-only — the
                // index still maps to the real path for Remove.
                foreach (var link in links)
                    shortcutListBox.Items.Add(File.Exists(link) ? link : link + "   (missing — moved or deleted)");
                shortcutListBox.Enabled = true;
            }
        }

        // ─── Button Handlers ──────────────────────────────────────────────────────
        private void backButton_Click(object sender, EventArgs e) => this.Close();

        private void shortcutAddButton_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                openFileDialog1.Multiselect = true;
                openFileDialog1.Filter = "Shortcuts (*.lnk)|*.lnk";
                if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

                var links = GetLinks();
                int wrapped = 0, copied = 0;
                var failed = new System.Collections.Generic.List<string>();
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                foreach (string file in openFileDialog1.FileNames)
                {
                    if (helper.WrapShortcut(file))
                    {
                        if (!links.Contains(file)) links.Add(file);
                        wrapped++;
                        continue;
                    }
                    // #4b: an in-place wrap failed — usually the .lnk lives in a protected folder
                    // (e.g. the All-Users Start Menu, C:\ProgramData\…) that a per-user app can't write.
                    // Wrap a COPY on the Desktop so wrapping still works without admin rights.
                    try
                    {
                        string copy = System.IO.Path.Combine(desktop, System.IO.Path.GetFileNameWithoutExtension(file) + " (TL update).lnk");
                        System.IO.File.Copy(file, copy, true);
                        if (helper.WrapShortcut(copy))
                        {
                            if (!links.Contains(copy)) links.Add(copy);
                            copied++;
                            continue;
                        }
                        try { System.IO.File.Delete(copy); } catch { }
                    }
                    catch { }
                    failed.Add(System.IO.Path.GetFileName(file));
                }
                SaveLinks(links);
                UpdateUI();

                // #4a: ALWAYS report the result — never silent, even when nothing got wrapped.
                var sb = new System.Text.StringBuilder();
                if (wrapped > 0) sb.AppendLine($"Wrapped {wrapped} shortcut(s) in place.");
                if (copied > 0) sb.AppendLine($"{copied} shortcut(s) were in a protected folder — wrapped a copy on your Desktop instead (look for \"… (TL update)\").");
                if (failed.Count > 0) sb.AppendLine($"Could not wrap: {string.Join(", ", failed)}.");
                if (wrapped + copied > 0) sb.Append("Pressing a wrapped shortcut now updates the TL patch, then launches the game.");
                else sb.Append("Nothing was wrapped. If a shortcut is in a protected folder, copy it to your Desktop and wrap that copy.");
                MessageBox.Show(sb.ToString().Trim(),
                    (wrapped + copied > 0) ? "Done" : "Nothing wrapped",
                    MessageBoxButtons.OK,
                    (wrapped + copied > 0) ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot wrap shortcut!\nException: {ex.Message}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void shortcutRemoveButton_Click(object sender, EventArgs e)
        {
            int idx = shortcutListBox.SelectedIndex;
            if (idx < 0) return;

            var links = GetLinks();
            if (idx < links.Count)
            {
                string path = links[idx];
                // #48: a Desktop "(TL update)" copy is OUR artifact (the protected-folder original was
                // never modified) → delete it so Remove doesn't leave a dead file behind. An in-place-
                // wrapped original is the user's own .lnk → restore its launcher target instead.
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                bool isOurDesktopCopy = path.EndsWith(" (TL update).lnk", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Path.GetDirectoryName(path), desktop, StringComparison.OrdinalIgnoreCase);
                if (isOurDesktopCopy)
                {
                    try { if (File.Exists(path)) File.Delete(path); } catch { }
                }
                else
                {
                    helper.RestoreShortcut(path);   // put the original launcher target back into the .lnk
                }
                links.RemoveAt(idx);
                SaveLinks(links);
                UpdateUI();
            }
        }

        private void shortcutAddButton_EnabledChanged(object sender, EventArgs e)
        {
            shortcutAddButton.BackgroundImage = shortcutAddButton.Enabled
                ? Resources.shortcutadd_button
                : Resources.shortcutadd_button_disabled;
        }

        private void shortcutRemoveButton_EnabledChanged(object sender, EventArgs e)
        {
            shortcutRemoveButton.BackgroundImage = shortcutRemoveButton.Enabled
                ? Resources.shortcutremove_button
                : Resources.shortcutremove_button_disabled;
        }

        private void FastLauncherForm_Load(object sender, EventArgs e)
        {
            MigrateLegacyLink();
            InitializeUI();
            UpdateUI();
        }

        // Retired in Arch B (launcher-selection controls are hidden); kept as no-ops so the
        // Designer's wired event handlers still resolve.
        private void launcherComboBox_SelectedIndexChanged(object sender, EventArgs e) { }

        private void shortcutListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            shortcutRemoveButton.Enabled = shortcutListBox.Enabled && shortcutListBox.SelectedIndex >= 0;
        }

        private void saveButton_Click(object sender, EventArgs e) { }

        private void saveButton_EnabledChanged(object sender, EventArgs e) { }
    }
}
