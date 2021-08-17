using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OlegMC.REST_API.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Data
{
    [Obsolete("Class could not be run efficiently.  Version handling will be client side.")]
    public static class Versions
    {
        public static Version[] Make(ServerType loader)
        {
            List<Version> value = new();

            using (var client = new System.Net.WebClient())
            {
                switch (loader)
                {
                    case ServerType.Vanilla:
                        Task.Run(() =>
                        {
                            JObject manifest = (JObject)JsonConvert.DeserializeObject(client.DownloadString("https://launchermeta.mojang.com/mc/game/version_manifest.json"));
                            var versions = (JArray)manifest["versions"];
                            int chunkSize = (int)Math.Round((double)versions.Count / 500);
                            chunkSize = (chunkSize == 0 || chunkSize == 1) ? 2 : chunkSize;
                            var chunks = GetChunk(versions, chunkSize);
                            value.AddRange(RunChunksAsync(chunks, loader).Result);
                        }).Wait();
                        //Task.Run(() =>
                        //{

                        //    foreach (JObject v in versions)
                        //    {
                        //        Task.Run(() =>
                        //        {
                        //            string id = v["id"].ToString();
                        //            var json = ((JObject)JsonConvert.DeserializeObject(client.DownloadString(v["url"].ToString())));
                        //            var server = json["downloads"]["server"];
                        //            if (server != null)
                        //            {
                        //                string url = server["url"].ToString();
                        //                Console.WriteLine(id); // TODO: REMOVE
                        //                value.Add(new(id, url, loader));
                        //            }
                        //        });
                        //    }
                        //}).Wait();
                        break;
                }

                client.Dispose();
            }
            return value.ToArray();
        }
        public static Version GetByID(string _id, ServerType loader)
        {
            Version[] versions = Make(loader);
            foreach (Version version in versions)
            {
                if (version.Equals(new Version(_id, version.URL, loader)))
                {
                    return version;
                }
            }
            return null;
        }

        private static Task<Version[]> RunChunksAsync(JArray[] json, ServerType loader)
        {
            return Task.Run(() =>
           {
               List<Version> v = new();
               foreach (JArray ja in json)
               {
                   foreach (JObject obj in ja)
                   {
                       Version version = RunChunk(obj, loader);
                       if (version != null)
                           v.Add(version);
                   }
               }
               return v.ToArray();
           });
        }

        private static Version RunChunk(JObject v, ServerType loader)
        {
            using var client = new System.Net.WebClient();
            string id = v["id"].ToString();
            var json = ((JObject)JsonConvert.DeserializeObject(client.DownloadString(v["url"].ToString())));
            var server = json["downloads"]["server"];
            if (server != null)
            {
                string url = server["url"].ToString();
                Console.WriteLine(id); // TODO: REMOVE
                return new(id, url, loader);
            }

            return null;

        }

        private static JArray[] GetChunk(JArray fullArray, int size)
        {
            List<JArray> range = new();
            for (int i = 0; i < fullArray.Count; i += size)
            {
                try
                {
                    range.Add(JArray.FromObject(fullArray.ToList().GetRange(i, Math.Min(size, fullArray.Count - i)).ToArray()));
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }
                catch
                {
                    continue;
                }
            }
            return range.ToArray();
        }
    }

    public class Version
    {
        public string ID { get; private set; }
        public string URL { get; private set; }
        public ServerType Loader { get; private set; }
        public Version(string _id, string _url, ServerType _loader)
        {
            ID = _id;
            URL = _url;
            Loader = _loader;
        }
        public void Download(string _path)
        {
            using var client = new System.Net.WebClient();
            client.DownloadFile(URL, _path);
            client.Dispose();
        }
        public override bool Equals(object obj)
        {
            if (!obj.GetType().Equals(typeof(Version))) return false;
            Version version = (Version)obj;
            return version.ID == ID && version.Loader == Loader;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
