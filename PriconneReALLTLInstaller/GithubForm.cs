using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HelperFunctions;
using Newtonsoft.Json.Linq;
using PriconneReALLTLInstaller.Properties;

namespace PriconneReALLTLInstaller
{
    public partial class GithubForm: BaseForm
    {
        public GithubForm()
        {
            InitializeComponent();

            var buttonImageMappings = new List<(Button button, Image normal, Image hover, EventHandler extraMouseEnterEvent, EventHandler extraMouseLeaveEvent)>
            {
                (backButton, Resources.back_arrow, Resources.back_arrow_lit, null, null),
                (saveButton, Resources.savetokenbutton, Resources.savetokenbutton_lit, null, null),
                (validateButton, Resources.validatetokenbutton, Resources.validatetokenbutton_lit, null, null),
            };

            RegisterButtonImagesBulk(buttonImageMappings);
        }

        private void InitializeUI()
        {
            apiKeyTextbox.UseSystemPasswordChar = true;   // a GitHub token is a credential — mask it on screen (DPAPI-encrypted at rest)
            // #73: do NOT load the decrypted token into the textbox — that would hold the plaintext as a
            // long-lived managed string inside a UI control (credential-vault "no long-lived field").
            // Leave it blank; a stored token still enables Validate (which reads the STORED token, not the
            // textbox), and Save (disabled until the user types) encrypts only newly-typed input.
            apiKeyTextbox.Text = "";
            validateButton.Enabled = !string.IsNullOrEmpty(Settings.Default.GithubAPIKey);
            saveButton.Enabled = false;
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void GithubForm_Load(object sender, EventArgs e)
        {
            InitializeUI();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                Settings.Default.GithubAPIKey = Helper.EncryptString(apiKeyTextbox.Text);
                Settings.Default.Save();
                MessageBox.Show("API key saved!", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                saveButton.Enabled = false;
                validateButton.Enabled = string.IsNullOrEmpty(Settings.Default.GithubAPIKey) ? false : true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot save API key!\nException: {ex.Message}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private void apiKeyTextbox_TextChanged(object sender, EventArgs e)
        {
            saveButton.Enabled = true;
        }

        private void saveButton_EnabledChanged(object sender, EventArgs e)
        {
            saveButton.BackgroundImage = saveButton.Enabled ? Resources.savetokenbutton : Resources.savetokenbutton_disabled;
        }

        private void validateButton_EnabledChanged(object sender, EventArgs e)
        {
            validateButton.BackgroundImage = validateButton.Enabled ? Resources.validatetokenbutton : Resources.validatetokenbutton_disabled;
        }

        private async void validateButton_Click(object sender, EventArgs e)
        {
            // #67: ValidateGitHubToken does a synchronous WebClient call — run it OFF the UI thread so a
            // cold (never-validated) token can't freeze the window. #66: guard the async void.
            try
            {
                validateButton.Enabled = false;
                var (tokenvalid, username) = await Task.Run(() => Helper.ValidateGitHubToken(Helper.DecryptString(Settings.Default.GithubAPIKey)));
                if (tokenvalid) MessageBox.Show($"Token valid!\n\nUsername: {username}", "Token validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                {
                    Settings.Default.GithubAPIKey = "";
                    Settings.Default.Save();
                    apiKeyTextbox.Text = "";
                    MessageBox.Show("Token invalid! Clearing saved token!", "Token validation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    saveButton.Enabled = false;
                }
            }
            catch (Exception ex) { MessageBox.Show("Could not validate the token: " + ex.Message, "Token validation", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { validateButton.Enabled = !string.IsNullOrEmpty(Settings.Default.GithubAPIKey); }
        }
    }
}
