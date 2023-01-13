using System.IO;
using BeatSaverDownloader.Bookmarks;
using BeatSaverDownloader.UI;

namespace BeatSaverDownloader.Misc
{
    public static class PluginConfig
    {
        private static readonly BS_Utils.Utilities.Config Config = new BS_Utils.Utilities.Config("BeatSaverDownloader");
        public static int MaxSimultaneousDownloads = 3;
        public static bool SyncOnLoad = true;
        public static OauthResponse UserTokens = null;
        public static string OauthEnvironment = "STAGE";

        public static void LoadConfig()
        {
            if (!Directory.Exists(IPA.Utilities.UnityGame.UserDataPath))
            {
                Directory.CreateDirectory(IPA.Utilities.UnityGame.UserDataPath);
            }

            Load();
        }

        private static void Load()
        {
            MaxSimultaneousDownloads = Config.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
            SyncOnLoad = Config.GetBool("BeatSaverDownloader", "syncOnLoad", true);
            OauthEnvironment = Config.GetString("BeatSaverDownloader", "oauthEnv", "STAGE");
            UserTokens = new OauthResponse(
                Config.GetString("OAuth", "AccessToken"),
                Config.GetString("OAuth", "TokenType"),
                Config.GetInt("OAuth", "ExpiresIn"),
                Config.GetString("OAuth", "RefreshToken")
            );
        }

        public static void SaveConfig()
        {
            Config.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", MaxSimultaneousDownloads);
            Config.SetBool("BeatSaverDownloader", "syncOnLoad", SyncOnLoad);
            Config.SetString("BeatSaverDownloader", "oauthEnv", OauthEnvironment);

            Config.SetString("OAuth", "AccessToken", UserTokens?.AccessToken ?? "");
            Config.SetString("OAuth", "TokenType", UserTokens?.TokenType ?? "");
            Config.SetInt("OAuth", "ExpiresIn", UserTokens?.ExpiresIn ?? 0);
            Config.SetString("OAuth", "RefreshToken", UserTokens?.RefreshToken ?? "");

            Settings.instance.LogoutInteractable = true;
        }
    }
}
