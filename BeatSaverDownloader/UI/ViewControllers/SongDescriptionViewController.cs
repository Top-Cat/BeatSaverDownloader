using BeatSaberMarkupLanguage.Attributes;
using HMUI;
namespace BeatSaverDownloader.UI.ViewControllers
{
    public class SongDescriptionViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.songDescription.bsml";

        [UIComponent("songDescription")]
        private TextPageScrollView _songDescription;

        internal void ClearData()
        {
            if (_songDescription)
                _songDescription.SetText("");
        }

        internal void Initialize(string description)
        {
            _songDescription.SetText(description);
        }
    }
}