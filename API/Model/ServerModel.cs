using ChaseLabs.CLConfiguration.List;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OlegMC.REST_API.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using TraceLd.MineStatSharp;

namespace OlegMC.REST_API.Model
{
    #region Enumerators
    /// <summary>
    /// <see cref="Vanilla"/> A basic minecraft server<br />
    /// <see cref="Forge"/> A forge based server<br />
    /// <see cref="Fabric"/> A fabric based server<br />
    /// <see cref="Spigot"/> A Spigot, Craftbukkit, Bukkit, Paper, or Bungiee based server<br />
    /// <see cref="Other"/> A custom .jar server
    /// </summary>
    public enum ServerType
    {
        Vanilla,
        Forge,
        Fabric,
        Spigot,
        Other,
    }
    /// <summary>
    /// <see cref="Normal"/> Stops the server by typing stop in the console. <br />
    /// <see cref="Kill"/> Terminates the process running the server. <br />
    /// <see cref="Restart"/> Stops server <seealso cref="Normal"/>, or by <seealso cref="Kill"/> and then restarts it.
    /// </summary>
    public enum StopMethod
    {
        Normal,
        Kill,
        Restart,
    }

    public enum ServerStatus
    {
        Offline,
        Online,
        Restarting,
        Stopping,
        Killing,
        Installing,
        BackingUp,
        Saving,
    }
    #endregion
    /// <summary>
    /// The outline for a basic server.
    /// </summary>
    public class ServerModel
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = Data.Global.Logger;
        #region Variables
        #region public

        public object JSONObject
        {
            get
            {
                if (ServerProcess != null && !ServerProcess.HasExited)
                {
                    ServerProcess.Refresh();
                }

                return new
                {
                    Status = CurrentStatus.ToString(),
                    PlayersOnline = CurrentPlayerCount,
                    MaxPlayers = MaxPlayerCount,
                    PlanTier = ServerPlan.Name,
                    CPU = _cpuUsage,
                    RAM = ServerProcess == null || ServerProcess.HasExited ? 0 : Math.Round(ServerProcess.PrivateMemorySize64 / 1024.0 / 1024 / 1024, 2),
                    MaxRAM = Max_Ram,
                    Port = ServerProperties.GetByName("server-port") != null ? ServerProperties.GetByName("server-port").Value : string.Empty,
                    IsModded = Directory.Exists(Path.Combine(ServerPath, "mods")),
                    IsPlugin = Directory.Exists(Path.Combine(ServerPath, "plugins")),
                    ModLoader = ServerType,
                    JavaVersion = Java_Version,
                };
            }
        }

        public bool IsRunning => ServerProcess != null && !ServerProcess.HasExited;

        public List<string> ConsoleLog { get; private set; }

        public ServerStatus PreviousStatus { get; set; }
        private ServerStatus _current = ServerStatus.Offline;
        public ServerStatus CurrentStatus { get => _current; set { PreviousStatus = _current; _current = value; } }
        public BackupListModel Backups { get; set; }
        public bool HasStartJar = false;
        public bool HasInstallJar = false;

