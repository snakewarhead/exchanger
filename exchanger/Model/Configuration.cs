using exchanger.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace exchanger.Model
{
    [Serializable]
    public class Configuration
    {
        public string coin1;
        public string coin2;

        public string lowestBalance1;
        public string lowestBalance2;

        public int idxExchange;

        public string apiID;
        public string secret;

        private const string CONFIG_FILE = "config.json";

        public static Configuration Load()
        {
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                Configuration config = JsonConvert.DeserializeObject<Configuration>(configContent);

                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                {
                    Logging.LogUsefulException(e);
                }

                return new Configuration
                {
                    coin1 = "",
                    coin2 = "",
                    lowestBalance1 = "",
                    lowestBalance2 = "",
                    idxExchange = 0,
                    apiID = "",
                    secret = ""
                };
            }
        }

        public static void Save(Configuration config)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

    }
}
