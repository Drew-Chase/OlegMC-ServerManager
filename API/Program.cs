using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OlegMC.REST_API.Data;
using OlegMC.REST_API.Model;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
                            Global.GenRuntime(true);
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
            CreateHostBuilder(args).Build().Run();
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                ServersListModel.GetInstance.StopAllServers();
            };
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