        /// <summary>
        /// Gets/Sets the max number of players allowed on the server
        /// </summary>
        public int MaxPlayerCount
        {
            get
            {
                if (ServerProperties.GetByName("max-players") != null && int.TryParse(ServerProperties.GetByName("max-players").Value, out int v))
                {
                    return v;
                }

                return 20;
            }
        }
        /// <summary>
        /// Gets the number of players currently on the server
        /// </summary>
        public int CurrentPlayerCount { get; private set; }
        /// <summary>
        /// Gets the <seealso cref="ServerPropertiesModel"/> <b><i>(server.properties)</i></b>
        /// </summary>
        public ServerPropertiesModel ServerProperties { get; private set; }
        /// <summary>
        /// Gets the <seealso cref="ServerType"/> of the server.
        /// </summary>
        public ServerType ServerType { get; set; }
        /// <summary>
        /// Gets the <seealso cref="PlanModel"/> of the server.
        /// </summary>
        public PlanModel ServerPlan { get; private set; }
        /// <summary>
        /// Returns the server directory.
        /// </summary>
        public string ServerPath { get; private set; }
        /// <summary>
        /// Returns the process currently holding the servers runtime.
        /// </summary>
        public Process ServerProcess { get; private set; }
        public ConfigManager config { get; private set; }
        public int Java_Version
        {
            get => java_version;
            set
            {
                config.GetConfigByKey("java").Value = value.ToString();
                java_version = value;
            }
        }
        public int Max_Ram
        {
            get => max_ram;
            set
            {
                if (ServerPlan.Name == "BYOS")
                {
                    config.GetConfigByKey("ram").Value = value.ToString();
                    max_ram = value;
                }
            }
        }
        #endregion
        #region private
        private int max_ram;
        private int java_version;
        private long _ramUsage = 0;
        private double _cpuUsage
        {
            get
            {
                if (ServerProcess != null && !ServerProcess.HasExited)
                {
                    return Task.Run(async () =>
                    {
                        try
                        {
                            DateTime startTime = DateTime.UtcNow;
                            TimeSpan startCpuUsage = ServerProcess.TotalProcessorTime;
                            await Task.Delay(500);
                            if (ServerProcess == null || ServerProcess.HasExited || CurrentStatus == ServerStatus.Offline)
                            {
                                return 0;
                            }

                            return Math.Round((ServerProcess.TotalProcessorTime - startCpuUsage).TotalMilliseconds / (Environment.ProcessorCount * (DateTime.UtcNow - startTime).TotalMilliseconds) * 100, 2);
                        }
                        catch { return 0; }
                    }).Result;
                }

                return 0;
            }
        }
        private string java_path
        {
            get
            {
                string path = Path.Combine(Global.Paths.Runtime, Java_Version.ToString(), "bin", $"java{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");
                if (!File.Exists(path))
                {
                    Global.Functions.GenRuntime();
                }
                return path;
            }
        }
        #endregion
        #endregion

        public ServerModel(PlanModel plan)
        {
            ServerPlan = plan;
            ServerPath = Path.Combine(Global.Paths.ServersPath, plan.Username);
            Directory.CreateDirectory(ServerPath);
            ServerProperties = ServerPropertiesModel.Init(ServerPath);
            string olegIdentifier = Path.Combine(ServerPath, "olegmc.server");
            config = new(olegIdentifier, false);

            config.Add("plan", plan.Name);
            config.Add("username", plan.Username);
            config.Add("ram", plan.RAM);
            config.Add("java", 16);
            config.Add("backups_enabled", false);
            config.Add("backup_intervals", 0);
            config.Add("max_backups", 5);
            ServerPlan.MaxBackups = 5;

            java_version = config.GetConfigByKey("java").ParseInt();
            max_ram = config.GetConfigByKey("ram").ParseInt();

            ConsoleLog = new();
            AcceptEULA();
            ForceScan();
            Backups = new(this, true);
        }

        #region Functions
        #region public

        public void DownloadServer(string version)
        {
            if (HasStartJar)
            {
                File.Delete(Path.Combine(ServerPath, "start.jar"));
            }
            if (HasInstallJar)
            {
                File.Delete(Path.Combine(ServerPath, "installer.jar"));
            }
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                switch (ServerType)
                {
                    case ServerType.Vanilla:

                        JObject manifest = (JObject)JsonConvert.DeserializeObject(client.DownloadString("https://launchermeta.mojang.com/mc/game/version_manifest.json"));
                        foreach (JObject v in (JArray)manifest["versions"])
                        {
                            if (v["id"].ToString() == version)
                            {
                                JObject versionManifest = (JObject)JsonConvert.DeserializeObject(client.DownloadString(v["url"].ToString()));
                                client.DownloadFile(versionManifest["downloads"]["server"]["url"].ToString(), Path.Combine(ServerPath, "installer.jar"));
                            }
                        }
                        break;
                }

                client.Dispose();
            }

