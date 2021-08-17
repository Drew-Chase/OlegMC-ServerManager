using ChaseLabs.CLConfiguration.List;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;

namespace OlegMC.REST_API.Data
{
    /// <summary>
    /// A List of static global variables.
    /// </summary>
    public static class Global
    {

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

        public static string GetUniqueTempFolder(string username)
        {
            string path = Path.Combine(Root, "temp", username);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
        public static void DestroyUniqueTempFolder(string username)
        {
            Directory.Delete(GetUniqueTempFolder(username), true);
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


        public static void GenRuntime()
        {
            Console.WriteLine("Extracting Runtime Binaries");
            string os = OperatingSystem.IsWindows() ? "Win64" : OperatingSystem.IsLinux() ? "Linux64" : "Unix64";
            System.IO.Compression.ZipFile.ExtractToDirectory(Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName, "Runtime", $"{os}.zip"), Runtime);
            Console.WriteLine("Done Extracting Runtime Binaries");
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
        public static string LocalIP
        {
            get
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (ip.ToString().StartsWith("192.168"))
                            return ip.ToString();
                    }
                }
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }
        public static string PublicIP
        {
            get
            {
                WebClient client = new();
                string ip = client.DownloadString("https://auth.api.openboxhosting.com/whatsmyip.php");
                client.Dispose();
                return ip;
            }
        }
        public static bool IsLoggedIn => File.Exists(Path.Combine(Root, "auth"));
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
            string local = $"{protocol}://{LocalIP}:{port}";
            string remote = $"{protocol}://{PublicIP}:{port}";

            ConfigManager manager = new(Path.Combine(Root, "auth"), true);
            manager.Add("local", local);
            manager.Add("remote", remote);
            manager.Add("port", port);
            manager.Add("token", token);

            SyncInfoWithServer();
        }

        public static void SyncInfoWithServer(bool force = false)
        {
            string cfg = Path.Combine(Root, "auth");
            if (!File.Exists(cfg))
                return;
            ConfigManager manager = new(cfg, true);
            if (manager.GetConfigByKey("token") == null || manager.GetConfigByKey("local") == null || manager.GetConfigByKey("remote") == null || manager.GetConfigByKey("port") == null) return;
            string token = manager.GetConfigByKey("token").Value;
            string localCFG = manager.GetConfigByKey("local").Value;
            string remoteCFG = manager.GetConfigByKey("remote").Value;
            int port = manager.GetConfigByKey("port").ParseInt();

            string local = $"{localCFG.Split(":")[0]}://{LocalIP}:{port}";
            string remote = $"{localCFG.Split(":")[0]}://{PublicIP}:{port}";
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
                File.Delete(Path.Combine(Root, "auth"));
        }
    }


    /// <summary>
    /// A custom class to allow me to gather average cpu usage from a given process.
    /// </summary>
    public class GetCPUUsage
    {
        TimeSpan start;
        public double CPUUsageTotal { get; private set; }
        public double CPUUsageLastMinute { get; private set; }

        TimeSpan oldCPUTime = new(0);
        DateTime lastMonitorTime = DateTime.UtcNow;
        public DateTime StartTime = DateTime.UtcNow;
        private Timer timer;

        /// <summary>
        /// Call once the process has started.
        /// </summary>
        /// <param name="process">The started process</param>
        public void OnStartup(Process process)
        {
            start = process.TotalProcessorTime;
            timer = new(2500)
            {
                AutoReset = true,
                Enabled = true,
            };
            CallCPU(process);
            timer.Elapsed += (s, e) => CallCPU(process);
            timer.Start();
        }

        /// <summary>
        /// Run when from the process on exit event.
        /// </summary>
        public void OnClose()
        {
            timer.Stop();
            CPUUsageTotal = 0;
        }

        /// <summary>
        /// Updates the cpu counter
        /// </summary>
        /// <param name="process">The process from the <seealso cref="OnStartup(Process)"/> function</param>
        void CallCPU(Process process)
        {
            //TimeSpan newCPUTime = process.TotalProcessorTime - start;
            //CPUUsageLastMinute = (newCPUTime - oldCPUTime).TotalSeconds / (Environment.ProcessorCount * DateTime.UtcNow.Subtract(lastMonitorTime).TotalSeconds);
            //lastMonitorTime = DateTime.UtcNow;
            //CPUUsageTotal = Math.Round((newCPUTime.TotalSeconds / (Environment.ProcessorCount * DateTime.UtcNow.Subtract(StartTime).TotalSeconds)) * 100, 2);
            //oldCPUTime = newCPUTime;
            CPUUsageTotal = GetCpuUsageForProcess(process).Result;

        }
        private async Task<double> GetCpuUsageForProcess(Process process)
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }
    }

}