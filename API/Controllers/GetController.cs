using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Model;
using System.IO;

namespace OlegMC.REST_API.Controllers
{
    /// <summary>
    /// Sends information from the server to the client.
    /// </summary>
    [ApiController]
    [Route("/Get/")]
    public class GetController : ControllerBase
    {
        [HttpGet("/status")]
        public IActionResult GetHostStatus()
        {
            return Ok(new { message = "open" });
        }

        /// <summary>
        /// Gets the basic information about the server.
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}")]
        public IActionResult GetServer(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            return new JsonResult(server.JSONObject);
        }

        /// <summary>
        /// Gets a list of all the server.properties. See: <seealso cref="ServerPropertiesModel"/>
        /// </summary>
        /// <param name="username">The Servers Username</param>
        /// <returns></returns>
        [HttpGet("{username}/server.properties")]
        public IActionResult GetServerProperties(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            server.ServerProperties.Update();
            return new JsonResult(server.ServerProperties.Properties);
        }

        /// <summary>
        /// Gets the current console output.
        /// </summary>
        /// <param name="username">The Servers Username</param>
        /// <returns>If the server is offline, Returns "Server Offline"</returns>
        [HttpGet("{username}/console")]
        public IActionResult GetConsole(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            if (server.CurrentStatus == ServerStatus.Offline)
            {
                return Ok(new { message = "Server Offline" });
            }

            return new JsonResult(server.ConsoleLog);
        }

        /// <summary>
        /// Gets a list of all backups.
        /// </summary>
        /// <param name="username">Servers Username</param>
        /// <param name="index">[Optional] Gets a specific backup file based on index.</param>
        /// <returns></returns>
        [HttpGet("{username}/backups/{index?}")]
        public IActionResult GetBackups(string username, int? index)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (index.HasValue)
            {
                return new JsonResult(BackupListModel.GetBackups(server)[index.Value - 1]);
            }

            return new JsonResult(new
            {
                enabled = server.Config.GetConfigByKey("backups_enabled").ParseBoolean(),
                max_backups = server.ServerPlan.MaxBackups,
                intervals = server.Config.GetConfigByKey("backup_intervals").ParseInt(),
                backups = BackupListModel.GetBackups(server)
            });
        }

        [HttpGet("{username}/datapack/{index?}")]
        public IActionResult GetDatapacks(string username, int? index)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (index.HasValue)
            {
                try
                {
                    return new JsonResult(DatapackListModel.GetServerInstance(server).Get(index.Value));
                }
                catch (IOException e)
                {
                    return BadRequest(new { message = $"ERROR: {e.Message}" });
                }
            }
            else
            {
                try
                {
                    return new JsonResult(DatapackListModel.GetServerInstance(server).Datapacks);
                }
                catch (IOException e)
                {
                    return BadRequest(new { message = $"ERROR: {e.Message}" });
                }
            }
        }
    }
}