            InstallServer();
        }

        /// <summary>
        /// Starts the server normally.
        /// </summary>
        /// <returns>if the server started successfully</returns>
        public bool StartServer()
        {
            ForceScan();
            if (HasStartJar)
            {
                if (ServerProperties.GetByName("server-port") != null && int.TryParse(ServerProperties.GetByName("server-port").Value, out int port))
                {
                    Task.Run(async () =>
                    {
                        if (!await Networking.IsPortOpen(port))
                        {
                            await Networking.OpenPort(port);
                        }
                    }).Wait();
                }
                AcceptEULA();
                log.Info($"Starting Server for {ServerPlan.Username}");
                try
                {
                    ServerProcess = new()
                    {
                        StartInfo = new()
                        {
                            FileName = java_path,
                            Arguments = $"-Xms128M -Xmx{Max_Ram}G -jar start.jar nogui",
                            UseShellExecute = false,
                            CreateNoWindow = false,
                            WorkingDirectory = ServerPath,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true
                        }
                    };
                    ServerProcess.Start();

                    _ramUsage = ServerProcess.WorkingSet64 / 1024 / 1024 / 1024;
                    string[] search_cmds = { @"Starting minecraft server version", @"Loading for game Minecraft" };

                    Timer timer = new(60 * 1000) { AutoReset = true, Enabled = true };
                    timer.Elapsed += (s, e) => UpdatePlayersOnline();
                    timer.Start();

                    ServerProcess.Exited += (s, o) =>
                    {

                        if (ServerProperties.GetByName("server-port") != null && int.TryParse(ServerProperties.GetByName("server-port").Value, out int port))
                        {
                            Task.Run(async () =>
                            {
                                if (await Networking.IsPortOpen(port))
                                {
                                    await Networking.ClosePort(port);
                                }
                            }).Wait();
                        }
                        timer.Stop();
                        CurrentStatus = ServerStatus.Offline;
                        ServerProcess = null;
                        if (ConsoleLog.Contains(@"Error: A JNI error has occurred, please check your installation and try again"))
                        {
                            Java_Version = 16;
                            StartServer();
                        }
                        foreach (string cmd in search_cmds)
                        {
                            if (ConsoleLog.Contains(cmd))
                            {
                                int index = ConsoleLog.IndexOf(cmd);
                                string version = ConsoleLog[index].Split(cmd)[^1];
                                int release = int.Parse(version.Split('.')[0].Replace(".", ""));
                                int major = int.Parse(version.Split('.')[1].Replace(".", ""));
                                if (major >= 16)
                                {
                                    Java_Version = 16;
                                }
                                else
                                {
                                    Java_Version = 8;
                                }

                                StartServer();
                                break;
                            }
                        }

                    };
                    ConsoleLog = new();
                    ServerProcess.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            string text = e.Data;
                            text = text.Replace(@"\u", "/u");
                            ConsoleLog.Add($"{text}");

                            if (text.Contains("joined the game") || text.Contains("left the game"))
                            {
                                UpdatePlayersOnline();
                            }
                            if (text.Contains("Saving the game"))
                            {
                                CurrentStatus = ServerStatus.Saving;
                            }
                            if (text.Contains("Saved the game"))
                            {
                                CurrentStatus = PreviousStatus;
                            }

                            foreach (string cmd in search_cmds)
                            {
                                if (ConsoleLog.Contains(cmd))
                                {
                                    int index = ConsoleLog.IndexOf(cmd);
                                    string version = ConsoleLog[index].Split(cmd)[^1];
                                    int release = int.Parse(version.Split('.')[0].Replace(".", ""));
                                    int major = int.Parse(version.Split('.')[1].Replace(".", ""));

                                    if (major >= 17 && Java_Version == 8)
                                    {
                                        Java_Version = 16;
                                        KillServer();
                                        StartServer();
                                    }
                                    else if (major < 17 && Java_Version == 16)
                                    {
                                        Java_Version = 8;
                                        KillServer();
                                        StartServer();
                                    }
                                    break;
                                }
                            }
                        }
                    };

                    ServerProcess.BeginOutputReadLine();

                    CurrentStatus = ServerStatus.Online;
                }
                catch
                {
                    CurrentStatus = ServerStatus.Offline;
                }
            }
            else
            {
                CurrentStatus = ServerStatus.Offline;
            }
            return CurrentStatus == ServerStatus.Online;
        }

        /// <summary>
        /// Accepts the server eula
        /// </summary>
        public void AcceptEULA()
        {
            string path = Path.Combine(ServerPath, "eula.txt");
            if (!File.Exists(path) || !File.ReadAllText(path).Contains("eula=true"))
            {
                File.WriteAllText(path, "eula=true");
            }
        }

        /// <summary>
        /// Stops the server using the <seealso cref="StopMethod"/>.
        /// </summary>
        /// <param name="method">The way in which to handle the server termination.</param>
        /// <returns>if the server was stopped successfully.</returns>
        public bool StopServer(StopMethod method)
        {
            if (CurrentStatus == ServerStatus.Online)
            {
                if (ServerProcess == null)
                {
                    return true;
                }
                CurrentStatus = ServerStatus.Stopping;
                return method switch
                {
                    StopMethod.Normal => StopServer(),
                    StopMethod.Kill => KillServer(),
                    StopMethod.Restart => RestartServer(),
                    _ => false,
                };
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// Installs server using the <seealso cref="ServerType"/>.
        /// </summary>
        /// <returns>if the server was installed correctly.</returns>
        public bool InstallServer()
        {
            ForceScan();
            CurrentStatus = ServerStatus.Installing;

            return ServerType switch
            {
                ServerType.Vanilla => InstallVanilla(),
                ServerType.Forge => InstallForge(),
                ServerType.Fabric => InstallFabric(),
                ServerType.Spigot => InstallSpigot(),
                ServerType.Other => InstallCustomServerType(),
                _ => InstallVanilla(),
            };
        }

        /// <summary>
        /// Scans the server path for a installer.jar and/or a start.jar<br />
        /// If one is found it will set the start and/or install jar to it.
        /// </summary>
        public void ForceScan()
        {
            HasStartJar = Directory.GetFiles(ServerPath, "start.jar", SearchOption.TopDirectoryOnly).Length > 0;
            HasInstallJar = Directory.GetFiles(ServerPath, "installer.jar", SearchOption.TopDirectoryOnly).Length > 0;
        }
        #endregion
        #region private

        /// <summary>
        /// Stops the server normally.<br />
        /// if the function runs more than 3x <seealso cref="KillServer"/> will run instead.
        /// </summary>
        /// <param name="ittertions">The number of times this function has been run recursivly. if the function runs more than 3x <seealso cref="KillServer"/> will run instead.</param>
        /// <returns>if the server was stopped successfully.</returns>
        private bool StopServer(int ittertions = 0)
        {
            bool failed = false;
            bool superFailed = false;
            ServerProcess.StandardInput.WriteLine("stop");
            System.Timers.Timer timer = new(15 * 1000);
            timer.AutoReset = false;
            timer.Start();
            timer.Elapsed += (s, e) =>
            {
                if (ServerProcess == null)
                {
                    return;
                }

                if (!ServerProcess.HasExited && ittertions < 2)
                {
                    superFailed = false;
                    failed = true;
                }
                else if (!ServerProcess.HasExited && ittertions > 2)
                {
                    failed = false;
                    superFailed = true;
                }
            };
            while (!ServerProcess.HasExited)
            {
                if (failed)
                {
                    return StopServer(ittertions + 1);
                }

                if (superFailed)
                {
                    return StopServer(StopMethod.Kill);
                }
            }
            return true;

        }
        /// <summary>
        /// Kills the server process.
        /// </summary>
        /// <returns>if the server process was killed successfully.</returns>
        private bool KillServer()
        {
            try
            {
                if (ServerProcess != null)
                {
                    CurrentStatus = ServerStatus.Killing;
                    ServerProcess.Kill(true);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Stopps the server normally using <seealso cref="StopServer(int)"/> and the starts it normally using <seealso cref="StartServer"/>.
        /// </summary>
        /// <returns>returns if the server was restarted successfully.</returns>
        private bool RestartServer()
        {
            CurrentStatus = ServerStatus.Restarting;
            if (StopServer())
            {
                return StartServer();
            }

            return false;
        }


        /// <summary>
        /// Installs custom jar server.
        /// </summary>
        /// <returns>if the server was installed correctly.</returns>
        private bool InstallCustomServerType()
        {
            return StartServer();
        }
        /// <summary>
        /// Installs a spigot based server (<seealso cref="ServerType.Spigot"/>) using start.jar.
        /// </summary>
        /// <returns>if the server was installed correctly.</returns>
        private bool InstallSpigot()
        {
            if (!HasStartJar && HasInstallJar)
            {
                File.Move(Path.Combine(ServerPath, "installer.jar"), Path.Combine(ServerPath, "start.jar"));
            }

            return StartServer();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>if the server was installed correctly.</returns>
        private bool InstallFabric()
        {

            ServerProcess = new()
            {
                StartInfo = new()
                {
                    FileName = java_path,
                    Arguments = $"-Xms128M -Xmx{ServerPlan.RAM}G -jar installer.jar server -downloadMinecraft",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = ServerPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };
            ServerProcess.Start();
            ConsoleLog = new();
            ServerProcess.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    string text = e.Data;
                    text = text.Replace(@"\u", "/u");
                    ConsoleLog.Add($"{text}");
                }
            };

            ServerProcess.BeginOutputReadLine();
            ServerProcess.Exited += (s, e) =>
            {
                CurrentStatus = ServerStatus.Offline;
                File.Move(Path.Combine(ServerPath, "fabric-server-launch.jar"), Path.Combine(ServerPath, "start.jar"));
            };
            return true;
        }
        /// <summary>
        /// Installs a forge server using the start.jar
        /// </summary>
        /// <returns>if the server was installed correctly.</returns>
        private bool InstallForge()
        {
            ServerProcess = new()
            {
                StartInfo = new()
                {
                    FileName = java_path,
                    Arguments = $"-Xms128M -Xmx{ServerPlan.RAM}G -jar installer.jar --installServer",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = ServerPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };
            ServerProcess.Start();

            ConsoleLog = new();
            ServerProcess.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    string text = e.Data;
                    text = text.Replace(@"\u", "/u");
                    ConsoleLog.Add($"{text}");
                }
            };

            ServerProcess.BeginOutputReadLine();

            string jar = "";
            ServerProcess.Exited += (s, e) =>
            {
                string[] jars = Directory.GetFiles(ServerPath, "forge*.jar", SearchOption.TopDirectoryOnly);
                if (jars.Length == 1)
                {
                    jar = jars[0];
                }
                else
                {
                    jar = "null";
                }

                CurrentStatus = ServerStatus.Offline;
            };
            if (jar.Equals("null"))
            {
                return false;
            }
            else
            {
                File.Move(jar, Path.Combine(ServerPath, "start.jar"));
                return true;
            }
        }
        /// <summary>
        /// Installs a vanilla server using the start.jar
        /// </summary>
        /// <returns>if the server was installed correctly.</returns>
        private bool InstallVanilla()
        {
            if (!HasStartJar && HasInstallJar)
            {
                File.Move(Path.Combine(ServerPath, "installer.jar"), Path.Combine(ServerPath, "start.jar"));
            }

            return StartServer();
        }

        private void UpdatePlayersOnline()
        {
            if (ushort.TryParse(ServerProperties.GetByName("server-port").Value, out ushort port))
            {
                MineStat mc = new("127.0.0.1", port);
                if (mc.ServerUp)
                {
                    if (ushort.TryParse(mc.CurrentPlayers, out ushort players))
                    {
                        CurrentPlayerCount = players;
                        return;
                    }
                }
            }
            CurrentPlayerCount = 0;

        }

        #endregion
        #endregion



    }
}
