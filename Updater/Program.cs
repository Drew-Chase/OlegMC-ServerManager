using System;
using System.IO;

namespace OlegMC.Updater
{
    internal class Program
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog Logger = ChaseLabs.CLLogger.LogManager.Init().SetLogDirectory(Path.Combine(Directory.CreateDirectory(Path.Combine(Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "OlegMC")).FullName, "Logs", "updater")).FullName, "latest.Logger")).SetPattern("[%TYPE%: %DATE%]: %MESSAGE%");

        private static void Main(string[] args)
        {
            Console.Title = "OlegMC Updater";
            Logger.Info("Updating OlegMC - Server Manager");
            System.Threading.Thread.Sleep(5000);
            string path = string.Empty;
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLower();
                    if (arg.StartsWith("-path="))
                    {
                        path = arg.Replace("-path=", "");
                        Logger.Debug($"PATH Identified as \"{path}\"");
                    }
                }
                if (!string.IsNullOrWhiteSpace(path))
                {
                    string updateTemp = Path.Combine(Path.GetTempPath(), "oleg-server-update.zip");
                    using System.Net.WebClient client = new();
                    string os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "osx" : "unsupported";
                    client.DownloadFileAsync(new Uri($"https://dl.openboxhosting.com/byos.php?os={os}"), updateTemp);
                    ProgressBar bar = new();
                    client.DownloadProgressChanged += (sender, @event) =>
                    {
                        bar.Report((double)@event.ProgressPercentage / 100);
                    };
                    client.DownloadFileCompleted += (sender, @event) =>
                    {
                        //Directory.Delete(path, true);
                        System.Threading.Thread.Sleep(500);
                        System.IO.Compression.ZipFile.ExtractToDirectory(updateTemp, path, true);
                        System.Threading.Thread.Sleep(500);
                        System.Diagnostics.ProcessStartInfo info = new();
                        if (OperatingSystem.IsWindows())
                        {
                            info = new()
                            {
                                FileName = Path.Combine(path, "OlegMC.exe"),
                                Arguments = "-firstLaunch"
                            };
                        }
                        else if (OperatingSystem.IsLinux())
                        {
                            info = new()
                            {
                                FileName = "mono",
                                Arguments = $"{Path.Combine(path, "OlegMC")} -firstLaunch"
                            };
                        }
                        else { return; }
                        new System.Diagnostics.Process()
                        {
                            StartInfo = info
                        }.Start();
                        return;
                    };
                }
            }
            else
            {
                Logger.Error("Path is Needed");
                Console.Write("Press Enter To Exit...");
                Console.ReadLine();
            }
        }
    }
}