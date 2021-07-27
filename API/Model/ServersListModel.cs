using OlegMC.REST_API.Data;
using System;
using System.Collections.Generic;

namespace OlegMC.REST_API.Model
{
    public class ServersListModel
    {
        private readonly List<ServerModel> servers;
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
            Find();
        }


        private void Find()
        {
            string[] files = System.IO.Directory.GetFiles(Global.ServersPath, "olegmc.server", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                Add(PlanModel.GetBasedOnName(text.Split(Environment.NewLine)[0], text.Split(Environment.NewLine)[1]));
            }
        }

        public void Add(PlanModel plan)
        {
            servers.Add(new(plan));
        }

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
    }
}
