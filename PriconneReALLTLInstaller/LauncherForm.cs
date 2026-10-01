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
                        // A destination that does not exist yet, copied WITHOUT overwrite: two protected shortcuts
                        // sharing a name (or an unrelated Desktop file) must never clobber each other. Because the
                        // path is unique, a failed wrap below deletes only a file this operation created.
                        string copy = UniqueDesktopCopyPath(desktop, System.IO.Path.GetFileNameWithoutExtension(file));
                        System.IO.File.Copy(file, copy, false);
                        if (helper.WrapShortcut(copy))
                        {
                            helper.MarkAsManagedCopy(copy);   // ownership lives in the file, not in its name
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
                // #48: a Desktop copy THIS installer created (it carries an ownership marker — never inferred from
                // the file name) is our artifact (the protected-folder original was never modified) → delete it so
                // Remove doesn't leave a dead file behind. Any other shortcut is the user's own .lnk → restore its
                // launcher target instead, and never delete it.
                bool cleaned;
                string why = null;
                if (!File.Exists(path))
                {
                    cleaned = true;                       // confirmed absence — nothing left to undo
                }
                else if (helper.IsManagedCopy(path))
                {
                    try { File.Delete(path); cleaned = true; }
                    catch (Exception ex) { cleaned = false; why = ex.Message; }
                }
                else
                {
                    // RestoreShortcut also returns false for "not wrapped (nothing to restore)" — that is already clean.
                    cleaned = helper.RestoreShortcut(path) || !helper.IsWrappedShortcut(path);
                    if (!cleaned) why = "the shortcut could not be restored to its original launcher";
                }

                if (!cleaned)
                {
                    // Keep the entry: dropping it now would leave a wrapped shortcut that later cleanup (and the
                    // uninstaller's --unwrap-all) no longer knows about, pointing at a removed exe.
                    MessageBox.Show($"Could not remove this shortcut:\n{path}\n\n{why}\n\nIt was kept in the list so you can retry (close any program using it first).",
                        "Shortcut not removed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                links.RemoveAt(idx);
                SaveLinks(links);
                UpdateUI();
            }
        }

        // "<name> (TL update).lnk" on the Desktop, or "(TL update 2)", "(TL update 3)" … if that name is taken.
        private static string UniqueDesktopCopyPath(string desktop, string baseName)
        {
            string candidate = Path.Combine(desktop, baseName + " (TL update).lnk");
            for (int n = 2; File.Exists(candidate) && n < 1000; n++)
                candidate = Path.Combine(desktop, $"{baseName} (TL update {n}).lnk");
            return candidate;
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

    }
}
