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
            string[] lines = System.IO.File.ReadAllLines(PATH);
            ServerPropertyModel[] value = new ServerPropertyModel[lines.Length];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = ServerPropertyModel.DecodeFromLine(lines[i]);
            }
            return value;
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
            throw new System.IO.IOException("Server Property doesn't exist");
        }
        /// <summary>
        /// Initializes the <seealso cref="ServerPropertiesModel"/> using the server directory
        /// </summary>
        /// <param name="server_path">Server Directory</param>
        /// <returns>Server Properties Object</returns>
        public static ServerPropertiesModel Init(string server_path)
        {
            return new(System.IO.Path.Combine(server_path, "server.properties"));
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
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.CreateText(path);
            }
        }
        /// <summary>
        /// Updates an existing properties value or adds one
        /// </summary>
        /// <param name="name">Property Name</param>
        /// <param name="value">New Property Value</param>
        public void Update(string name, string value)
        {
            string current = File.ReadAllText(PATH);
            string[] lines = current.Split('\n');
            string after = string.Empty;
            bool found = false;
            foreach (string line in lines)
            {
                if (line.Split('=')[0].StartsWith(name))
                {
                    after += $"{name}={value}";
                    found = true;
                }
                else
                {
                    after += line;
                }
                after += "\n";
            }
            if (!found)
            {
                after += $"{name}={value}";
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
        #endregion
        #endregion
        #region Functions
        #region public
        /// <summary>
        /// The Constructor for the Server Property
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="value">The properties value.</param>
        public ServerPropertyModel(string name, string value)
        {
            Name = name;
            Value = value;
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
            name = name.Replace("-", " ");
            return new(name, value);
        }
        #endregion
        #endregion
    }
}