using OlegMC.REST_API.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Model
{
    public class BackupModel
    {
        public System.Timers.Timer BackupTimer { get; private set; }

        public ServerModel Server { get; private set; }
        public bool IsFullBackup { get; set; }

        public int NumberOfBackups { get; private set; }

        public BackupModel(ServerModel server, bool isFullBackup = true, int backupEvery = 0)
        {
            Server = server;
            IsFullBackup = isFullBackup;
            if (backupEvery != 0)
                CreateBackupSchedule(backupEvery);
        }

        public static void CreateManualBackup(ServerModel server, bool full)
        {
            if (!server.IsRunning) return;
            string backup_folder = Path.Combine(Global.BackupPath, server.ServerPlan.Username);
            Directory.CreateDirectory(backup_folder);
            string oldestFile = string.Empty;
            try
            {
                oldestFile = new DirectoryInfo(backup_folder).GetFileSystemInfos("*.zip", SearchOption.TopDirectoryOnly).OrderBy(fi => fi.CreationTime).First().FullName;
                if (server.Backups.NumberOfBackups + 1 > server.ServerPlan.MaxBackups) File.Delete(oldestFile);
            }
            catch
            {

            }

            DateTime now = DateTime.Now;
            string backup_file = $"{now:HH-mm-ss (MM-dd-yyyy)}.zip";
            string backup_path = Path.Combine(backup_folder, backup_file);
            if (full)
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(server.ServerPath, backup_path);
            }

            server.Backups.NumberOfBackups += 1;

        }

        private void CreateBackup(bool full, int intervals_in_minutes = 0)
        {
            if (intervals_in_minutes != 0)
            {
                CreateBackupSchedule(intervals_in_minutes);
            }
            CreateManualBackup(Server, full);
        }

        public void CreateBackupSchedule(int minutes)
        {
            BackupTimer = new((minutes * 1000) * 60);
            BackupTimer.Elapsed += (s, e) =>
            {
                CreateBackup(true);
            };
            BackupTimer.AutoReset = true;
            BackupTimer.Start();
        }

        public void StopBackupSchedule()
        {
            if (BackupTimer == null) return;
            BackupTimer.Stop();
            BackupTimer = null;
        }
    }
}
