using BeatSaverDownloader.Misc;

namespace BeatSaverDownloader.UI.ViewControllers.MoreSongsList
{
    public class SortFilter
    {
        public readonly Filters.FilterMode Mode;
        public readonly Filters.BeatSaverFilterOptions BeatSaverOption;
        public readonly Filters.ScoreSaberFilterOptions ScoreSaberOption;

        public SortFilter(Filters.FilterMode mode, Filters.BeatSaverFilterOptions beatSaverOption = default, Filters.ScoreSaberFilterOptions scoreSaberOption = default)
        {
            Mode = mode;
            BeatSaverOption = beatSaverOption;
            ScoreSaberOption = scoreSaberOption;
        }
    }
}
