using System.Collections.Generic;

namespace BeatSaverDownloader.BeastSaber
{
    public class BeastSaberSong
    {
        public string title;
        public string song_key;
        public string hash;
        public string level_author_name;
    }

    public class BeastSaberApiResult
    {
        public List<BeastSaberSong> songs;
        public int next_page;
    }
}
