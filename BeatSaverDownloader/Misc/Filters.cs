namespace BeatSaverDownloader.Misc
{
    public static class Filters
    {
        public enum FilterMode { Search, BeatSaver, ScoreSaber, BeastSaber }
        public enum BeatSaverFilterOptions { Latest, Hot, Rating, Downloads, Plays, Uploader }
        public enum ScoreSaberFilterOptions { Trending, Ranked, Difficulty, Qualified, Loved, Plays }
        public enum BeastSaberFilterOptions { CuratorRecommended };

        //Extension Methods
        public static string Name(this BeatSaverFilterOptions option)
        {
            return option.ToString();
        }
        public static string Name(this ScoreSaberFilterOptions option)
        {
            return option.ToString();
        }
        public static string Name(this BeastSaberFilterOptions option)
        {
            switch(option)
            {
                case BeastSaberFilterOptions.CuratorRecommended:
                    return "Curator Recommended";
                default:
                    return option.ToString();
            }
        }
    }
}
