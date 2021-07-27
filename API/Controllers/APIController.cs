using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Model;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Controllers
{
    [ApiController]
    [Route("/API/")]
    public class APIController : ControllerBase
    {
        [HttpGet("Create/{plan}/{username}/")]
        public IActionResult MakeServer(string username, string plan)
        {
            PlanModel planModel;
            try
            {
                planModel = PlanModel.GetBasedOnName(plan, username);
            }
            catch
            {
                return BadRequest();
            }
            ServersListModel.GetInstance.Add(planModel);
            return Ok();
        }
        [HttpGet("Get/{username}")]
        public IActionResult GetServer(string username)
        {
            return new JsonResult(ServersListModel.GetInstance.GetServer(username).JSONObject);
        }

        [HttpPost("{username}/upload/server"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadServerJar(string username)
        {
            try
            {
                ServerModel server = ServersListModel.GetInstance.GetServer(username);
                if (server == null)
                {
                    return BadRequest(new { message = $"User {username} does NOT have a server created!" });
                }

                IFormCollection formCollection = await Request.ReadFormAsync();
                IFormFile file = formCollection.Files[0];
                string folderName = server.ServerPath;
                if (file.Length > 0)
                {
                    string fileName = "start.jar";
                    string fullPath = Path.Combine(folderName, fileName);
                    using (FileStream stream = new(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    server.ForceScan();
                    return Ok(new { message = "Server \"start.jar\" Successfully Uploaded" });
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Internal server error {e}");
            }
        }

        [HttpGet("Get/{username}/server.properties")]
        public IActionResult GetServerProperties(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            return new JsonResult(server.ServerProperties.Properties);
        }
        [HttpGet("Save/{username}/server.properties/{name}/{value}")]
        public IActionResult SaveServerProperties(string username, string name, string value)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.ServerProperties.Update(name, value);
            return Ok();
        }

        [HttpGet("Action/{username}/start")]
        public IActionResult StartServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.ForceScan();
            if (server.HasStartJar)
                server.StartServer();
            else return new JsonResult(new { message = "no start.jar" });
            return Ok();
        }
        [HttpGet("Action/{username}/install/{loader}")]
        public IActionResult InstallServer(string username, string loader)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.ServerType = (ServerType)Enum.Parse(typeof(ServerType), loader);
            server.ForceScan();
            if (server.HasInstallJar)
                server.InstallServer();
            else return new JsonResult(new { message = "no installer.jar" });
            return Ok();
        }

        [HttpGet("Action/{username}/stop")]
        public IActionResult StopServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.StopServer(StopMethod.Normal);
            return Ok();
        }
        [HttpGet("Action/{username}/kill")]
        public IActionResult KillServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.StopServer(StopMethod.Kill);
            return Ok();
        }
        [HttpGet("Action/{username}/restart")]
        public IActionResult RestartServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.StopServer(StopMethod.Restart);
            return Ok();
        }
        [HttpGet("Get/{username}/console")]
        public IActionResult GetConsole(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            if (!server.IsRunning) return Ok(new { message = "Server Offline" });
            Stream stream = server.ServerProcess.StandardOutput.BaseStream;
            return new FileStreamResult(stream, "text/html");
        }

        [HttpGet("Action/{username}/console/{message}")]
        public IActionResult SendConsoleMessage(string username, string message)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.ServerProcess.StandardInput.WriteLine(message);
            return Ok();
        }

        [HttpGet("Action/{username}/Backup/{full}/{intervals?}")]
        public IActionResult CreateBackup(string username, bool full, int? intervals = 0)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            BackupModel.CreateManualBackup(server, full);
            server.Backups.CreateBackupSchedule(intervals.Value);
            return Ok();
        }

        [HttpGet("Action/{username}/ScheduleBackup/{intervals}")]
        public IActionResult CreateScheduledBackup(string username, int intervals)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.Backups.CreateBackupSchedule(intervals);
            return Ok();
        }

        [HttpGet("Action/{username}/ScheduleBackup/stop")]
        public IActionResult DeleteScheduledBackup(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null) return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            server.Backups.StopBackupSchedule();
            return Ok();
        }

    }
}
