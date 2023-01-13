namespace BeatSaverDownloader.Misc
{
    public static class Filters
    {
        public enum FilterMode { Search, BeatSaver, ScoreSaber }
        public enum BeatSaverFilterOptions { Latest, Curated, Rating, Downloads, Plays, Uploader }
        public enum ScoreSaberFilterOptions { Trending, Ranked, Difficulty, Qualified, Loved, Plays }

        //Extension Methods
        public static string Name(this BeatSaverFilterOptions option)
        {
            return option.ToString();
        }
        public static string Name(this ScoreSaberFilterOptions option)
        {
            return option.ToString();
        }
    }
}
