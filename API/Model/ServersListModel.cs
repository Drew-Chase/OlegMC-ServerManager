using ChaseLabs.CLConfiguration.List;
using OlegMC.REST_API.Data;
using System.Collections.Generic;
using System.Linq;

namespace OlegMC.REST_API.Model
{
    public class ServersListModel
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = Data.Global.Logger;
        private readonly List<ServerModel> servers;
        public List<int> Ports { get; private set; }
        #region Singleton
        private static ServersListModel _instance;
        public static ServersListModel GetInstance
        {
            get
            {
                if (_instance == null)
                {
                    _ = new ServersListModel();
                }

                return _instance;
            }
        }
        #endregion

        private ServersListModel()
        {
            _instance = this;
            servers = new();
            Ports = new();
            Find();
        }

        /// <summary>
        /// Finds an Available port based on the current ports allocated to other servers.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public int FindAvailablePort(int port = 0)
        {
            if (Ports.Count == 0)
            {
                return 1;
            }

            int[] protectedPorts = { 22, 80, 5076 };
            port = port == 0 ? Ports[^1] + 1 : port + 1;
            return protectedPorts.ToList().Contains(port) ? FindAvailablePort(port) : port;
        }

        /// <summary>
        /// Finds exisiting servers.  Only needs to be run on api start.
        /// </summary>
        private void Find()
        {
            string[] files = System.IO.Directory.GetFiles(Global.Paths.ServersPath, "olegmc.server", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                ConfigManager config = new ConfigManager(files[i], false);
                ServerModel server = new(PlanModel.GetBasedOnName(config.GetConfigByKey("plan") == null ? "byos" : config.GetConfigByKey("plan").Value, config.GetConfigByKey("username").Value));
                ServerPropertyModel property = server.ServerProperties.GetByName("server-port");
                if (property != null)
                {
                    Ports.Add(int.Parse(property.Value));
                }
                else
                {
                    int port = FindAvailablePort();
                    server.ServerProperties.Update("server-port", port);
                    Ports.Add(port);
                }
                Add(PlanModel.GetBasedOnName(config.GetConfigByKey("plan") == null ? "byos" : config.GetConfigByKey("plan").Value, config.GetConfigByKey("username").Value));
            }
        }

        /// <summary>
        /// Adds a server based on server plan, See: <seealso cref="PlanModel" />
        /// </summary>
        /// <param name="plan"></param>
        public void Add(PlanModel plan)
        {
            servers.Add(new(plan));
        }

        /// <summary>
        /// Gets server based on username
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        public ServerModel GetServer(string username)
        {
            foreach (ServerModel server in servers)
            {
                if (server.ServerPlan.Username.Equals(username))
                {
                    return server;
                }
            }
            return null;
        }

        public void StopAllServers()
        {
            servers.ForEach(server => server.StopServer(StopMethod.Normal));
        }
    }
}
