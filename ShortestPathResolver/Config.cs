using Newtonsoft.Json;
using System.IO;

namespace ShortestPathResolver
{
    public class Config
    {
        ConfigJsonItems Items = new ConfigJsonItems();
        public Config()
        {
            using (StreamReader r = new StreamReader("config.json"))
            {
                string Json = r.ReadToEnd();
                ConfigJsonItems items = JsonConvert.DeserializeObject<ConfigJsonItems>(Json);
                this.Items = items;
            }
        }
        public int GetVertices()
        {
            return this.Items.Vertices;
        }
        public int GetClients()
        { 
            return this.Items.Clients;
        }
    }
}
