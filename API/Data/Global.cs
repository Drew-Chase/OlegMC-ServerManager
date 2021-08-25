using ChaseLabs.CLConfiguration.List;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
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

        public static class Paths
        {
            public static string ExecutingBinary => Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName, $"{AppDomain.CurrentDomain.FriendlyName}{(OperatingSystem.IsWindows() ? ".exe" : "")}");
            /// <summary>
            /// The root directory of the application
            /// </summary>
            public static string Root
            {
                get
                {
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LFInteractive", "OlegMC");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    return path;
                }
            }

            /// <summary>
            /// The Servers path.
            /// </summary>
            public static string ServersPath
            {
                get
                {
                    string path = Path.Combine(Root, "Servers");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    return path;
                }
            }
            /// <summary>
            /// The path to the java runtime executables.
            /// </summary>
            public static string Runtime
            {
                get
                {
                    string os = OperatingSystem.IsWindows() ? "Win64" : OperatingSystem.IsLinux() ? "Linux64" : "Unix64";
                    string path = Path.Combine(Root, "Runtime", os);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
            }

            /// <summary>
            /// The path to the server backup directories
            /// </summary>
            public static string BackupPath
            {
                get
                {
                    string path = Path.Combine(Root, "Backups");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    return path;
                }
            }
        }

        public static class Functions
        {
            public static void SafelyCreateZipFromDirectory(string sourceDirectoryName, string zipFilePath)
            {
                using FileStream zipToOpen = new(zipFilePath, FileMode.Create);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);

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
                        Debug.WriteLine($"Unable to add {file}");
                        continue;
                    }
                }

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
                    GenRuntime().Wait();
                }
                return path;
            }


            public static async Task GenRuntime(bool force = false)
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

                        Console.WriteLine("Downloading Runtime Binaries");
                        string os = OperatingSystem.IsWindows() ? "Win64" : OperatingSystem.IsLinux() ? "Linux64" : "Unix64";
                        ProgressBar progress = new ProgressBar();
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            progress.Report((double)e.ProgressPercentage / 100);
                        };
                        client.DownloadFileCompleted += (s, e) =>
                        {
                            progress.Dispose();
                            Console.WriteLine("Extracting Runtime Binaries");
                            System.IO.Compression.ZipFile.ExtractToDirectory(temp, Paths.Runtime);
                            Console.WriteLine("Done Extracting Runtime Binaries");
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
                    Console.WriteLine("Updated Server Locations");
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