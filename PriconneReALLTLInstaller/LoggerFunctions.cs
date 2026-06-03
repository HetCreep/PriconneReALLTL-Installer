using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System;

namespace LoggerFunctions
{
    // Fail-closed redaction applied to EVERY log line (file + UI) before it is written, so a GitHub
    // token (or any Authorization header value) can never leak into a log even if a future code path
    // accidentally passes one through. See .claude/rules/ecc/domain/log-sanitization.md.
    public static class LogRedactor
    {
        private static readonly System.Text.RegularExpressions.Regex GithubToken =
            new System.Text.RegularExpressions.Regex(@"gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex AuthHeader =
            new System.Text.RegularExpressions.Regex(@"(?i)\b(bearer|token)\s+[A-Za-z0-9_\-\.]{8,}", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static string Scrub(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            try
            {
                // Log-injection prevention (OWASP A09): neutralize CR/LF/tab FIRST so
                // attacker-controlled data (GitHub API messages, file paths, version
                // strings) can't forge fake log lines. Done before token redaction.
                string s = message.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                s = GithubToken.Replace(s, "[REDACTED_TOKEN]");
                s = AuthHeader.Replace(s, m => m.Groups[1].Value + " [REDACTED]");
                return s;
            }
            catch { return "[REDACTED]"; }   // if redaction itself throws, drop the content rather than risk a leak
        }
    }

    public class Logger
    {
        private string logFilePath;
        private Dictionary<string, Color> colors = new Dictionary<string, Color>
            {
                { "info", Color.Black },
                { "error", Color.Red },
                { "success", Color.Green },
                { "add", Color.Blue },
                { "remove", Color.Red },
            };
        private RichTextBox outputTextBox;
        private ToolStripStatusLabel toolStripStatusLabel1;

        public Logger(string logFilePath, RichTextBox outputTextBox, ToolStripStatusLabel toolStripStatusLabel1)
        {
            this.logFilePath = logFilePath;
            this.outputTextBox = outputTextBox;
            this.toolStripStatusLabel1 = toolStripStatusLabel1;
        }

        public void StartSession()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, false))
                {
                    writer.WriteLine($"[PriconneReALLTL Installer version: {String.Format(System.Windows.Forms.Application.ProductVersion)}]");
                    writer.WriteLine($"[Log file created at: {DateTime.Now}]");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing log file: {ex.Message}");
            }
        }

        public void Log(string message, string level, bool writeToToolStrip = false)
        {
            message = LogRedactor.Scrub(message);
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true)) writer.WriteLine($"[{DateTime.Now}] - {message}");

                if (outputTextBox != null && !outputTextBox.IsDisposed) outputTextBox.AppendText($"[{DateTime.Now}] - {message}" + Environment.NewLine, colors[level]);

                if (writeToToolStrip)
                {
                    toolStripStatusLabel1.ForeColor = colors[level];
                    // Keep the status bar within its frame — long messages (e.g. the cache-hit line) get
                    // an ellipsis; the full text is still in the log box + file.
                    toolStripStatusLabel1.Text = message.Length > 64 ? message.Substring(0, 61) + "..." : message;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public void Error(string message)
        {
            message = LogRedactor.Scrub(message);
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true)) writer.WriteLine($"[{DateTime.Now}] - ERROR: {message}");

                if (outputTextBox != null && !outputTextBox.IsDisposed) outputTextBox.AppendText($"[{DateTime.Now}] - ERROR:" + Environment.NewLine + message + Environment.NewLine, colors["error"]);

                toolStripStatusLabel1.ForeColor = colors["error"];
                toolStripStatusLabel1.Text = $"ERROR! - See log for details.";


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

    }
    public class AutoUpdateLogger
    {
        private string logFilePath;
        private Dictionary<string, Color> colors = new Dictionary<string, Color>
            {
                { "info", Color.Black },
                { "error", Color.Red },
                { "success", Color.Green },
                { "add", Color.Blue },
                { "remove", Color.Red },
            };
        private Label statusLabel;

        public AutoUpdateLogger(string logFilePath, Label statusLabel)
        {
            this.logFilePath = logFilePath;
            this.statusLabel = statusLabel;
        }

        public void StartSession()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, false))
                {
                    writer.WriteLine($"[PriconneReALLTL Installer (Autoupdate) version: {String.Format(System.Windows.Forms.Application.ProductVersion)}]");
                    writer.WriteLine($"[Log file created at: {DateTime.Now}]");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing log file: {ex.Message}");
            }
        }

        public void Log(string message, string level, bool writeToStatus = false)
        {
            message = LogRedactor.Scrub(message);
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true)) writer.WriteLine($"[{DateTime.Now}] - {message}");

                if (writeToStatus)
                {
                    statusLabel.ForeColor = colors[level];
                    statusLabel.Text = message;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public void Error(string message)
        {
            message = LogRedactor.Scrub(message);
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true)) writer.WriteLine($"[{DateTime.Now}] - ERROR: {message}");

                statusLabel.ForeColor = colors["error"];
                statusLabel.Text = $"ERROR! - See log for details.";

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

    }
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}