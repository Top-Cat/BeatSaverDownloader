using BeatSaberMarkupLanguage;
using BeatSaverDownloader.UI.ViewControllers;
using BeatSaverDownloader.Misc;
using HMUI;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaverDownloader.Bookmarks;
using BeatSaverDownloader.UI.ViewControllers.DownloadQueue;

namespace BeatSaverDownloader.UI
{
    internal class MoreSongsFlowCoordinator : FlowCoordinator
    {
        public FlowCoordinator ParentFlowCoordinator { get; protected set; }
        public bool AllowFlowCoordinatorChange { get; protected set; } = true;

        private NavigationController _moreSongsNavigationcontroller;
        private MoreSongsListViewController _moreSongsView;
        private SongDetailViewController _songDetailView;
        private MultiSelectDetailViewController _multiSelectDetailView;
        private SongDescriptionViewController _songDescriptionView;
        private DownloadQueueViewController _downloadQueueView;

        public void Awake()
        {
            if (_moreSongsView == null)
            {
                _moreSongsView = BeatSaberUI.CreateViewController<MoreSongsListViewController>();
                _songDetailView = BeatSaberUI.CreateViewController<SongDetailViewController>();
                _multiSelectDetailView = BeatSaberUI.CreateViewController<MultiSelectDetailViewController>();
                _moreSongsNavigationcontroller = BeatSaberUI.CreateViewController<NavigationController>();
                _moreSongsView.NavController = _moreSongsNavigationcontroller;
                _songDescriptionView = BeatSaberUI.CreateViewController<SongDescriptionViewController>();
                _downloadQueueView = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

                _moreSongsView.DidSelectSong += HandleDidSelectSong;
                _moreSongsView.FilterDidChange += HandleFilterDidChange;
                _moreSongsView.MultiSelectDidChange += HandleMultiSelectDidChange;
                _songDetailView.DidPressDownload += HandleDidPressDownload;
                _songDetailView.DidPressUploader += HandleDidPressUploader;
                _songDetailView.SetDescription += _songDescriptionView.Initialize;
                _multiSelectDetailView.MultiSelectClearPressed += _moreSongsView.MultiSelectClear;
                _multiSelectDetailView.MultiSelectDownloadPressed += HandleMultiSelectDownload;
            }
        }

        public void AddQueueManager(QueueManager queueManager, BookmarksApi bookmarksApi)
        {
            _downloadQueueView.AddQueueManager(queueManager);
            _moreSongsView.AddBookmarksApi(bookmarksApi);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    UpdateTitle();
                    showBackButton = true;

                    SetViewControllersToNavigationController(_moreSongsNavigationcontroller, _moreSongsView);
                    ProvideInitialViewControllers(_moreSongsNavigationcontroller, _downloadQueueView);
                    //  PopViewControllerFromNavigationController(_moreSongsNavigationcontroller);
                }
                if (addedToHierarchy)
                {
                }
            }
            catch (Exception ex)
            {
                Plugin.LOG.Error(ex);
            }
        }

        public void SetParentFlowCoordinator(FlowCoordinator parent)
        {
            if (!AllowFlowCoordinatorChange)
                throw new InvalidOperationException("Changing the parent FlowCoordinator is not allowed on this instance.");
            ParentFlowCoordinator = parent;
        }

        private void UpdateTitle()
        {
            var localTitle = $"{_moreSongsView.CurrentFilter}";
            switch (_moreSongsView.CurrentFilter)
            {
                case Filters.FilterMode.BeatSaver:
                    SetTitle(localTitle + $" - {_moreSongsView.CurrentBeatSaverFilter.Name()}");
                    break;
                case Filters.FilterMode.ScoreSaber:
                    SetTitle(localTitle + $" - {_moreSongsView.CurrentScoreSaberFilter.Name()}");
                    break;
            }
        }

        private void HandleDidSelectSong(StrongBox<BeatSaverSharp.Models.Beatmap> song, Sprite cover = null, Task<AudioClip> clip = null)
        {
            _songDetailView.ClearData();
            _songDescriptionView.ClearData();
            if (!_moreSongsView.MultiSelectEnabled)
            {
                if (!_songDetailView.isInViewControllerHierarchy)
                {
                    PushViewControllerToNavigationController(_moreSongsNavigationcontroller, _songDetailView);
                }
                SetRightScreenViewController(_songDescriptionView, ViewController.AnimationType.None);
                _songDetailView.Initialize(song, cover, clip);
            }
            else
            {
                var count = _moreSongsView.MultiSelectSongs.Count;
                var grammar = count > 1 ? "Songs" : "Song";
                _multiSelectDetailView.MultiDownloadText = $"Add {count} {grammar} To Queue";
            }
        }

        private void HandleDidPressDownload(BeatSaverSharp.Models.Beatmap song, Sprite cover)
        {
            Plugin.LOG.Info("Download pressed for song: " + song.Metadata.SongName);
            //    Misc.SongDownloader.Instance.DownloadSong(song);
            _songDetailView.UpdateDownloadButtonStatus();
            _downloadQueueView.EnqueueSong(song, cover);
        }

        private void HandleMultiSelectDownload()
        {
            _downloadQueueView.EnqueueSongs(_moreSongsView.MultiSelectSongs.ToArray());
            _moreSongsView.MultiSelectClear();
        }

        private void HandleMultiSelectDidChange()
        {

            if (_moreSongsView.MultiSelectEnabled)
            {
                _songDetailView.ClearData();
                _songDescriptionView.ClearData();
                _moreSongsNavigationcontroller.PopViewControllers(_moreSongsNavigationcontroller.viewControllers.Count, null, true);
                _moreSongsNavigationcontroller.PushViewController(_moreSongsView, null, true);
                _moreSongsNavigationcontroller.PushViewController(_multiSelectDetailView, null, false);
            }
            else
            {
                _moreSongsNavigationcontroller.PopViewControllers(_moreSongsNavigationcontroller.viewControllers.Count, null, true);
                _moreSongsNavigationcontroller.PushViewController(_moreSongsView, null, true);
            }
        }

        private void HandleDidPressUploader(BeatSaverSharp.Models.User uploader)
        {
            Plugin.LOG.Info("Uploader pressed for user: " + uploader.Name);
            _moreSongsView.SortByUser(uploader);
        }

        private void HandleFilterDidChange()
        {
            UpdateTitle();
            if (_songDetailView.isInViewControllerHierarchy)
            {
                PopViewControllersFromNavigationController(_moreSongsNavigationcontroller, 1);
            }
            _songDetailView.ClearData();
            _songDescriptionView.ClearData();
        }

        protected override void BackButtonWasPressed(ViewController topView)
        {
            if (_songDetailView.isInViewControllerHierarchy)
            {
                PopViewControllersFromNavigationController(_moreSongsNavigationcontroller, 1, null, true);
            }
            _moreSongsView.Cleanup();
            _downloadQueueView.AbortAllDownloads();
            PluginUI.SongPreviewPlayer.CrossfadeToDefault();
            ParentFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}