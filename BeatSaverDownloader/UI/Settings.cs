using BeatSaberMarkupLanguage.Attributes;

namespace BeatSaverDownloader.UI
{
    public class Settings : PersistentSingleton<Settings>
    {
        [UIValue("toggle1")]
        public bool toggle1
        {
            get { /*fetch value*/ return true; }
            set { /*Update value*/ }
        }

        public static void SetupSettings()
        {
      //      BSMLSettings.instance.AddSettingsMenu("BeatSaverDownloader", "BeatSaverDownloader.UI.BSML.settings.bsml", instance);
        }
    }
}