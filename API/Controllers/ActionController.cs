using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Data;
using OlegMC.REST_API.Model;
using System;

namespace OlegMC.REST_API.Controllers
{
    /// <summary>
    /// For completing actions on the server.
    /// </summary>
    [ApiController]
    [Route("/Action/")]
    public class ActionController : ControllerBase
    {
        /// <summary>
        /// Installs a server based on a specific loader, See: <seealso cref="ServerType"/>
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <param name="loader">The servers modloader, See: <seealso cref="ServerType"/></param>
        /// <returns></returns>
        [HttpGet("{username}/install/{loader}")]
        public IActionResult InstallServer(string username, string loader)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.ServerType = (ServerType)Enum.Parse(typeof(ServerType), loader);
            server.ForceScan();
            if (server.HasInstallJar)
            {
                server.InstallServer();
            }
            else
            {
                return new JsonResult(new { message = "no installer.jar" });
            }

            return Ok();
        }
        /// <summary>
        /// Starts the server
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}/start")]
        public IActionResult StartServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.ForceScan();
            if (server.HasStartJar)
            {
                server.StartServer();
            }
            else
            {
                return new JsonResult(new { message = "no start.jar" });
            }

            return Ok(new { message = "Server is starting correctly" });
        }

        /// <summary>
        /// Stops the server normally
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}/stop")]
        public IActionResult StopServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.StopServer(StopMethod.Normal);
            return Ok();
        }
        /// <summary>
        /// Kills the servers process
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}/kill")]
        public IActionResult KillServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.StopServer(StopMethod.Kill);
            return Ok();
        }
        /// <summary>
        /// Stops the server normally, then restarts it.
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}/restart")]
        public IActionResult RestartServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.StopServer(StopMethod.Restart);
            return Ok();
        }
        /// <summary>
        /// Stops the server normally, then backs it up.
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}/stop-backup")]
        public IActionResult StopAndBackupServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            if (server.StopServer(StopMethod.Normal))
                BackupListModel.CreateManualBackup(server);
            return Ok();
        }

        /// <summary>
        /// Sends a message to the console
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <param name="message">Message to be sent</param>
        /// <returns></returns>
        [HttpGet("{username}/console/{message}")]
        public IActionResult SendConsoleMessage(string username, string message)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.ServerProcess.StandardInput.WriteLine(message);
            return Ok();
        }

        /// <summary>
        /// Backs up the server.
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <param name="full">if the server should do a complete backup or a partial.</param>
        /// <param name="intervals">[Optional] if provided, server will backup every <b>X</b> minutes</param>
        /// <returns></returns>
        [HttpGet("{username}/Backup/{full}/{intervals?}")]
        public IActionResult CreateBackup(string username, bool full, int? intervals)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            BackupListModel.CreateManualBackup(server, full);
            if (intervals.HasValue)
                server.Backups.CreateBackupSchedule(intervals.Value);
            return Ok(new { message = "Backup Created!" });
        }
        /// <summary>
        /// Schedules a backup for every <b>X</b> minutes
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <param name="intervals">Intervals in minutes</param>
        /// <returns></returns>
        [HttpGet("{username}/ScheduleBackup/{intervals}")]
        public IActionResult CreateScheduledBackup(string username, int intervals)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.Backups.CreateBackupSchedule(intervals);
            server.config.GetConfigByKey("backup_intervals").Value = intervals.ToString();
            server.config.GetConfigByKey("backups_enabled").Value = true.ToString();
            return Ok(new { message = $"backup scheduled for every {intervals} minutes" });
        }
        /// <summary>
        /// Stops the backup schedule
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}/ScheduleBackup/stop")]
        public IActionResult DeleteScheduledBackup(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.Backups.StopBackupSchedule();

            server.config.GetConfigByKey("backup_intervals").Value = 0.ToString();
            server.config.GetConfigByKey("backups_enabled").Value = false.ToString();
            return Ok(new { message = "Backup Schedule Canceled" });
        }
        /// <summary>
        /// Downloads a server installer for specific version and loader
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <param name="loader">modloader, See: <seealso cref="ServerType"/></param>
        /// <param name="version">Minecraft Version.</param>
        /// <returns></returns>
        [HttpGet("{username}/download/{loader}/{version}")]
        public IActionResult DownloadServer(string username, string loader, string version)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.ServerType = (ServerType)Enum.Parse(typeof(ServerType), loader);
            try
            {

                server.DownloadServer(version);
            }
            catch
            {
                return BadRequest(new { message = $"Unable to download {loader} server version {version}" });
            }

            return Ok(new { message = "Server Downloaded" });
        }

        /// <summary>
        /// Removes server property. See: <seealso cref="ServerPropertiesModel"/>
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <param name="name">Property Name, See: <seealso cref="ServerPropertyModel"/></param>
        /// <returns></returns>
        [HttpGet("{username}/server.properties/remove/{name}")]
        public IActionResult RemoveServerProperty(string username, string name)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (server.ServerProperties.GetByName(name) != null && !server.ServerProperties.GetByName(name).Protected)
            {
                server.ServerProperties.Remove(name);
                return Ok(new { message = "Deleted!" });
            }
            else if (server.ServerProperties.GetByName(name) == null)
            {
                return BadRequest(new { message = $"\"{name}\" server property does NOT exist?  Did you miss type?" });
            }
            else if (server.ServerProperties.GetByName(name).Protected)
            {
                return BadRequest(new { message = $"\"{name}\" is a protected property, thus can NOT be removed and/or modified?  Are you trying to do something bad?" });
            }
            else
            {
                return BadRequest(new { message = $"Something went wrong while trying to update \"{name}\"! Please try again later." });
            }
        }

        [HttpGet("{username}/download/world")]
        public IActionResult DownloadWorld(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            string levelName = server.ServerProperties.GetByName("level-name").Value;
            levelName = string.IsNullOrWhiteSpace(levelName) ? "world" : levelName;
            string zipArchive = System.IO.Path.Combine(Global.GetUniqueTempFolder(username), "download-world.zip");
            if (System.IO.File.Exists(zipArchive)) System.IO.File.Delete(zipArchive);
            System.IO.Compression.ZipFile.CreateFromDirectory(System.IO.Path.Combine(server.ServerPath, levelName), zipArchive);
            return new FileStreamResult(new System.IO.FileStream(zipArchive, System.IO.FileMode.Open), "application/zip");
        }
        [HttpGet("{username}/download/server")]
        public IActionResult DownloadServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            string zipArchive = System.IO.Path.Combine(Global.GetUniqueTempFolder(username), "download-server.zip");
            if (System.IO.File.Exists(zipArchive)) System.IO.File.Delete(zipArchive);
            System.IO.Compression.ZipFile.CreateFromDirectory(server.ServerPath, zipArchive);
            return new FileStreamResult(new System.IO.FileStream(zipArchive, System.IO.FileMode.Open), "application/zip");
        }

        [HttpGet("{username}/download/backup/{index}")]
        public IActionResult DownloadBackup(string username, int index)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            return new FileStreamResult(new System.IO.FileStream(BackupListModel.GetBackups(server)[index - 1].FilePath, System.IO.FileMode.Open), "application/zip");
        }
        [HttpGet("{username}/backup/remove/{index?}")]
        public IActionResult RemoveBackup(string username, int? index)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (index.HasValue)
                System.IO.File.Delete(BackupListModel.GetBackups(server)[index.Value - 1].FilePath);
            else
            {
                foreach (var backup in BackupListModel.GetBackups(server))
                {
                    System.IO.File.Delete(backup.FilePath);
                }
            }
            return Ok(new { message = "Removed Successfully!" });
        }
    }
}
