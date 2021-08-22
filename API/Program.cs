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
        public static void Main(string[] args)
        {
            if (OperatingSystem.IsWindows())
            {
                ModifyWindow(false);
                Console.Title = "OlegMC - Server Manager";
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
                        case "-updateServer":
                            Global.SyncInfoWithServer(true);
                            continue;
                        case "-show":
                            if (OperatingSystem.IsWindows())
                            {
                                ModifyWindow(true);
                            }
                            continue;
                        case "-genRuntime":
                            Global.GenRuntime(true).Wait();
                            continue;
                        case "-firewall":
                            FirewallManager.FirewallCom firewall = new();
                            firewall.AddAuthorizeApp(
                                new("OlegMC - Server Manager", Global.ExecutingBinary)
                                {
                                    Enabled = true
                                });
                            firewall.AddAuthorizeApp(
                                new("OlegMC - Server Manager (java 16 runtime)", Global.GetRuntimeExecutable(Global.JavaVersion.Latest))
                                {
                                    Enabled = true
                                });

                            firewall.AddAuthorizeApp(
                                new("OlegMC - Server Manager (java 8 runtime)", Global.GetRuntimeExecutable(Global.JavaVersion.Legacy))
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
            Global.GenRuntime();
            Global.SyncInfoWithServer();
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
            CreateHostBuilder(args).Build().Run();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => OnClose();
        }

        public static void ModifyWindow(bool show)
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

        public static void AddToFirewall()
        {
            Console.WriteLine("Adding Firewall Rule");
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = Global.ExecutingBinary,
                    Arguments = "-firewall",
                    Verb = "runas",
                    UseShellExecute = true,
                }
            };
            process.Start();
            process.WaitForExit();
        }

        private static void WaitForCommand()
        {
            Console.Write(">> ");
            string command = Console.ReadLine();
            switch (command.ToLower())
            {
                case "help":
                    Console.WriteLine("You need help.");
                    break;
                case "show":
                    ModifyWindow(true);
                    break;
                case "hide":
                    ModifyWindow(false);
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{command} is not a reconnized command!");
                    Console.WriteLine("Type help for more information");
                    Console.ForegroundColor = ConsoleColor.White;
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
