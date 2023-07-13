using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
using BeatSaverDownloader.Bookmarks;
using BeatSaverDownloader.Misc;

namespace BeatSaverDownloader.UI
{
    public class Settings : NotifiableSingleton<Settings>
    {
        [UIValue("syncOnLoad")]
        public bool SyncOnLoad
        {
            get => PluginConfig.SyncOnLoad;
            set
            {
                PluginConfig.SyncOnLoad = value;
                PluginConfig.SaveConfig();
                NotifyPropertyChanged();
            }
        }

        [UIValue("logoutInteractable")]
        public bool LogoutInteractable
        {
            get => PluginConfig.UserTokens?.CouldBeValid == true;
            set => NotifyPropertyChanged();
        }

        [UIAction("logout")]
        private void Logout()
        {
            PluginConfig.UserTokens = null;
            PluginConfig.SaveConfig();

            LogoutInteractable = true;
        }

        [UIValue("envChoice")]
        public string EnvChoice
        {
            get => PluginConfig.OauthEnvironment;
            set
            {
                PluginConfig.OauthEnvironment = value;
                PluginConfig.SaveConfig();
                NotifyPropertyChanged();
            }
        }

        [UIValue("envOptions")]
        public List<object> EnvOptions => OauthConfig.Options;

        public static void SetupSettings()
        {
            BSMLSettings.instance.AddSettingsMenu("BeatSaverDL", "BeatSaverDownloader.UI.BSML.settings.bsml", instance);
        }
    }
}
