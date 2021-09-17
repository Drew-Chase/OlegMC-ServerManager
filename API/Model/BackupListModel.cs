using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static OlegMC.REST_API.Data.Global;

namespace OlegMC.REST_API.Model
{
    public class BackupListModel
    {
        public System.Timers.Timer BackupTimer { get; private set; }

        public ServerModel Server { get; private set; }
        public bool IsFullBackup { get; set; }

        public int NumberOfBackups { get; private set; }

        public List<BackupModel> Backups { get; private set; }

        public BackupListModel(ServerModel server, bool isFullBackup = true, int backupEvery = 0)
        {
            Server = server;
            IsFullBackup = isFullBackup;
            if (backupEvery != 0)
            {
                CreateBackupSchedule(backupEvery);
            }

            Backups = new();

            string path = Path.Combine(Paths.BackupPath, server.ServerPlan.Username);
            Directory.CreateDirectory(path);
            UpdateBackups(path, server);
        }

        public static BackupModel[] GetBackups(ServerModel server)
        {
            string path = Path.Combine(Paths.BackupPath, server.ServerPlan.Username);
            string[] dirNames = Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly);

            dirNames = dirNames.OrderBy(o => new FileInfo(o).CreationTime).Reverse().ToArray();

            BackupModel[] backups = new BackupModel[dirNames.Length];
            for (int i = 0; i < dirNames.Length; i++)
            {
                backups[i] = new(i, new FileInfo(dirNames[i]).Name, dirNames[i], new FileInfo(dirNames[i]).CreationTime);
            }
            //backups = backups.OrderBy(o => o.Creation).ToArray();
            return backups;
        }

        public static void CreateManualBackupAsync(ServerModel server, bool full = true)
        {
            ServerStatus tmpStatus = server.CurrentStatus;
            Logger.Debug($"Backing Up {server.ServerPlan.Username}'s server");
            if (server.IsRunning)
            {
                DateTime start = DateTime.Now;
                Logger.Debug("Doing Save Game Check");
                server.ServerProcess.StandardInput.WriteLine("say Creating Backup");
                server.ServerProcess.StandardInput.WriteLine("save-off");
                server.ServerProcess.StandardInput.WriteLine("save-all");
                server.ServerProcess.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        if ((bool)e.Data?.Contains("Saved the game"))
                        {
                            Task.Run(() => CreateManualBackup(server, full));
                        }
                    }
                };
                Logger.Debug("Save Game Check Completed");
            }
            else
            {
                Task.Run(() => CreateManualBackup(server, full));
            }
        }

        private static void CreateManualBackup(ServerModel server, bool full = true)
        {
            server.CurrentStatus = ServerStatus.BackingUp;
            string backup_folder = Path.Combine(Paths.BackupPath, server.ServerPlan.Username);
            server.Backups.UpdateBackups(backup_folder, server);
            Directory.CreateDirectory(backup_folder);
            string oldestFile = string.Empty;
            try
            {
                oldestFile = new DirectoryInfo(backup_folder).GetFileSystemInfos("*.zip", SearchOption.TopDirectoryOnly).OrderBy(fi => fi.CreationTime).First().FullName;
                if (server.Backups.NumberOfBackups + 1 > server.ServerPlan.MaxBackups)
                {
                    File.Delete(oldestFile);
                }
            }
            catch
            {
            }

            string backup_path = Path.Combine(backup_folder, $"{DateTime.Now:HH-mm-ss (MM-dd-yyyy)}.zip");
            if (full)
            {
                Logger.Debug("Creating Zip File");
                Functions.SafelyCreateZipFromDirectory(server.ServerPath, backup_path);
            }

            server.Backups.UpdateBackups(backup_folder, server);
            server.CurrentStatus = server.PreviousStatus;

            if (server.IsRunning)
            {
                server.ServerProcess.StandardInput.WriteLine("save-on");
                server.ServerProcess.StandardInput.WriteLine("say Backup Completed");
            }
        }

        private void UpdateBackups(string _path, ServerModel _server)
        {
            NumberOfBackups = Directory.GetFiles(_path, "*.zip", SearchOption.TopDirectoryOnly).Length;
            if (NumberOfBackups > _server.ServerPlan.MaxBackups)
            {
                for (int i = 0; i < (NumberOfBackups - _server.ServerPlan.MaxBackups); i++)
                {
                    File.Delete(new DirectoryInfo(_path).GetFileSystemInfos("*.zip", SearchOption.TopDirectoryOnly).OrderBy(fi => fi.CreationTime).First().FullName);
                }
            }
        }

        public void CreateBackupSchedule(int minutes)
        {
            if (BackupTimer != null)
            {
                BackupTimer.Stop();
            }

            BackupTimer = new((minutes * 1000) * 60);
            BackupTimer.Elapsed += (s, e) =>
            {
                if (Server.CurrentStatus == ServerStatus.Online)
                {
                    CreateManualBackup(Server);
                }
            };
            BackupTimer.AutoReset = true;
            BackupTimer.Start();
        }

        public void StopBackupSchedule()
        {
            if (BackupTimer == null)
            {
                return;
            }

            BackupTimer.Stop();
            BackupTimer = null;
        }
    }

    public class BackupModel
    {
        public int ID { get; private set; }
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public DateTime Creation { get; private set; }

        public BackupModel(int id, string fileName, string filePath, DateTime creation)
        {
            ID = id;
            FileName = fileName;
            FilePath = filePath;
            Creation = creation;
        }
    }
}