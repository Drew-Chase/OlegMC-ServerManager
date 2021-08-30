using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Model;

namespace OlegMC.REST_API.Controllers
{
    /// <summary>
    /// The controller for setting values from the client to the server.
    /// </summary>
    [ApiController]
    [Route("/Action/Post/")]
    public class PostActionController : ControllerBase
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = Data.Global.Logger;
        /// <summary>
        /// Creates a server for a user.
        /// </summary>
        /// <param name="username">The Username for the server</param>
        /// <param name="plan">The Server Plan. See: <seealso cref="PlanModel"/></param>
        /// <returns></returns>
        [HttpGet("{username}/{plan}")]
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
            ServersListModel.GetInstance.Add(new(planModel));
            return Ok();
        }
        /// <summary>
        /// Saves Server Properties value. See: <seealso cref="ServerPropertiesModel"/>
        /// </summary>
        /// <param name="username">The Servers Username</param>
        /// <param name="name">server property name. See: <seealso cref="ServerPropertyModel"/></param>
        /// <param name="value">server property value. See: <seealso cref="ServerPropertyModel"/></param>
        /// <returns></returns>
        [HttpGet("{username}/server.properties/{name}/{value}")]
        public IActionResult SaveServerProperties(string username, string name, string value)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (server.ServerProperties.GetByName(name) != null)
            {
                if (!server.ServerPlan.Name.ToLower().Equals("byos")&&server.ServerProperties.GetByName(name).Protected)
                {
                    return BadRequest(new { message = $"\"{name}\" is a protected property, thus can NOT be removed and/or modified?  Are you trying to do something bad?" });
                }
                server.ServerProperties.Update(name, value);
                return Ok(new { message = "updated" });
            }
            else if (server.ServerProperties.GetByName(name) == null)
            {
                return BadRequest(new { message = $"\"{name}\" server property does NOT exist?  Did you miss type?" });
            }
            else
            {
                return BadRequest(new { message = $"Something went wrong while trying to update \"{name}\"! Please try again later." });
            }
        }
        [HttpPost("java_version")]
        public IActionResult SwitchJavaVersion([FromForm] string username, [FromForm] int version)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            server.Java_Version = version;
            return Ok(new { version = server.Java_Version });
        }

        [HttpPost("ram")]
        public IActionResult SetMaxRam([FromForm] string username, [FromForm] int ram)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (server.ServerPlan.Name == "BYOS")
            {
                server.Max_Ram = ram;
                return Ok(new { message = $"Ram set to {ram}" });
            }
            else
            {
                return BadRequest(new { message = $"{server.ServerPlan.Name} does NOT allow for max ram modification!" });
            }
        }

        [HttpPost("max_backups")]
        public IActionResult SetMaxBackups([FromForm] string username, [FromForm] int backups)
        {
            ServerModel server = ServersListModel.GetInstance.GetServer(username);
            if (server == null)
            {
                return BadRequest(new { message = $"User {username} does NOT have a server created!" });
            }
            if (server.ServerPlan.Name.ToLower().Equals("byos"))
            {
                server.ServerPlan.MaxBackups = backups;
                server.config.GetConfigByKey("max_backups").Value = backups.ToString();
                return Ok(new { message = $"Max Backups set to {backups}" });
            }
            else
            {
                return BadRequest(new { message = $"{server.ServerPlan.Name} does NOT allow for max ram modification!" });
            }
        }
    }
}
