using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using BeatSaverDownloader.Bookmarks;
using UnityEngine;
using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.ViewControllers.MoreSongsList;
using TMPro;
using Color = UnityEngine.Color;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using UnityEngine.UI;
using IPA.Utilities;
using ScoreSaberSharp;

namespace BeatSaverDownloader.UI.ViewControllers
{
    internal class MoreSongsListViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {

        internal Filters.FilterMode CurrentFilter = Filters.FilterMode.BeatSaver;
        private Filters.FilterMode _previousFilter = Filters.FilterMode.BeatSaver;
        internal Filters.BeatSaverFilterOptions CurrentBeatSaverFilter = Filters.BeatSaverFilterOptions.Curated;
        internal Filters.ScoreSaberFilterOptions CurrentScoreSaberFilter = Filters.ScoreSaberFilterOptions.Trending;
        private User _currentUploader;
        private string _currentSearch;
        private string _fetchingDetails = "";
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.moreSongsList.bsml";
        internal NavigationController NavController;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        [UIParams]
        private BeatSaberMarkupLanguage.Parser.BSMLParserParams _parserParams;

        [UIComponent("list")]
        public CustomListTableData customListTableData;
        [UIComponent("sortList")]
        public CustomListTableData sortListTableData;
        [UIComponent("sourceList")]
        public CustomListTableData sourceListTableData;
        [UIComponent("loadingModal")]
        public ModalView loadingModal;
        [UIComponent("sortModal")]
        public ModalView sortModal;
        [UIComponent("sortButton")]
        private Button _sortButton;
        [UIComponent("searchButton")]
        private Button _searchButton;
        [UIComponent("searchKeyboard")]
        private ModalKeyboard _searchKeyboard;

        private string _searchValue = "";
        [UIValue("searchValue")]
        public string SearchValue
        {
            get => _searchValue;
            set
            {
                _searchValue = value;
                NotifyPropertyChanged();
            }
        }
        private readonly List<StrongBox<Beatmap>> _songs = new List<StrongBox<Beatmap>>();
        public readonly List<Tuple<Beatmap, Sprite>> MultiSelectSongs = new List<Tuple<Beatmap, Sprite>>();
        public LoadingControl loadingSpinner;
        private Progress<double> _fetchProgress;

        public Action<StrongBox<Beatmap>, Sprite, Task<AudioClip>> DidSelectSong;
        public Action FilterDidChange;
        public Action MultiSelectDidChange;
        public bool Working
        {
            get { return _working; }
            set { _working = value; _songsDownButton.interactable = !value; SetLoading(value); }
        }

        internal bool AllowAIGeneratedMaps = false;

        private bool _working;
        private uint _lastPage = 0;
        private bool _endOfResults = false;
        private bool _multiSelectEnabled = false;
        internal bool MultiSelectEnabled
        {
            get => _multiSelectEnabled;
            set
            {
                _multiSelectEnabled = value;
                ToggleMultiSelect(value);
            }
        }
        internal void ToggleMultiSelect(bool value)
        {
            MultiSelectClear();
            if (value)
            {
                customListTableData.TableView.selectionType = TableViewSelectionType.Multiple;
                _sortButton.interactable = false;
                _searchButton.interactable = false;
            }
            else
            {
                _sortButton.interactable = true;
                _searchButton.interactable = true;
                customListTableData.TableView.selectionType = TableViewSelectionType.Single;
            }

        }
        internal void MultiSelectClear()
        {
            customListTableData.TableView.ClearSelection();
            MultiSelectSongs.Clear();
        }

        [UIAction("listSelect")]
        internal void Select(TableView tableView, int row)
        {
            if (MultiSelectEnabled)
                if (MultiSelectSongs.All(x => x.Item1.LatestVersion.Hash != _songs[row].Value.LatestVersion.Hash))
                    MultiSelectSongs.Add(new Tuple<Beatmap, Sprite>(_songs[row].Value, customListTableData.Data[row].Icon));

            Task<AudioClip> preview = null;
            if (customListTableData.Data[row] is BeatSaverCustomSongCellInfo bsCellInfo)
            {
                preview = bsCellInfo.LoadPreview();
            }

            DidSelectSong?.Invoke(_songs[row], customListTableData.Data[row].Icon, preview);
        }

        private void SortClosed()
        {
            CurrentFilter = _previousFilter;
        }

        [UIAction("sortPressed")]
        internal void SortPressed()
        {
            sourceListTableData.TableView.ClearSelection();
            sortListTableData.TableView.ClearSelection();
        }

