using OlegMC.REST_API.Data;
using System;
using System.Diagnostics;
using System.IO;

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
        Other
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
    #endregion
    /// <summary>
    /// The outline for a basic server.
    /// </summary>
    public class ServerModel
    {
        #region Variables
        #region public

        public object JSONObject => new
        {
            Running = IsRunning,
            PlayersOnline = CurrentPlayerCount,
            MaxPlayers = MaxPlayerCount,
            PlanTier = ServerPlan.Name,
            CPU = _cpuUsage,
            RAM = _ramUsage
        };

        /// <summary>
        /// Gets if the server is running or not.<br />
        /// Set to <b>true</b> to run the server.<br />
        /// Set to <b>false</b> to stop the server normally.
        /// </summary>
        public bool IsRunning { get; private set; }
        public BackupModel Backups { get; set; }
        public bool HasStartJar = false;
        public bool HasInstallJar = false;

        /// <summary>
        /// Gets/Sets the max number of players allowed on the server
        /// </summary>
        public int MaxPlayerCount { get; set; }
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
        #endregion
        #region private
        private Int64 _ramUsage = 0;
        private double _cpuUsage = 0;
        #endregion
        #endregion

        public ServerModel(PlanModel plan)
        {
            ServerPlan = plan;
            ServerPath = Path.Combine(Global.ServersPath, plan.Username);
            if (!Directory.Exists(ServerPath))
            {
                Directory.CreateDirectory(ServerPath);
            }

            ServerProperties = ServerPropertiesModel.Init(ServerPath);
            string olegIdentifier = Path.Combine(ServerPath, "olegmc.server");
            StreamWriter oleg = File.CreateText(olegIdentifier);
            oleg.WriteLine(plan.Name);
            oleg.WriteLine(plan.Username);
            oleg.Flush();
            oleg.Dispose();
            oleg.Close();
            AcceptEULA();
            ForceScan();
            Backups = new(this, true);
        }

        #region Functions
        #region public
        /// <summary>
        /// Starts the server normally.
        /// </summary>
        /// <returns>if the server started successfully</returns>
        public bool StartServer()
        {
            AcceptEULA();
            Console.WriteLine($"Starting Server for {ServerPlan.Username}");
            try
            {

                ServerProcess = new()
                {
                    StartInfo = new()
                    {
                        FileName = "java",
                        Arguments = $"-Xms128M -Xmx{ServerPlan.RAM}G -jar start.jar nogui",
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        WorkingDirectory = ServerPath,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true
                    }
                };
                ServerProcess.Start();
                _ramUsage = ServerProcess.WorkingSet64;
                _cpuUsage = ServerProcess.TotalProcessorTime.TotalMilliseconds;
                ServerProcess.Exited += (s, o) => { IsRunning = false; ServerProcess = null; };
                IsRunning = true;
            }
            catch
            {
                IsRunning = false;
            }
            return IsRunning;
        }

        /// <summary>
        /// Accepts the server eula
        /// </summary>
        public void AcceptEULA()
        {
            File.WriteAllText(Path.Combine(ServerPath, "eula.txt"), "eula=true");
        }

        /// <summary>
        /// Stops the server using the <seealso cref="StopMethod"/>.
        /// </summary>
        /// <param name="method">The way in which to handle the server termination.</param>
        /// <returns>if the server was stopped successfully.</returns>
        public bool StopServer(StopMethod method)
        {
            if (IsRunning)
            {
                if (ServerProcess == null) return true;
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
            if (StopServer())
                return StartServer();
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
                    FileName = "java",
                    Arguments = $"-Xms128M -Xmx{ServerPlan.RAM}G -jar installer.jar server -downloadMinecraft",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = ServerPath,
                    //RedirectStandardInput = true,
                    //RedirectStandardOutput = true
                }
            };
            ServerProcess.EnableRaisingEvents = true;
            ServerProcess.Exited += (s, e) =>
            {
                File.Move(Path.Combine(ServerPath, "fabric-server-launch.jar"), Path.Combine(ServerPath, "start.jar"));
            };
            ServerProcess.Start();
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
                    FileName = "java",
                    Arguments = $"-Xms128M -Xmx{ServerPlan.RAM}G -jar installer.jar --installServer",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = ServerPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };
            ServerProcess.Start();
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
            return StartServer();
        }

        #endregion
        #endregion



    }
}
