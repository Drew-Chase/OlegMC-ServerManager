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
using static OlegMC.REST_API.Data.Global;

namespace OlegMC.REST_API.Model
{
    #region Enumerators

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
        Starting,
    }

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

    #endregion Enumerators

    /// <summary>
    /// The outline for a basic server.
    /// </summary>
    public class ServerModel
    {
        #region Variables

        #region public

        public bool HasInstallJar = false;

        public bool HasStartJar = false;

        public BackupListModel Backups { get; set; }

        public ConfigManager Config { get; private set; }

        public List<string> ConsoleLog { get; private set; }

        /// <summary>
        /// Gets the number of players currently on the server
        /// </summary>
        public int CurrentPlayerCount { get; private set; }

        public ServerStatus CurrentStatus { get => current_server_status; set { PreviousStatus = current_server_status; current_server_status = value; } }

        public bool IsRunning => ServerProcess != null && !ServerProcess.HasExited;

        public int JavaVersion
        {
            get => java_version;
            set
            {
                Config.GetConfigByKey("java").Value = value.ToString();
                java_version = value;
            }
        }

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
                    CPU = cpu_usage,
                    RAM = ServerProcess == null || ServerProcess.HasExited ? 0 : Math.Round(ServerProcess.PrivateMemorySize64 / 1024.0 / 1024 / 1024, 2),
                    MaxRAM = MaxRam,
                    Port = ServerProperties.GetByName("server-port") != null ? ServerProperties.GetByName("server-port").Value : string.Empty,
                    IsModded = Directory.Exists(Path.Combine(ServerPath, "mods")),
                    IsPlugin = Directory.Exists(Path.Combine(ServerPath, "plugins")),
                    ModLoader = ServerType,
                    JavaVersion = JavaVersion,
                };
            }
        }

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

        public int MaxRam
        {
            get => max_ram;
            set
            {
                if (ServerPlan.Name == "BYOS")
                {
                    Config.GetConfigByKey("ram").Value = value.ToString();
                    max_ram = value;
                }
            }
        }

        public ServerStatus PreviousStatus { get; set; }

        /// <summary>
        /// Returns the server directory.
        /// </summary>
        public string ServerPath { get; private set; }

        /// <summary>
        /// Gets the <seealso cref="PlanModel"/> of the server.
        /// </summary>
        public PlanModel ServerPlan { get; private set; }

        /// <summary>
        /// Returns the process currently holding the servers runtime.
        /// </summary>
        public Process ServerProcess { get; private set; }

        /// <summary>
        /// Gets the <seealso cref="ServerPropertiesModel"/> <b><i>(server.properties)</i></b>
        /// </summary>
        public ServerPropertiesModel ServerProperties { get; private set; }

        /// <summary>
        /// Gets the <seealso cref="ServerType"/> of the server.
        /// </summary>
        public ServerType ServerType { get; set; }

        #endregion public

        #region private

        private ServerStatus current_server_status = ServerStatus.Offline;
        private int java_version;
        private int max_ram;

        private double cpu_usage
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
                string path = Path.Combine(Paths.Runtime, JavaVersion.ToString(), "bin", $"java{(OperatingSystem.IsWindows() ? ".exe" : string.Empty)}");
                if (!File.Exists(path))
                {
                    Functions.GenRuntime();
                }
                return path;
            }
        }

        #endregion private

        #endregion Variables

        public ServerModel(PlanModel plan)
        {
            ServerPlan = plan;
            ServerPath = Path.Combine(Paths.ServersPath, plan.Username);
            Directory.CreateDirectory(ServerPath);
            ServerProperties = ServerPropertiesModel.Init(ServerPath);
            string olegIdentifier = Path.Combine(ServerPath, "olegmc.server");
            Config = new(olegIdentifier, false);

            Config.Add("plan", plan.Name);
            Config.Add("username", plan.Username);
            Config.Add("ram", plan.RAM);
            Config.Add("java", 16);
            Config.Add("backups_enabled", false);
            Config.Add("backup_intervals", 0);
            Config.Add("max_backups", 5);
            Config.Add("theme", "default");
            ServerPlan.MaxBackups = Config.GetConfigByKey("max_backups").ParseInt();

            java_version = Config.GetConfigByKey("java").ParseInt();
            max_ram = Config.GetConfigByKey("ram").ParseInt();

            ConsoleLog = new();
            AcceptEULA();
            ForceScan();
            Backups = new(this, true);
            if (Config.GetConfigByKey("backup_intervals").ParseInt() != 0)
            {
                Backups.CreateBackupSchedule(Config.GetConfigByKey("backup_intervals").ParseInt());
            }
        }

        #region Functions

        #region public

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
        /// Scans the server path for a installer.jar and/or a start.jar<br />
        /// If one is found it will set the start and/or install jar to it.
        /// </summary>
        public void ForceScan()
        {
            HasStartJar = Directory.GetFiles(ServerPath, "start.jar", SearchOption.TopDirectoryOnly).Length > 0;
            HasInstallJar = Directory.GetFiles(ServerPath, "installer.jar", SearchOption.TopDirectoryOnly).Length > 0;
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
                Logger.Info($"Starting Server for {ServerPlan.Username}");
                try
                {
                    ServerProcess = new()
                    {
                        StartInfo = new()
                        {
                            FileName = java_path,
                            Arguments = $"-Xms128M -Xmx{MaxRam}G -jar start.jar nogui",
                            UseShellExecute = false,
                            CreateNoWindow = false,
                            WorkingDirectory = ServerPath,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true
                        }
                    };
                    ServerProcess.Start();

                    Timer timer = new(60 * 1000) { AutoReset = true, Enabled = true };
                    timer.Elapsed += (s, e) => UpdatePlayersOnline();
                    timer.Start();

                    ServerProcess.Exited += (s, o) =>
                    {
                        CurrentPlayerCount = 0;
                        timer.Stop();
                        MonitorServerOutput();
                    };
                    ConsoleLog = new();
                    ServerProcess.OutputDataReceived += (s, e) =>
                    {
                        MonitorServerOutput(e.Data);
                    };

                    ServerProcess.BeginOutputReadLine();

                    CurrentStatus = ServerStatus.Starting;
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

        #endregion public

        #region private

        /// <summary>
        /// Installs custom jar server.
        /// </summary>
        /// <returns>if the server was installed correctly.</returns>
        private bool InstallCustomServerType()
        {
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

        private void MonitorServerOutput()
        {
            string[] search_cmds = { @"Starting minecraft server version", @"Loading for game Minecraft" };
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
            CurrentStatus = ServerStatus.Offline;
            ServerProcess = null;
            if (ConsoleLog.Contains(@"Error: A JNI error has occurred, please check your installation and try again"))
            {
                JavaVersion = 16;
                StartServer();
            }
            foreach (string cmd in search_cmds)
            {
                if (ConsoleLog.Contains(cmd))
                {
                    if (int.Parse(ConsoleLog[ConsoleLog.IndexOf(cmd)].Split(cmd)[^1].Split('.')[1].Replace(".", "")) >= 16)
                    {
                        JavaVersion = 16;
                    }
                    else
                    {
                        JavaVersion = 8;
                    }

                    StartServer();
                    break;
                }
            }
        }

        private void MonitorServerOutput(string text)
        {
            if (!ServerProcess.HasExited)
            {
                ConsoleLog.Add($"{text}");
                switch (text)
                {
                    case string a when (a.Contains("joined the game") || a.Contains("left the game")):
                        UpdatePlayersOnline();
                        break;

                    case string a when (CurrentStatus == ServerStatus.Starting && a.Contains(")! For help, type \"help\"")):
                        CurrentStatus = ServerStatus.Online;
                        break;

                    case string a when a.Contains("Saving the game"):
                        CurrentStatus = ServerStatus.Saving;
                        break;

                    case string a when a.Contains("Saved the game"):
                        CurrentStatus = PreviousStatus;
                        break;
                }
            }
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
            Timer timer = new(15 * 1000);
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

        #endregion private

        #endregion Functions
    }
}