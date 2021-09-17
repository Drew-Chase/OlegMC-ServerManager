using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Data;
using OlegMC.REST_API.Model;
using System;
using System.IO;
using static OlegMC.REST_API.Data.Global;

namespace OlegMC.REST_API.Controllers
{
    [ApiController]
    [Route("/server/")]
    public class ServerController : ControllerBase
    {
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

        #endregion Updater

        #region Themes

        [HttpPost("{username}/settings/theme")]
        public IActionResult ChangeTheme(string username, [FromForm] string theme)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }

            server.Config.GetConfigByKey("theme").Value = theme;

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

            return new JsonResult(new { theme = server.Config.GetConfigByKey("theme").Value });
        }

        [HttpPost("{username}/settings/theme/upload")]
        public IActionResult UploadTheme(IFormFile file)
        {
            return Ok();
        }

        #endregion Themes

        #region StartOnBoot

        [HttpPost("settings/startonboot")]
        public IActionResult StartOnBoot([FromForm] bool start)
        {
            if (start)
            {
                RegistryHelper.EnableStartOnBoot();
            }
            else
            {
                RegistryHelper.DisableStartOnBoot();
            }

            Logger.Debug($"Start on Boot set to {start}");
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

        #endregion StartOnBoot

        #region Logs
        [HttpGet("logs")]
        public IActionResult DownloadLogs()
        {
            return new FileStreamResult(new FileStream(Global.Functions.SafelyCreateZipFromDirectory(Directory.GetParent(Global.Paths.Logs).FullName, Path.Combine(Global.Paths.Root, "logs.zip")), FileMode.Open), "application/zip");
        }

        [HttpGet("clean-logs")]
        public IActionResult CleanLogs()
        {
            Directory.GetParent(Global.Paths.Logs).Delete(true);
            return Ok(new { message = "Logs Deleted" });
        }
        #endregion
    }
}