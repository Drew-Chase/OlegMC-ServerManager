using System;
using System.IO;

namespace OlegMC.REST_API.Data
{
    public static class Global
    {
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
}
