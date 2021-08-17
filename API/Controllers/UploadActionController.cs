﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Model;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Controllers
{
    /// <summary>
    /// A Controller for uploading files.
    /// </summary>
    [ApiController]
    [Route("/Action/Upload/")]
    public class UploadActionController : ControllerBase
    {

        /// <summary>
        /// Uploads the server jar file and coppies it to the server path.
        /// </summary>
        /// <param name="username">Users Username</param>
        /// <param name="file">File Object from the HTML Form</param>
        /// <returns></returns>
        [HttpPost("{username}/server"), DisableRequestSizeLimit]
        public IActionResult UploadServerJar(string username, IFormFile file)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (file.ContentType.Equals("application/java-archive") || file.ContentType.Equals("application/octet-stream"))
            {

                FileStream stream = new(Path.Combine(server.ServerPath, "start.jar"), FileMode.Create);
                file.CopyTo(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
                return Ok();
            }
            else
            {
                return BadRequest("File has to be a .jar file");
            }
        }

        /// <summary>
        /// Upload the Installer jar file.
        /// </summary>
        /// <param name="username">Users Username</param>
        /// <param name="loader">The installers modloader. See: <seealso cref="ServerType"/></param>
        /// <param name="file">File Object from the HTML Form</param>
        /// <returns></returns>
        [HttpPost("{username}/installer/{loader}"), DisableRequestSizeLimit]
        public IActionResult UploadInstallerJar(string username, string loader, IFormFile file)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            server.ServerType = (ServerType)Enum.Parse(typeof(ServerType), loader);
            if (file.ContentType.Equals("application/java-archive") || file.ContentType.Equals("application/octet-stream"))
            {
                FileStream stream = new(Path.Combine(server.ServerPath, "installer.jar"), FileMode.Create);
                file.CopyTo(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();

                server.ForceScan();
                if (server.HasInstallJar)
                {
                    if (server.InstallServer())
                    {
                        server.ForceScan();
                        if (server.HasStartJar)
                        {
                            server.StartServer();
                        }
                    }
                }
                return Ok(new { message = "Uploaded and Installed" });
            }
            else
            {
                return BadRequest("File has to be a .jar file");
            }
        }

        /// <summary>
        /// Upload a Mod to be placed in the mods folder inside the server path.
        /// </summary>
        /// <param name="username">Users Username</param>
        /// <param name="file">File Object from the HTML Form</param>
        /// <returns></returns>
        [HttpPost("{username}/mod"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadMod(string username, IFormFile file)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            Directory.CreateDirectory(Path.Combine(server.ServerPath, "mods"));
            if (file.ContentType.Equals("application/zip"))
            {
                string temp_zip = Path.Combine(server.ServerPath, "mods.zip");
                FileStream stream = new(temp_zip, FileMode.Create);
                await file.CopyToAsync(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
                string temp_dir = Path.Combine(server.ServerPath, "tmp");
                Directory.CreateDirectory(temp_dir);
                System.IO.Compression.ZipFile.ExtractToDirectory(temp_zip, temp_dir, true);
                string[] jars = Directory.GetFiles(temp_dir, "*.jar", SearchOption.TopDirectoryOnly);
                foreach (string jar in jars)
                {
                    System.IO.File.Move(jar, Path.Combine(server.ServerPath, "mods", new FileInfo(jar).Name));
                }
                if (System.IO.File.Exists(temp_zip))
                {
                    System.IO.File.Delete(temp_zip);
                }

                Directory.Delete(temp_dir, true);
            }
            else if (file.ContentType.Equals("application/java-archive") || file.ContentType.Equals("application/octet-stream"))
            {
                FileStream stream = new(Path.Combine(server.ServerPath, "mods", file.FileName), FileMode.Create);
                await file.CopyToAsync(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
            }
            else
            {
                return BadRequest($"File has to be either a .jar or a .zip containing .jars.   The current file type is {file.ContentType}");
            }
            return Ok();
        }
    }
}