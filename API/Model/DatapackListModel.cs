using System.IO;

namespace OlegMC.REST_API.Model
{
    public class DatapackListModel
    {
        public DatapackModel[] Datapacks { get; private set; }
        private readonly ServerModel server;

        public static DatapackListModel GetServerInstance(ServerModel server)
        {
            return new DatapackListModel(server, GetList(server));
        }

        private static DatapackModel[] GetList(ServerModel server)
        {
            string datapack_dir = Path.Combine(server.ServerPath, server.ServerProperties.GetByName("level-name").Value, "datapacks");
            if (!Directory.Exists(datapack_dir))
            {
                throw new IOException("Server doesn't have the ability to use datapacks");
            }

            string[] files = Directory.GetFiles(Path.Combine(server.ServerPath, server.ServerProperties.GetByName("level-name").Value, "datapacks"));
            DatapackModel[] datapacks = new DatapackModel[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                datapacks[i] = new(i, files[i]);
            }
            return datapacks;
        }

        public void Add(string _temp_path)
        {
            File.Move(_temp_path, Path.Combine(server.ServerPath, server.ServerProperties.GetByName("level-name").Value, "datapacks", new FileInfo(_temp_path).Name), true);
            Datapacks = GetList(server);
        }

        public void Remove(int? id)
        {
            if (id.HasValue)
            {
                foreach (DatapackModel pack in Datapacks)
                {
                    if (pack.ID == id.Value - 1)
                    {
                        if (File.Exists(pack.Path))
                        {
                            File.Delete(pack.Path);
                        }

                        Datapacks = GetList(server);
                        return;
                    }
                }
            }
            else
            {
                foreach (DatapackModel pack in Datapacks)
                {
                    File.Delete(pack.Path);
                    Datapacks = GetList(server);
                }
            }
        }

        public DatapackModel Get(int id)
        {
            foreach (DatapackModel pack in Datapacks)
            {
                if (pack.ID == id)
                {
                    return pack;
                }
            }
            throw new IOException("Datapack doesn't exist");
        }

        private DatapackListModel(ServerModel _server, DatapackModel[] _packs)
        {
            Datapacks = _packs;
            server = _server;
        }
    }

    public class DatapackModel
    {
        public int ID { get; private set; }
        public string Path { get; private set; }
        public string FileName { get; private set; }

        public DatapackModel(int _id, string _path)
        {
            ID = _id;
            Path = _path;
            FileName = new FileInfo(_path).Name;
        }
    }
}