using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace NetworkWidgetInstaller
{
    public static class Installer
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            bool silent = false;
            bool elevatedPass = false;

            foreach (string arg in args)
            {
                if (arg.Equals("/silent", StringComparison.OrdinalIgnoreCase))
                    silent = true;
                if (arg.Equals("/elevated", StringComparison.OrdinalIgnoreCase))
                    elevatedPass = true;
            }

            if (elevatedPass)
                silent = true;

            try
            {
                string installDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NetworkWidget"
                );
                Directory.CreateDirectory(installDir);

                string exePath = Path.Combine(installDir, "NetworkWidget.exe");
                CloseRunningWidget(exePath);
                ExtractWidget(exePath);

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs"
                );
                string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

                CreateShortcut(Path.Combine(desktop, "Network Widget.lnk"), exePath);
                CreateShortcut(Path.Combine(startMenu, "Network Widget.lnk"), exePath);
                CreateShortcut(Path.Combine(startup, "Network Widget.lnk"), exePath);
                EnsureElevatedTasks(exePath, elevatedPass);

                if (!silent)
                {
                    MessageBox.Show(
                        "Network Widget installato.\n\nLo trovi sul Desktop e nel menu Start.",
                        "Installazione completata",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                Process.Start(exePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Errore installazione", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ExtractWidget(string exePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream input = assembly.GetManifestResourceStream("NetworkWidget.exe"))
            {
                if (input == null)
                    throw new InvalidOperationException("Risorsa NetworkWidget.exe non trovata.");

                using (FileStream output = File.Create(exePath))
                    input.CopyTo(output);
            }
        }

        private static void CloseRunningWidget(string exePath)
        {
            foreach (Process process in Process.GetProcessesByName("NetworkWidget"))
            {
                try
                {
                    if (process.MainModule.FileName.Equals(exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(1500))
                            process.Kill();
                    }
                }
                catch
                {
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.IconLocation = targetPath + ",0";
            shortcut.Description = "Widget trasparente per Wi-Fi ed Ethernet";
            shortcut.Save();
        }

        private static void EnsureElevatedTasks(string exePath, bool elevatedPass)
        {
            if (!IsAdministrator())
            {
                if (!elevatedPass)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(Application.ExecutablePath, "/elevated /silent");
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    Process.Start(startInfo);
                }

                return;
            }

            CreateElevatedTask(@"\NetworkWidget\WiFiEnable", exePath, "--toggle \"Wi-Fi\" enable");
            CreateElevatedTask(@"\NetworkWidget\WiFiDisable", exePath, "--toggle \"Wi-Fi\" disable");
            CreateElevatedTask(@"\NetworkWidget\EthernetEnable", exePath, "--toggle \"Ethernet\" enable");
            CreateElevatedTask(@"\NetworkWidget\EthernetDisable", exePath, "--toggle \"Ethernet\" disable");
        }

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void CreateElevatedTask(string taskName, string exePath, string arguments)
        {
            string taskRun = "\"" + exePath + "\" " + arguments;
            string schtasksArgs = "/Create /F /TN \"" + taskName + "\" /TR \"" + taskRun + "\" /SC ONCE /ST 23:59 /RL HIGHEST";

            ProcessStartInfo startInfo = new ProcessStartInfo("schtasks.exe", schtasksArgs);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }
    }
}
