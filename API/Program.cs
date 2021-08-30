using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OlegMC.REST_API.Data;
using OlegMC.REST_API.Model;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OlegMC.REST_API
{
    public class Program
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = Global.Logger;
        public static void Main(string[] args)
        {
            Console.Title = "OlegMC - Server Manager";
            if (OperatingSystem.IsWindows())
            {
                ModifyWindow(false);
            }
            if (!Networking.IsPortOpen(Global.API_PORT).Result)
            {
                Networking.OpenPort(Global.API_PORT).Wait();
            }

            if (args.Length != 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-firstLaunch":
                            AddToFirewall();
                            continue;
                        case "-updateServer":
                            Global.Functions.SyncInfoWithServer(true);
                            continue;
                        case "-show":
                            if (OperatingSystem.IsWindows())
                            {
                                ModifyWindow(true);
                            }
                            continue;
                        case "-genRuntime":
                            Global.Functions.GenRuntime(true);
                            continue;
                        case "-firewall":
                            FirewallManager.FirewallCom firewall = new();
                            firewall.AddAuthorizeApp(
                                                        new("OlegMC - Server Manager", Global.Paths.ExecutingBinary)
                                                        {
                                                            Enabled = true
                                                        });
                            firewall.AddAuthorizeApp(
                                new("OlegMC - Server Manager (java 16 runtime)", Global.Functions.GetRuntimeExecutable(JavaVersion.Latest))
                                {
                                    Enabled = true
                                });

                            firewall.AddAuthorizeApp(
                                new("OlegMC - Server Manager (java 8 runtime)", Global.Functions.GetRuntimeExecutable(JavaVersion.Legacy))
                                {
                                    Enabled = true
                                });
                            Environment.Exit(0);
                            continue;
                        default:
                            continue;
                    }
                }
            }
            Global.Functions.GenRuntime();
            Global.Functions.SyncInfoWithServer();
            if (OperatingSystem.IsWindows())
            {
                Task.Run(() =>
                {
                    Thread.Sleep(3000);
                    WaitForCommand();
                });
            }
            if (!Global.IsLoggedIn)
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = $"http://127.0.0.1:{Global.API_PORT}"
                    }
                }.Start();
            }
            _ = ServersListModel.GetInstance;
            CreateHostBuilder(args).Build().Run();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => OnClose();
        }

        public static void ModifyWindow(bool show)
        {
            if (OperatingSystem.IsWindows())
            {
                [DllImport("kernel32.dll")]
                static extern IntPtr GetConsoleWindow();

                [DllImport("user32.dll")]
                static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

                const int SW_HIDE = 0;
                const int SW_SHOW = 5;

                IntPtr handle = GetConsoleWindow();
                ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
            }
        }

        public static void AddToFirewall()
        {
            if (OperatingSystem.IsWindows())
            {
                log.Info("Adding Firewall Rule");
                Process process = new()
                {
                    StartInfo = new()
                    {
                        FileName = Global.Paths.ExecutingBinary,
                        Arguments = "-firewall",
                        Verb = "runas",
                        UseShellExecute = true,
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }

        private static void WaitForCommand()
        {
            string command = Console.ReadLine();
            switch (command.ToLower())
            {
                case "help":
                    log.Debug("You need help.");
                    break;
                case "show":
                    ModifyWindow(true);
                    break;
                case "hide":
                    ModifyWindow(false);
                    break;
                default:
                    log.Error($"\"{command}\" is not a reconnized command!");
                    log.Warn("Type help for more information");
                    break;
            }
            Thread.Sleep(500);
            WaitForCommand();

        }

        private static void OnClose()
        {
            ServersListModel.GetInstance.StopAllServers();
            if (Networking.IsPortOpen(Global.API_PORT).Result)
            {
                Networking.ClosePort(Global.API_PORT).Wait();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(kestral =>
                    {
                        kestral.ListenAnyIP(Global.API_PORT);
                    }).UseStartup<Startup>();
                });
        }
    }
}
