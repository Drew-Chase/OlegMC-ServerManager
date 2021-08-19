using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OlegMC.REST_API.Data;
using OlegMC.REST_API.Model;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OlegMC.REST_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--updateServer":
                            Global.SyncInfoWithServer(true);
                            continue;
                        case "--withConsole":
                            if (OperatingSystem.IsWindows())
                            {
                                ModifyWindow(true);
                            }
                            continue;
                    }
                }
            }
            else
            {
                if (OperatingSystem.IsWindows())
                {
                    ModifyWindow(false);
                }
            }

            string path = Path.Combine(Global.Runtime, 8.ToString(), "bin", $"java{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");

            if (!File.Exists(path))
            {
                Global.GenRuntime();
            }
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


        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(kestral =>
                    {
                        kestral.ListenAnyIP(5077);
                    }).UseStartup<Startup>();
                });
        }
    }
}
