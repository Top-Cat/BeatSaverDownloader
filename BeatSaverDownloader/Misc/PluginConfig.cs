using System.IO;

namespace BeatSaverDownloader.Misc
{
    public static class PluginConfig
    {
        private static readonly BS_Utils.Utilities.Config Config = new BS_Utils.Utilities.Config("BeatSaverDownloader");
        public static int MaxSimultaneousDownloads = 3;

        public static void LoadConfig()
        {
            if (!Directory.Exists("UserData"))
            {
                Directory.CreateDirectory("UserData");
            }

            Load();
        }

        private static void Load()
        {
            MaxSimultaneousDownloads = Config.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
        }

        public static void SaveConfig()
        {
            Config.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", MaxSimultaneousDownloads);
        }
    }
}