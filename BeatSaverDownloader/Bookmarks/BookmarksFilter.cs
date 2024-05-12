using System.Threading;
using System.Threading.Tasks;
using BetterSongList;
using BetterSongList.FilterModels;
using BetterSongList.Interfaces;
using BetterSongList.SortModels;

namespace BeatSaverDownloader.Bookmarks
{
    internal class BookmarksFilter : IFilter, ITransformerPlugin
    {
        private readonly BookmarksApi _bookmarksApi;

        public string name => "Bookmarked";
        public bool visible => true;

        public BookmarksFilter(BookmarksApi bookmarksApi)
        {
            Plugin.LOG.Info("Loading bookmarks filter");
            _bookmarksApi = bookmarksApi;
            FilterMethods.Register(this);
        }

        Task IFilter.Prepare(CancellationToken cancelToken) => Task.CompletedTask;
        Task ISorter.Prepare(CancellationToken cancelToken) => Task.CompletedTask;

        bool ISorter.isReady => false;
        bool IFilter.isReady => true;

        public bool GetValueFor(BeatmapLevel level) => _bookmarksApi.IsBookmarked(level);

        public void ContextSwitch(SelectLevelCategoryViewController.LevelCategory levelCategory, BeatmapLevelPack playlist)
        {
            // Do nothing
        }
    }
}
