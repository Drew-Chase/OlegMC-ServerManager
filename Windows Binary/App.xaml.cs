using ChaseLabs.CLConfiguration.List;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace OlegMC.Windows_Binary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Windows.Forms.NotifyIcon NotifyIcon;
        System.Windows.Forms.ContextMenuStrip contextMenu;
        System.Windows.Forms.ToolStripItem showConsole;
        Process api;
        private bool isConsoleVisible = false;

        public App()
        {
            SystemTray();
            StartAPI();
            //Update();
        }

        private void SystemTray()
        {
            NotifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)
            };
            NotifyIcon.MouseDoubleClick += (s, e) =>
            {
            };
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
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "OlegMC");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, "auth");

            if (File.Exists(path))
            {
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
                }
                else
                {
                    File.Delete(path);
                    RestartAPI();
                }
            }
            else
            {
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
            api.Kill();
            StartAPI();
        }
        private void ShowConsole()
        {
            if (api != null && !api.HasExited)
            {
                api.StandardInput.WriteLine("show");
                showConsole.Text = "Hide Console";
                isConsoleVisible = true;
                showConsole.Click -= (s, e) => ShowConsole();
                showConsole.Click += (s, e) => HideConsole();
            }
        }
        private void HideConsole()
        {
            if (api != null && !api.HasExited)
            {
                api.StandardInput.WriteLine("hide");
                showConsole.Text = "Show Console";
                showConsole.Click -= (s, e) => HideConsole();
                showConsole.Click += (s, e) => ShowConsole();
                isConsoleVisible = false;
            }
        }

        private void Close()
        {
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

        private void Update()
        {
            System.Timers.Timer timer = new System.Timers.Timer(2 * 1000)
            {
                AutoReset = true,
                Enabled = true,
            };
            timer.Elapsed += (s, e) =>
            {
            };
            timer.Start();
        }

    }
}