        [UIValue("syncEnabled")]
        public bool SyncEnabled
        {
            get => _syncEnable;
            set
            {
                _syncEnable = value;
                NotifyPropertyChanged();
            }
        }
        private bool _syncEnable = true;

        [UIAction("syncPressed")]
        internal async void SyncPressed()
        {
            SyncEnabled = false;

            try
            {
                await _bookmarksApi.Sync(true, SyncCallback);
            }
            catch (TokenApi.InvalidOauthCredentialsException)
            {
                _parserParams.EmitEvent("open-oauthModal");
            }
            finally
            {
                SyncEnabled = true;
            }
        }

        private async Task SyncCallback()
        {
            SyncEnabled = false;

            try
            {
                _parserParams.EmitEvent("close-oauthModal");

                await _bookmarksApi.Sync(false);
            }
            finally
            {
                SyncEnabled = true;
            }
        }

        [UIAction("cancelOauth")]
        internal void CancelOauth()
        {
            _parserParams.EmitEvent("close-oauthModal");
        }

        [UIAction("sortSelect")]
        internal async void SelectedSortOption(TableView tableView, int row)
        {
            if (!(sortListTableData.Data[row] is SortFilterCellInfo sfci)) return;
            
            var filter = sfci.SortFilter;
            CurrentFilter = filter.Mode;
            _previousFilter = filter.Mode;
            CurrentBeatSaverFilter = filter.BeatSaverOption;
            CurrentScoreSaberFilter = filter.ScoreSaberOption;
            _parserParams.EmitEvent("close-sortModal");
            ClearData();
            FilterDidChange?.Invoke();
            await GetNewPage(3);
        }

        [UIAction("sourceSelect")]
        internal void SelectedSource(TableView tableView, int row)
        {
            if (!(sourceListTableData.Data[row] is SourceCellInfo sci)) return;

            _parserParams.EmitEvent("close-sourceModal");
            var filter = sci.Filter;
            _previousFilter = CurrentFilter;
            CurrentFilter = filter;
            SetupSortOptions();
            _parserParams.EmitEvent("open-sortModal");
        }

        [UIAction("searchOpened")]
        internal void SearchOpened()
        {
            _interactableGroup.gameObject.SetActive(false);
            customListTableData.TableView.ClearSelection();
            FilterDidChange?.Invoke();
        }

        [UIComponent("interactableGroup")]
        private VerticalLayoutGroup _interactableGroup;

        [UIAction("searchPressed")]
        internal async void SearchPressed(string text)
        {
            _interactableGroup.gameObject.SetActive(true);
            if (string.IsNullOrWhiteSpace(text)) return;
            _currentSearch = text;
            CurrentFilter = Filters.FilterMode.Search;
            ClearData();
            FilterDidChange?.Invoke();
            await GetNewPage(3);

        }

        [UIAction("multiSelectToggle")]
        internal void ToggleMultiSelect()
        {
            MultiSelectEnabled = !MultiSelectEnabled;
            MultiSelectDidChange?.Invoke();
        }

        [UIAction("abortClicked")]
        private void AbortPageFetch()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            Working = false;
        }

        internal async void SortByUser(User user)
        {
            _currentUploader = user;
            CurrentFilter = Filters.FilterMode.BeatSaver;
            CurrentBeatSaverFilter = Filters.BeatSaverFilterOptions.Uploader;
            ClearData();
            FilterDidChange?.Invoke();
            await GetNewPage(3);
        }

        [UIAction("pageUpPressed")]
        internal void PageUpPressed()
        { }

        [UIComponent("songsPageDown")]
        private Button _songsDownButton;

        private BookmarksApi _bookmarksApi;

        [UIAction("pageDownPressed")]
        internal async void PageDownPressed()
        {
            if (!(customListTableData.Data.Count >= 1) || _endOfResults || _multiSelectEnabled) return;
            if (customListTableData.Data.Count - customListTableData.TableView.visibleCells.Last().idx <= 7)
            {
                await GetNewPage(4);
            }
        }

        private void ClearData()
        {
            _lastPage = 0;
            customListTableData.TableView.ClearSelection();
            customListTableData.Data.Clear();
            customListTableData.TableView.ReloadData();
            customListTableData.TableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
            _songs.Clear();
            MultiSelectSongs.Clear();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _interactableGroup.gameObject.SetActive(true);
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (!firstActivation)
            {
                InitSongList();
            }
        }

