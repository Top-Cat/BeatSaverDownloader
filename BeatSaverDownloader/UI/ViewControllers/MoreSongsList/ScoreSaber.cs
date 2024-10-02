using System;
using BeatSaberMarkupLanguage.Components;

namespace BeatSaverDownloader.UI.ViewControllers.MoreSongsList
{
    public class ScoreSaberCustomSongCellInfo : CustomListTableData.CustomCellInfo
    {
        private readonly ScoreSaberSharp.Song _song;
        private readonly Action<CustomListTableData.CustomCellInfo> _callback;

        public ScoreSaberCustomSongCellInfo(ScoreSaberSharp.Song song, Action<CustomListTableData.CustomCellInfo> callback, string text, string subtext = null) :
            base(text, subtext)
        {
            _song = song;
            _callback = callback;
            LoadImage();
        }

        private async void LoadImage()
        {
            var image = await _song.FetchCoverImage();
            Icon = Misc.Sprites.LoadSpriteRaw(image);

            _callback(this);
        }
    }
}
