using BeatSaberMarkupLanguage.Components;
using BeatSaverDownloader.Misc;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers.MoreSongsList
{
    public class SourceCellInfo : CustomListTableData.CustomCellInfo
    {
        public readonly Filters.FilterMode Filter;
        public SourceCellInfo(Filters.FilterMode filter, string text, string subtext = null, Sprite icon = null) : base(text, subtext, icon)
        {
            Filter = filter;
        }
    }
}