        [UIAction("#post-parse")]
        internal void SetupList()
        {
            if (transform is RectTransform rt)
            {
                rt.sizeDelta = new Vector2(70, 0);
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 1);
            }

            _fetchProgress = new Progress<double>(ProgressUpdate);
            SetupSourceOptions();
            sortModal.blockerClickedEvent += SortClosed;
            var keyKey = new KEYBOARD.KEY(_searchKeyboard.Keyboard, new Vector2(-35, 11f), "Key:", 15, 10, new Color(0.92f, 0.64f, 0));
            var includeAIKey = new KEYBOARD.KEY(_searchKeyboard.Keyboard, new Vector2(-27f, 11f), "Include Auto Generated", 45, 10, new Color(0.984f, 0.282f, 0.305f));
            keyKey.KeyAction += KeyKeyPressed;
            includeAIKey.KeyAction += IncludeAIKeyPressed;
            _searchKeyboard.Keyboard.Keys.Add(keyKey);
            _searchKeyboard.Keyboard.Keys.Add(includeAIKey);
            InitSongList();
        }

        private void IncludeAIKeyPressed(KEYBOARD.KEY key)
        {
            AllowAIGeneratedMaps = !AllowAIGeneratedMaps;
            var color = AllowAIGeneratedMaps ? new Color(0.341f, 0.839f, 0.341f) : new Color(0.984f, 0.282f, 0.305f);
            key.GetField<TMP_Text, KEYBOARD.KEY>("buttonText")
               .transform.parent
               .GetComponentInChildren<Image>().color = color;
        }

        private void KeyKeyPressed(KEYBOARD.KEY key)
        {
            _searchKeyboard.Keyboard.KeyboardText.text = "Key:";
        }

        private async void InitSongList()
        {
            await GetNewPage(3);
        }

        public void ProgressUpdate(double progress)
        {
            SetLoading(true, progress, _fetchingDetails);
        }

        public void SetLoading(bool value, double progress = 0, string details = "")
        {
            if (loadingSpinner == null)
                loadingSpinner = Instantiate(Resources.FindObjectsOfTypeAll<LoadingControl>().First(), loadingModal.transform);
            Destroy(loadingSpinner.GetComponent<Touchable>());
            if (value)
            {
                _parserParams.EmitEvent("open-loadingModal");
                loadingSpinner.ShowDownloadingProgress("Fetching More Songs... " + details, (float)progress);
            }
            else
            {
                _parserParams.EmitEvent("close-loadingModal");
            }
        }

        public void SetupSourceOptions()
        {
            sourceListTableData.Data.Clear();
            sourceListTableData.Data.Add(new SourceCellInfo(Filters.FilterMode.BeatSaver, "BeatSaver", null, Sprites.BeatSaverIcon));
            sourceListTableData.Data.Add(new SourceCellInfo(Filters.FilterMode.ScoreSaber, "ScoreSaber", null, Sprites.ScoreSaberIcon));
            sourceListTableData.TableView.ReloadData();
        }
        public void SetupSortOptions()
        {
            sortListTableData.Data.Clear();
            switch (CurrentFilter)
            {
                case Filters.FilterMode.BeatSaver:
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.BeatSaver, Filters.BeatSaverFilterOptions.Latest), "Latest", "BeatSaver", Sprites.BeatSaverIcon));
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.BeatSaver, Filters.BeatSaverFilterOptions.Rating), "Rating", "BeatSaver", Sprites.BeatSaverIcon));
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.BeatSaver, Filters.BeatSaverFilterOptions.Curated), "Curated", "BeatSaver", Sprites.BeatSaverIcon));
              // Sort By Downloads will return in BeatSaver: Infinity War
              //      sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.BeatSaver, Filters.BeatSaverFilterOptions.Downloads), "Downloads", "BeatSaver", Sprites.BeatSaverIcon));
              //      sortListTableData.data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.BeatSaver, Filters.BeatSaverFilterOptions.Plays), "Plays", "BeatSaver", Sprites.BeatSaverIcon));
                    break;
                case Filters.FilterMode.ScoreSaber:
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.ScoreSaber, default, Filters.ScoreSaberFilterOptions.Trending), "Trending", "ScoreSaber", Sprites.ScoreSaberIcon));
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.ScoreSaber, default, Filters.ScoreSaberFilterOptions.Ranked), "Ranked", "ScoreSaber", Sprites.ScoreSaberIcon));
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.ScoreSaber, default, Filters.ScoreSaberFilterOptions.Qualified), "Qualified", "ScoreSaber", Sprites.ScoreSaberIcon));
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.ScoreSaber, default, Filters.ScoreSaberFilterOptions.Loved), "Loved", "ScoreSaber", Sprites.ScoreSaberIcon));
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.ScoreSaber, default, Filters.ScoreSaberFilterOptions.Difficulty), "Difficulty", "ScoreSaber", Sprites.ScoreSaberIcon));
                    sortListTableData.Data.Add(new SortFilterCellInfo(new SortFilter(Filters.FilterMode.ScoreSaber, default, Filters.ScoreSaberFilterOptions.Plays), "Plays", "ScoreSaber", Sprites.ScoreSaberIcon));
                    break;

            }
            sortListTableData.TableView.ReloadDataKeepingPosition();
        }
        public void Cleanup()
        {
            AbortPageFetch();
            _parserParams.EmitEvent("closeAllModals");
            ClearData();
        }

        private async Task GetNewPage(uint count = 1)
        {
            if (Working) return;
            _endOfResults = false;
            Plugin.LOG.Info($"Fetching {count} new page(s)");
            Working = true;
            try
            {
                switch (CurrentFilter)
                {
                    case Filters.FilterMode.BeatSaver:
                        await GetPagesBeatSaver(count);
                        break;
                    case Filters.FilterMode.ScoreSaber:
                        await GetPagesScoreSaber(count);
                        break;
                    case Filters.FilterMode.Search:
                        await GetPagesSearch(count);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (TaskCanceledException e)
            {
                Plugin.LOG.Warn("Page Fetching Aborted.");
                Plugin.LOG.Critical(e.InnerException ?? e);
            }
            catch (Exception e)
            {
                Plugin.LOG.Critical("Failed to fetch new pages!");
                Plugin.LOG.Critical(e.InnerException ?? e);
            }
            Working = false;
        }

        private async Task<Songs> FetchFromScoreSaber(Filters.ScoreSaberFilterOptions filter)
        {
            switch (filter)
            {
                case Filters.ScoreSaberFilterOptions.Trending:
                    return await ScoreSaber.Trending(_lastPage, _cancellationTokenSource.Token, _fetchProgress);
                case Filters.ScoreSaberFilterOptions.Ranked:
                    return await ScoreSaber.Ranked(_lastPage, _cancellationTokenSource.Token, _fetchProgress);
                case Filters.ScoreSaberFilterOptions.Qualified:
                    return await ScoreSaber.Qualified(_lastPage, _cancellationTokenSource.Token, _fetchProgress);
                case Filters.ScoreSaberFilterOptions.Loved:
                    return await ScoreSaber.Loved(_lastPage, _cancellationTokenSource.Token, _fetchProgress);
                case Filters.ScoreSaberFilterOptions.Plays:
                    return await ScoreSaber.Plays(_lastPage, _cancellationTokenSource.Token, _fetchProgress);
                case Filters.ScoreSaberFilterOptions.Difficulty:
                    return await ScoreSaber.Difficulty(_lastPage, _cancellationTokenSource.Token, _fetchProgress);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task GetPagesScoreSaber(uint count)
        {
            var newMaps = new List<Song>();
            for (uint i = 0; i < count; ++i)
            {
                _fetchingDetails = $"({i + 1}/{count})";
                var page = await FetchFromScoreSaber(CurrentScoreSaberFilter);
                _lastPage++;

                if (page?.songs != null)
                    newMaps.AddRange(page.songs);

                if (page?.songs?.Any() != true) break;
            }

            var maps = await Plugin.BeatSaver.BeatmapByHash(newMaps.Select(x => x.id).ToArray());
            var newMapsCast = newMaps
                .Select(x => maps.TryGetValue(x.id.ToUpperInvariant(), out var fromScoreSaber) ? fromScoreSaber : null)
                .Where(x => x != null)
                .ToList();

            AddMapsToView(newMapsCast);
            _fetchingDetails = "";
        }

        private SearchTextFilterOption GenerateOptions(Filters.BeatSaverFilterOptions filter, bool allowAI)
        {
            var options = new SearchTextFilterOption();
            if (allowAI) options.IncludeAutomappers = true;

            switch (filter)
            {
                case Filters.BeatSaverFilterOptions.Latest:
                    options.SortOrder = SortingOptions.Latest;
                    break;
                case Filters.BeatSaverFilterOptions.Rating:
                    options.SortOrder = SortingOptions.Rating;
                    break;
                case Filters.BeatSaverFilterOptions.Uploader:
                    options.SortOrder = SortingOptions.Latest;
                    break;
                case Filters.BeatSaverFilterOptions.Curated:
                    options.SortOrder = SortingOptions.Curated;
                    break;
                // Plays and downloads kept for compatibility
                case Filters.BeatSaverFilterOptions.Plays:
                case Filters.BeatSaverFilterOptions.Downloads:
                default:
                    options.SortOrder = SortingOptions.Relevance;
                    break;
            }

            return options;
        }

        private async Task GetPagesBeatSaver(uint count)
        {
            var newMaps = new List<Beatmap>();
            for (uint i = 0; i < count; ++i)
            {
                try
                {
                    _fetchingDetails = $"({i + 1}/{count})";

                    var page = CurrentBeatSaverFilter != Filters.BeatSaverFilterOptions.Uploader
                        ? await Plugin.BeatSaver.SearchBeatmaps(GenerateOptions(CurrentBeatSaverFilter, AllowAIGeneratedMaps), (int)_lastPage, _cancellationTokenSource.Token)
                        : await _currentUploader.Beatmaps((int)_lastPage, _cancellationTokenSource.Token);

                    _lastPage++;

                    if (page?.Beatmaps != null)
                        newMaps.AddRange(page.Beatmaps);

                    if (page?.Empty != false) break;
                }
                catch (Exception)
                {
                    // pages didn't load properly
                }
            }

            AddMapsToView(newMaps);
            _fetchingDetails = "";
        }

        private async Task DoKeySearch()
        {
            var key = _currentSearch.Split(':')[1];
            _fetchingDetails = $" (By Key:{key}";
            var keyMap = await Plugin.BeatSaver.Beatmap (key, _cancellationTokenSource.Token);
            if (keyMap != null && _songs.All(x => x.Value != keyMap))
            {
                _songs.Add(new StrongBox<Beatmap>(keyMap));
                customListTableData.Data.Add(SongDownloader.IsSongDownloaded(keyMap.LatestVersion.Hash)
                    ? new BeatSaverCustomSongCellInfo(keyMap, CellDidSetImage, $"<#7F7F7F>{keyMap.Name}", keyMap.Uploader.Name)
                    : new BeatSaverCustomSongCellInfo(keyMap, CellDidSetImage, keyMap.Name, keyMap.Uploader.Name));
                customListTableData.TableView.ReloadDataKeepingPosition();
            }
            _fetchingDetails = "";
        }

        private async Task GetPagesSearch(uint count)
        {
            if (_currentSearch.StartsWith("Key:"))
            {
                await DoKeySearch();
                return;
            }

            var newMaps = new List<Beatmap>();
            for (uint i = 0; i < count; ++i)
            {
                try
                {
                    _fetchingDetails = $"({i + 1}/{count})";
                    var page = await Plugin.BeatSaver.SearchBeatmaps(new SearchTextFilterOption(_currentSearch), (int) _lastPage, _cancellationTokenSource.Token);

                    _lastPage++;

                    if (page?.Beatmaps != null)
                        newMaps.AddRange(page.Beatmaps);

                    if (page?.Empty != false) break;
                }
                catch
                {
                    // really should add proper error handling to this
                }
            }

            AddMapsToView(newMaps);
            _fetchingDetails = "";
        }

        private void AddMapsToView(List<Beatmap> newMaps)
        {
            _songs.AddRange(newMaps.Select(x => new StrongBox<Beatmap>(x)));
            foreach (var song in newMaps)
            {
                customListTableData.Data.Add(SongDownloader.IsSongDownloaded(song.LatestVersion.Hash)
                    ? new BeatSaverCustomSongCellInfo(song, CellDidSetImage, $"<#7F7F7F>{song.Name}", song.Uploader.Name)
                    : new BeatSaverCustomSongCellInfo(song, CellDidSetImage, song.Name, song.Uploader.Name));
                customListTableData.TableView.ReloadDataKeepingPosition();
            }
        }

        private void CellDidSetImage(CustomListTableData.CustomCellInfo cell)
        {
            var shouldRefresh = customListTableData.TableView.visibleCells
                .Select(visibleCell => visibleCell as LevelListTableCell)
                .Any(levelCell => levelCell.GetField<TextMeshProUGUI, LevelListTableCell>("_songNameText")?.text == cell.Text);

            if (!shouldRefresh) return;

            customListTableData.TableView.RefreshCellsContent();
        }

        public void AddBookmarksApi(BookmarksApi bookmarksApi)
        {
            _bookmarksApi = bookmarksApi;
        }
    }
}
