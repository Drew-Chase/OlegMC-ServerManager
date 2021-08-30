using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace OlegMC.REST_API.Data
{
    public static class UpdateManager
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = Global.Logger;
        public static bool CheckForUpdates()
        {
            string jsonString = string.Empty;
            using (System.Net.WebClient client = new())
            {
                string os = OperatingSystem.IsWindows() ? "windows/OlegMC" : "linux";
                try
                {
                    jsonString = client.DownloadString($"https://dl.openboxhosting.com/byos/{os}/OlegMC.deps.json");
                }
                catch
                {
                    return false;
                }
            }
            JObject json = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            (int remoteRelease, int remoteMajor, int remoteMinor) = ExtractVersion(json);
            (int localRelease, int localMajor, int localMinor) = ExtractVersion((JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(Directory.GetParent(Global.Paths.ExecutingBinary).FullName, $"{new FileInfo(Global.Paths.ExecutingBinary).Name.Replace(new FileInfo(Global.Paths.ExecutingBinary).Extension, "")}.deps.json"))));
            return (remoteMinor != localMinor || remoteMajor != localMajor || remoteRelease != localRelease);
        }
        public static void Update(bool force = false)
        {
            if (CheckForUpdates() || force)
            {
                Update();
            }
        }
        private static void Update()
        {
            string os = OperatingSystem.IsWindows() ? "oleg-updater.exe" : "oleg-updater";
            string updateExe = Path.Combine(Path.GetTempPath(), os);
            if (File.Exists(updateExe))
            {
                File.Delete(updateExe);
            }

            log.Debug($"cmd /c \"{updateExe} -path='{Directory.GetParent(Global.Paths.ExecutingBinary).Parent.FullName}'\"");
            Console.ReadLine();
            using (System.Net.WebClient client = new())
            {
                client.DownloadFile($"https://dl.openboxhosting.com/updater/{os}", updateExe);
            }
            System.Diagnostics.ProcessStartInfo info = new();
            if (OperatingSystem.IsWindows())
            {
                info = new()
                {
                    FileName = "cmd",
                    Arguments = $"/c \"{updateExe} -path='{Directory.GetParent(Global.Paths.ExecutingBinary).Parent.FullName}'\""
                };
            }

            new System.Diagnostics.Process()
            {
                StartInfo = info

            }.Start();
        }
        private static (int release, int major, int minor) ExtractVersion(JObject json)
        {
            string version = json["targets"][".NETCoreApp,Version=v5.0"].ToString().Split(":")[0].Replace("{", "").Replace("\"", "").Replace("OlegMC/", "").Trim();
            int release, major, minor;
            string[] versions = version.Split('.');
            try
            {
                release = int.Parse(versions[0].ToString().Replace(".", ""));
                major = int.Parse(versions[1].ToString().Replace(".", ""));
                minor = int.Parse(versions[2].ToString().Replace(".", ""));
            }
            catch (FormatException e)
            {
                log.Error(e);
                return (0, 0, 0);
            }
            return (release, major, minor);
        }
    }
}
