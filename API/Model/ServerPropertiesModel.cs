using System.Collections.Generic;
using System.IO;

namespace OlegMC.REST_API.Model
{
    /// <summary>
    /// The server.properites file for the server.
    /// </summary>
    public class ServerPropertiesModel
    {

        #region Variables
        #region public
        /// <summary>
        /// The path to the server.properties
        /// </summary>
        public string PATH { get; private set; }
        /// <summary>
        /// A List of all server properties
        /// </summary>
        public ServerPropertyModel[] Properties => Make();
        /// <summary>
        /// Creates a <seealso cref="ServerPropertyModel"/> array for all the server properties in the server.properties
        /// </summary>
        /// <returns>All server properties</returns>
        private ServerPropertyModel[] Make()
        {
            List<ServerPropertyModel> value = new();
            try
            {
                string[] lines = File.ReadAllLines(PATH);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("#") || lines[i] == "")
                    {
                        continue;
                    }

                    try
                    {
                        value.Add(ServerPropertyModel.DecodeFromLine(lines[i]));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
            }
            return value.ToArray();
        }
        /// <summary>
        /// Gets the server property based on the property name.
        /// </summary>
        /// <param name="name">Property Name</param>
        /// <returns>The <seealso cref="ServerPropertyModel"/> from the name</returns>
        public ServerPropertyModel GetByName(string name)
        {
            ServerPropertyModel[] properties = Properties;
            foreach (ServerPropertyModel property in properties)
            {
                if (property.Name.ToLower().Equals(name.ToLower()))
                {
                    return property;
                }
            }
            return null;
        }
        /// <summary>
        /// Initializes the <seealso cref="ServerPropertiesModel"/> using the server directory
        /// </summary>
        /// <param name="server_path">Server Directory</param>
        /// <returns>Server Properties Object</returns>
        public static ServerPropertiesModel Init(string server_path)
        {
            return new(Path.Combine(server_path, "server.properties"));
        }
        #endregion
        #endregion

        /// <summary>
        /// Private Constructor for the <seealso cref="ServerPropertiesModel"/>
        /// </summary>
        /// <param name="path">server.properties path</param>
        private ServerPropertiesModel(string path)
        {
            PATH = path;
            int port = ServersListModel.GetInstance.FindAvailablePort();
            ServersListModel.GetInstance.Ports.Add(port);
            if (!File.Exists(path))
            {
                StreamWriter writer = null;
                try
                {
                    writer = File.CreateText(path);
                    // Sets the default properties
                    writer.WriteLine($"server-port={port}");
                    writer.WriteLine("max-players=20");
                }
                catch (IOException)
                {

                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Flush();
                        writer.Dispose();
                        writer.Close();
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Update()
        {
            Update(string.Empty, string.Empty);
        }

        public void Remove(string name)
        {
            Update(name, string.Empty, true);
        }

        /// <summary>
        /// Updates an existing properties value or adds one
        /// </summary>
        /// <param name="name">Property Name</param>
        /// <param name="value">New Property Value</param>
        public void Update(string name, object value, bool remove = false)
        {
            string after = string.Empty;
            bool found = string.IsNullOrWhiteSpace(name);

            foreach (ServerPropertyModel property in Properties)
            {
                if (!found && (GetByName(name) == null || name == property.Name))
                {
                    if (!remove)
                        after += $"{name}={value}\n";
                    found = true;
                }
                else
                {
                    after += $"{property.Name}={property.Value}\n";
                }
            }
            if (!found)
            {
                after += $"{name}={value}\n";
            }

            File.WriteAllText(PATH, after);
        }
    }
    /// <summary>
    /// Server Property Object
    /// </summary>
    public class ServerPropertyModel
    {
        #region Variables
        #region public
        /// <summary>
        /// Name of the property.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The properties value.
        /// </summary>
        public string Value { get; private set; }
        /// <summary>
        /// If the property can be changed.
        /// </summary>
        public bool Protected { get; private set; }
        #endregion
        #endregion
        #region Functions
        #region public
        /// <summary>
        /// The Constructor for the Server Property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="value">The properties value.</param>
        public ServerPropertyModel(string name, string value, bool isProtected = false)
        {
            Name = name;
            Value = value;
            Protected = isProtected;
        }
        /// <summary>
        /// Creates a <seealso cref="ServerPropertyModel"/> based on a line from the server.properties file
        /// </summary>
        /// <param name="line">line from the server.properties file.</param>
        /// <returns>A <seealso cref="ServerPropertyModel"/> based on the line provided.</returns>
        public static ServerPropertyModel DecodeFromLine(string line)
        {
            string name = line.Split('=')[0].Replace("=", "");
            string value = line.Split('=')[1].Replace("=", "");
            return new(name, value, (name.Equals("server-port") || name.Equals("server-ip")));
        }
        public override bool Equals(object obj)
        {
            if (!obj.GetType().Equals(typeof(ServerPropertyModel)))
            {
                return false;
            }

            ServerPropertyModel model = (ServerPropertyModel)obj;
            if (model.Name.ToLower().Equals(Name.ToLower()))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
        #endregion
    }
}