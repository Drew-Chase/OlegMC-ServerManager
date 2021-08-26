using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Data;
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
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = Data.Global.Logger;
        /// <summary>
        /// Uploads the server jar file and coppies it to the server path.
        /// </summary>
        /// <param name="username">Users Username</param>
        /// <param name="file">File Object from the HTML Form</param>
        /// <returns></returns>
        [HttpPost("{username}/server-jar"), DisableRequestSizeLimit]
        public IActionResult UploadServerJar(string username, IFormFile file)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (file.ContentType.Equals("application/java-archive") || file.ContentType.Equals("application/octet-stream"))
            {

                FileStream stream = new(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "start.jar"), FileMode.Create);
                file.CopyTo(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
                System.IO.File.Move(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "start.jar"), Path.Combine(server.ServerPath, "start.jar"), true);
                Global.Functions.DestroyUniqueTempFolder(username);
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
                FileStream stream = new(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "installer.jar"), FileMode.Create);
                file.CopyTo(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();

                System.IO.File.Move(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "installer.jar"), Path.Combine(server.ServerPath, "installer.jar"), true);

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
                Global.Functions.DestroyUniqueTempFolder(username);
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
                string temp_dir = Path.Combine(Global.Functions.GetUniqueTempFolder(username), "mods");
                Directory.CreateDirectory(temp_dir);
                System.IO.Compression.ZipFile.ExtractToDirectory(temp_zip, temp_dir, true);
                string[] jars = Directory.GetFiles(temp_dir, "*.jar", SearchOption.TopDirectoryOnly);
                foreach (string jar in jars)
                {
                    System.IO.File.Move(jar, Path.Combine(server.ServerPath, "mods", new FileInfo(jar).Name), true);
                }
                if (System.IO.File.Exists(temp_zip))
                {
                    System.IO.File.Delete(temp_zip);
                }

                Global.Functions.DestroyUniqueTempFolder(username);
            }
            else if (file.ContentType.Equals("application/java-archive") || file.ContentType.Equals("application/octet-stream"))
            {
                string temp_file = Path.Combine(Global.Functions.GetUniqueTempFolder(username), "mods", file.FileName);
                FileStream stream = new(temp_file, FileMode.Create);
                await file.CopyToAsync(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
                string mod_dir = Path.Combine(server.ServerPath, "mods");
                Directory.CreateDirectory(mod_dir);
                System.IO.File.Move(temp_file, mod_dir, true);
            }
            else
            {
                return BadRequest($"File has to be either a .jar or a .zip containing .jars.   The current file type is {file.ContentType}");
            }
            return Ok();
        }

        [HttpPost("{username}/datapack"), DisableRequestSizeLimit]
        public IActionResult UploadDatapack(string username, IFormFile file)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            string temp_path = Directory.CreateDirectory(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "datapacks")).FullName;
            if (file.ContentType == "application/zip")
            {
                string file_path = Path.Combine(temp_path, file.FileName);
                FileStream stream = new(file_path, FileMode.Create);
                file.CopyTo(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
                DatapackListModel.GetServerInstance(server).Add(file_path);
                Global.Functions.DestroyUniqueTempFolder(username);
                return Ok(new { message = "Uploaded Successfully" });
            }
            else
            {
                return BadRequest(new { message = "File needs to be a zip archive" });
            }
        }


        [HttpPost("{username}/world"), DisableRequestSizeLimit]
        public IActionResult UploadWorld(string username, IFormFile file)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            string temp_path = Directory.CreateDirectory(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "world")).FullName;
            if (file.ContentType == "application/zip")
            {
                string file_path = Path.Combine(temp_path, file.FileName);
                FileStream stream = new(file_path, FileMode.Create);
                file.CopyTo(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
                if (server.ServerProperties.GetByName("level-name") != null)
                {
                    if (Directory.Exists(Path.Combine(server.ServerPath, server.ServerProperties.GetByName("level-name").Value)))
                    {
                        Directory.Delete(Path.Combine(server.ServerPath, server.ServerProperties.GetByName("level-name").Value), true);
                    }
                }

                string unzip = Directory.CreateDirectory(Path.Combine(server.ServerPath, new FileInfo(file_path).Name.Replace(new FileInfo(file_path).Extension, "").Replace(".", ""))).FullName;
                string levelName = new DirectoryInfo(unzip).Name;
                System.IO.Compression.ZipFile.ExtractToDirectory(file_path, unzip, true);
                server.ServerProperties.Update("level-name", levelName);
                Global.Functions.DestroyUniqueTempFolder(username);
                return Ok(new { message = "Uploaded Successfully" });
            }
            else
            {
                return BadRequest(new { message = "File needs to be a zip archive" });
            }
        }


        [HttpPost("{username}/server"), DisableRequestSizeLimit]
        public IActionResult UploadServer(string username, IFormFile file)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            string temp_path = Directory.CreateDirectory(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "server")).FullName;
            if (file.ContentType == "application/zip")
            {
                string file_path = Path.Combine(temp_path, file.FileName);
                FileStream stream = new(file_path, FileMode.Create);
                file.CopyTo(stream);
                stream.Flush();
                stream.Dispose();
                stream.Close();
                int port = int.Parse(server.ServerProperties.GetByName("server-port").Value);
                System.IO.File.Move(Path.Combine(server.ServerPath, "olegmc.server"), Path.Combine(Global.Functions.GetUniqueTempFolder(username), "olegmc.server"), true);

                if (Directory.Exists(server.ServerPath))
                {
                    Directory.Delete(server.ServerPath, true);
                }

                string unzip = Directory.CreateDirectory(server.ServerPath).FullName;
                System.IO.Compression.ZipFile.ExtractToDirectory(file_path, unzip, true);

                System.IO.File.Move(Path.Combine(Global.Functions.GetUniqueTempFolder(username), "olegmc.server"), Path.Combine(Directory.CreateDirectory(server.ServerPath).FullName, "olegmc.server"), true);
                server.AcceptEULA();
                server.ServerProperties.Update("server-port", port);

                Global.Functions.DestroyUniqueTempFolder(username);
                return Ok(new { message = "Uploaded Successfully" });
            }
            else
            {
                return BadRequest(new { message = "File needs to be a zip archive" });
            }
        }
    }
}
