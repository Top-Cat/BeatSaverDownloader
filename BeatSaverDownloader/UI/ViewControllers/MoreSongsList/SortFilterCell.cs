using BeatSaberMarkupLanguage.Components;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers.MoreSongsList
{
    public class SortFilterCellInfo : CustomListTableData.CustomCellInfo
    {
        public readonly SortFilter SortFilter;
        public SortFilterCellInfo(SortFilter filter, string text, string subtext = null, Sprite icon = null) : base(text, subtext, icon)
        {
            SortFilter = filter;
        }
    }
}
