using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PriconneReALLTLInstaller
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Single-exe: Newtonsoft.Json is embedded as a resource (see the .csproj EmbeddedResource)
            // and resolved from there, so the installer runs without a loose Newtonsoft.Json.dll
            // sitting next to the exe. Registered before any Newtonsoft type is touched.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && args[0] == "autoupdate")
            {
                // A wrapped/created AutoUpdate shortcut may encode the launch target to run
                // after the patch update completes (base64 so paths/args need no quoting):
                //   --launch <b64 exe-or-lnk> [--targs <b64 args>] [--tdir <b64 workingdir>]
                // No --launch (or --dmm) => launch via the DMM Game Player URI (also fallback).
                string target = DecodeArg(args, "--launch");
                string targetArgs = DecodeArg(args, "--targs");
                string targetDir = DecodeArg(args, "--tdir");
                Application.Run(new AutoUpdateForm(target, targetArgs, targetDir));
            }
            else
            {
                Application.Run(new MainForm());
            }
        }

        // Loads an embedded assembly (e.g. Newtonsoft.Json.dll) from the exe's resources when the
        // runtime can't find it on disk. Returns null for anything not embedded so normal probing continues.
        private static System.Reflection.Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs eventArgs)
        {
            string wanted = new System.Reflection.AssemblyName(eventArgs.Name).Name + ".dll";
            System.Reflection.Assembly self = System.Reflection.Assembly.GetExecutingAssembly();
            string resource = self.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(wanted, StringComparison.OrdinalIgnoreCase));
            if (resource == null) return null;
            using (System.IO.Stream stream = self.GetManifestResourceStream(resource))
            {
                if (stream == null) return null;
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return System.Reflection.Assembly.Load(bytes);
            }
        }

        private static string DecodeArg(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                {
                    try { return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[i + 1])); }
                    catch { return null; }
                }
            }
            return null;
        }
    }
}
