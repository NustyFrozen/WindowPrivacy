namespace WindowPrivacy
{
    public struct processData
    {
        public string Name;
        public string Path;
        public int pid;
        public bool hidden;
    }
    public class Config
    {

        //ms
        public bool runOnStartUp = false;
        public List<processData> whiteList;
        private static Config singleTone;

        public static Config getConfig()
        {
            if (singleTone is null)
                singleTone = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            return singleTone;
        }
        public Config(bool skip)
        {

        }
        public static void createNewConfigFile()
        {
            singleTone = new Config(true) { whiteList = new List<processData>() };
            singleTone.saveConfig();
        }
        public void addToList(processData pdata)
        {
            whiteList.Add(pdata);
            saveConfig();
        }
        public void removeFromList(processData pdata)
        {
            whiteList.RemoveAll(x => x.Path == pdata.Path);
            saveConfig();
        }
        public void saveConfig()
        {
            File.WriteAllText("config.json", Newtonsoft.Json.JsonConvert.SerializeObject(singleTone));
        }
    }
}
