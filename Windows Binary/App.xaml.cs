using ChaseLabs.CLConfiguration.List;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace OlegMC.Windows_Binary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static ChaseLabs.CLLogger.Interfaces.ILog log = ChaseLabs.CLLogger.LogManager.Init().SetLogDirectory(Path.Combine(Directory.CreateDirectory(Path.Combine(Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "OlegMC")).FullName, "Logs", "win-service")).FullName, "latest.log")).SetPattern("[%TYPE%: %DATE%]: %MESSAGE%");

        private System.Windows.Forms.NotifyIcon NotifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripItem showConsole;
        private Process api;

        public App()
        {
            log.Debug("Starting Windows Service");
            SystemTray();
            StartAPI();
        }

        private void SystemTray()
        {
            log.Debug("Creating System Tray Icon");
            NotifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)
            };
            NotifyIcon.MouseDoubleClick += (s, e) =>
            {
            };
            log.Debug("Setting up system tray context items");
            contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open Control Panel", null, (s, e) => OpenControlPanel());
            showConsole = contextMenu.Items.Add("Show Console", null, (s, e) => ShowConsole());
            showConsole = contextMenu.Items.Add("Restart API", null, (s, e) => RestartAPI());
            contextMenu.Items.Add("Exit", null, (s, e) => Close());

            NotifyIcon.ContextMenuStrip = contextMenu;
            NotifyIcon.Visible = true;
        }

        private void OpenControlPanel()
        {
            log.Debug("Opening control panel");
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "OlegMC");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, "auth");

            if (File.Exists(path))
            {
                log.Debug("User is logged in!");
                ConfigManager manager = new ConfigManager(path, true);
                if (manager.GetConfigByKey("token") != null)
                {

                    new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = $"http://myaccount.openboxhosting.com#token={manager.GetConfigByKey("token").Value}"
                        }
                    }.Start();
                    log.Debug("Opening openbox control panel=> \"http://myaccount.openboxhosting.com\"");
                }
                else
                {
                    log.Error("User is not logged in");
                    log.Info("Restarting API and Opening login page");
                    File.Delete(path);
                    RestartAPI();
                }
            }
            else
            {
                log.Error("User is not logged in");
                log.Info("Opening login page");
                new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "http://127.0.0.1:5077"
                    }
                }.Start();
            }
        }

        private void StartAPI()
        {
            if (api == null || api.HasExited)
            {
                log.Debug("Starting API Process");
                api = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "OlegMC/OlegMC.exe",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = false,
                        UseShellExecute = false,
                    }
                };
                api.Exited += (s, e) =>
                {
                    showConsole.Visible = false;
                };
                api.Start();
                showConsole.Visible = true;
            }
        }
        private void RestartAPI()
        {
            log.Debug("Killing API Process");
            api.Kill();
            StartAPI();
        }
        private void ShowConsole()
        {
            if (api != null && !api.HasExited)
            {
                log.Debug("Opening Console");
                api.StandardInput.WriteLine("show");
                showConsole.Text = "Hide Console";
                showConsole.Click -= (s, e) => ShowConsole();
                showConsole.Click += (s, e) => HideConsole();
            }
        }
        private void HideConsole()
        {
            if (api != null && !api.HasExited)
            {
                log.Debug("Closing Console");
                api.StandardInput.WriteLine("hide");
                showConsole.Text = "Show Console";
                showConsole.Click -= (s, e) => HideConsole();
                showConsole.Click += (s, e) => ShowConsole();
            }
        }

        private void Close()
        {
            log.Warn("Closing Windows Service Application...");
            if (NotifyIcon != null)
            {
                NotifyIcon.Visible = false;
            }
            if (api != null && !api.HasExited)
            {
                api.Kill();
            }

            Environment.Exit(0);
        }
    }
}
