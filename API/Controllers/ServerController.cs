using ChaseLabs.CLLogger.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Data;
using OlegMC.REST_API.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Controllers
{
    [ApiController]
    [Route("/server/")]
    public class ServerController : ControllerBase
    {
        private static ILog log = Global.Logger;
        [HttpGet]
        public IActionResult Index()
        {
            return new JsonResult(new
            {
                OS = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "osx" : "unknown",
                StartOnBoot = RegistryHelper.ShouldStartOnBoot(),
            });
        }
        #region Updater
        [HttpGet("check-for-updates")]
        public IActionResult CheckForUpdates()
        {
            return new JsonResult(new
            {
                needsUpdate = UpdateManager.CheckForUpdates()
            });
        }
        [HttpPost("update/{force?}")]
        public IActionResult Update([FromForm] bool? force)
        {
            UpdateManager.Update(force.HasValue && force.Value);
            return Ok(new { message = "If an update is avaliable, the system will update." });
        }
        #endregion
        #region Themes

        [HttpPost("{username}/settings/theme")]
        public IActionResult ChangeTheme(string username, [FromForm] string theme)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.config.GetConfigByKey("theme").Value = theme;

            return Ok(new { message = $"Saved theme as {theme}" });
        }
        [HttpGet("{username}/settings/theme/")]
        public IActionResult GetTheme(string username)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            return new JsonResult(new { theme = server.config.GetConfigByKey("theme").Value });
        }

        [HttpPost("{username}/settings/theme/upload")]
        public IActionResult UploadTheme(IFormFile file)
        {
            return Ok();
        }
        #endregion
        #region StartOnBoot
        [HttpPost("settings/startonboot")]
        public IActionResult StartOnBoot([FromForm] bool start)
        {
            if (start)
                RegistryHelper.EnableStartOnBoot();
            else
                RegistryHelper.DisableStartOnBoot();
            log.Debug($"Start on Boot set to {start}");
            return Ok(new { message = $"Start on Boot set to {start}" });
        }
        [HttpGet("settings/startonboot")]
        public IActionResult GetStartOnBoot()
        {
            return new JsonResult(new
            {
                StartOnBoot = RegistryHelper.ShouldStartOnBoot()
            });
        }
        #endregion
        [HttpGet("logs")]
        public IActionResult DownloadLogs() => new FileStreamResult(new FileStream(Global.Functions.SafelyCreateZipFromDirectory(Directory.GetParent(Global.Paths.Logs).FullName, Path.Combine(Global.Paths.Root, "logs.zip")), FileMode.Open), "application/zip");
        [HttpGet("clean-logs")]
        public IActionResult CleanLogs()
        {
            Directory.GetParent(Global.Paths.Logs).Delete(true);
            return Ok(new { message = "Logs Deleted" });
        }
    }
}
