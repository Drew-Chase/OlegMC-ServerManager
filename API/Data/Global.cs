using ChaseLabs.CLConfiguration.List;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Data
{
    public enum JavaVersion
    {
        Latest,
        Legacy
    }

    /// <summary>
    /// A List of static global variables.
    /// </summary>
    public static class Global
    {
        public static int API_PORT => 5077;
        public static bool IsLoggedIn => File.Exists(Path.Combine(Paths.Root, "auth"));
#if DEBUG
        public static ChaseLabs.CLLogger.Interfaces.ILog Logger = ChaseLabs.CLLogger.LogManager.Init().SetLogDirectory(Path.Combine(Paths.Logs, "latest.Logger")).SetPattern("[%TYPE%: %DATE%]: %MESSAGE%");
#else
        public static ChaseLabs.CLLogger.Interfaces.ILog Logger = ChaseLabs.CLLogger.LogManager.Init().SetLogDirectory(Path.Combine(Paths.Logs, "latest.Logger")).SetPattern("[%TYPE%: %DATE%]: %MESSAGE%").SetMinimumLogType(ChaseLabs.CLLogger.Lists.LogTypes.Info);
#endif

        public static class Paths
        {
            /// <summary>
            /// Path to the .exe file
            /// </summary>
            public static string ExecutingBinary => Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName, $"{AppDomain.CurrentDomain.FriendlyName}{(OperatingSystem.IsWindows() ? ".exe" : "")}");

            /// <summary>
            /// The root directory of the application
            /// </summary>
            public static string Root => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "OlegMC")).FullName;

            /// <summary>
            /// Directory that contains all the Logger files
            /// </summary>
            public static string Logs => Directory.CreateDirectory(Path.Combine(Root, "Logs", "api")).FullName;

            /// <summary>
            /// The Servers path.
            /// </summary>
            public static string ServersPath => Directory.CreateDirectory(Path.Combine(Root, "Servers")).FullName;

            /// <summary>
            /// The path to the java runtime executables.
            /// </summary>
            public static string Runtime => Directory.CreateDirectory(Path.Combine(Root, "Runtime", OperatingSystem.IsWindows() ? "Win64" : OperatingSystem.IsLinux() ? "Linux64" : "Unix64")).FullName;

            /// <summary>
            /// The path to the server backup directories
            /// </summary>
            public static string BackupPath => Directory.CreateDirectory(Path.Combine(Root, "Backups")).FullName;
        }

        public static class Functions
        {
            public static string SafelyCreateZipFromDirectory(string sourceDirectoryName, string zipFilePath)
            {
                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                    Thread.Sleep(500);
                }

                using ZipArchive archive = new(new FileStream(zipFilePath, FileMode.Create), ZipArchiveMode.Create);

                foreach (string file in Directory.GetFiles(sourceDirectoryName, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        string entryName = file.Split(new DirectoryInfo(sourceDirectoryName).Name)[^1].Trim('\\');
                        ZipArchiveEntry entry = archive.CreateEntry(entryName);
                        entry.LastWriteTime = File.GetLastWriteTime(file);
                        using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using Stream stream = entry.Open();
                        fs.CopyTo(stream);
                    }
                    catch
                    {
                        Logger.Debug($"Unable to add {file}");
                        continue;
                    }
                }
                return zipFilePath;
            }

            public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
            {
                DirectoryInfo dir = new(sourceDirName);

                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException(
                        "Source directory does not exist or could not be found: "
                        + sourceDirName);
                }

                DirectoryInfo[] dirs = dir.GetDirectories();

                Directory.CreateDirectory(destDirName);

                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(tempPath, false);
                }

                if (copySubDirs)
                {
                    foreach (DirectoryInfo subdir in dirs)
                    {
                        string tempPath = Path.Combine(destDirName, subdir.Name);
                        DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                    }
                }
            }

            public static string GetUniqueTempFolder(string username)
            {
                string path = Path.Combine(Paths.Root, "temp", username);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }

            public static void DestroyUniqueTempFolder(string username)
            {
                Directory.Delete(GetUniqueTempFolder(username), true);
            }

            public static string GetRuntimeExecutable(JavaVersion version)
            {
                return GetRuntimeExecutable(version == JavaVersion.Latest ? 16 : 8);
            }

            public static string GetRuntimeExecutable(int version)
            {
                string path = Path.Combine(Paths.Runtime, version.ToString(), "bin", $"java{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");
                if (!File.Exists(path))
                {
                    GenRuntime();
                }
                return path;
            }

            public static async void GenRuntime(bool force = false)
            {
                await Task.Run(() =>
                {
                    Thread.Sleep(1000 * 5);
                    string leg = Path.Combine(Paths.Runtime, 8.ToString(), "bin", $"java{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");
                    string lat = Path.Combine(Paths.Runtime, 16.ToString(), "bin", $"java{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");
                    if (!File.Exists(leg) || !File.Exists(lat) || force)
                    {
                        using WebClient client = new();
                        string temp = Path.Combine(Path.GetTempPath(), "oleg-server-runtime.zip");
                        if (File.Exists(temp))
                        {
                            File.Delete(temp);
                        }

                        Logger.Info("Downloading Runtime Binaries");
                        string os = OperatingSystem.IsWindows() ? "Win64" : OperatingSystem.IsLinux() ? "Linux64" : "Unix64";
                        ProgressBar progress = new();
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            progress.Report((double)e.ProgressPercentage / 100);
                        };
                        client.DownloadFileCompleted += (s, e) =>
                        {
                            progress.Dispose();
                            Logger.Info("Extracting Runtime Binaries");
                            System.IO.Compression.ZipFile.ExtractToDirectory(temp, Paths.Runtime);
                            Logger.Info("Done Extracting Runtime Binaries");
                            if (OperatingSystem.IsWindows())
                            {
                                Program.AddToFirewall();
                            }
                        };
                        client.DownloadFileAsync(new Uri($"https://dl.openboxhosting.com/runtime/{os}.zip"), temp);
                    }
                });
            }

            public static void LogIn(string username, string password, int port, string protocol)
            {
                string token = "";
                string URI = "https://auth.api.openboxhosting.com/login.php";
                string myParameters = $"email={username}&password={password}";
                using (WebClient wc = new())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    JObject result = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(wc.UploadString(URI, myParameters));
                    token = result["token"].ToString();
                }
                string local = $"{protocol}://{Networking.GetLocalIP()}:{port}";
                string remote = $"{protocol}://{Networking.GetPublicIP()}:{port}";

                ConfigManager manager = new(Path.Combine(Paths.Root, "auth"), true);
                manager.Add("local", local);
                manager.Add("remote", remote);
                manager.Add("port", port);
                manager.Add("token", token);

                SyncInfoWithServer();
            }

            public static void SyncInfoWithServer(bool force = false)
            {
                string cfg = Path.Combine(Paths.Root, "auth");
                if (!File.Exists(cfg))
                {
                    return;
                }

                ConfigManager manager = new(cfg, true);
                if (manager.GetConfigByKey("token") == null || manager.GetConfigByKey("local") == null || manager.GetConfigByKey("remote") == null || manager.GetConfigByKey("port") == null)
                {
                    return;
                }

                string token = manager.GetConfigByKey("token").Value;
                string localCFG = manager.GetConfigByKey("local").Value;
                string remoteCFG = manager.GetConfigByKey("remote").Value;
                int port = manager.GetConfigByKey("port").ParseInt();

                string local = $"{localCFG.Split(":")[0]}://{Networking.GetLocalIP()}:{port}";
                string remote = $"{localCFG.Split(":")[0]}://{Networking.GetPublicIP()}:{port}";
                if (!(localCFG.Equals(local) || remoteCFG.Equals(remote)) || force)
                {
                    string URI = "https://auth.api.openboxhosting.com/updateAccount.php";
                    using (WebClient wc = new())
                    {
                        wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        _ = wc.UploadString(URI, $"token={token}&setting=localVersionUrl!{local}");
                    }
                    using (WebClient wc = new())
                    {
                        wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        _ = wc.UploadString(URI, $"token={token}&setting=remoteVersionUrl!{remote}");
                    }

                    manager.Add("local", local);
                    manager.Add("remote", remote);
                    Logger.Debug("Updated Server Locations");
                }
            }

            public static void LogOut()
            {
                if (IsLoggedIn)
                {
                    File.Delete(Path.Combine(Paths.Root, "auth"));
                }
            }
        }
    }
}