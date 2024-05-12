using System;
using System.Collections;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using System.Linq;
using BeatSaberMarkupLanguage.Util;
using BeatSaverDownloader.Bookmarks;
using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.ViewControllers.DownloadQueue;
using BS_Utils.Utilities;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaverDownloader.UI
{
    public class PluginUI : PersistentSingleton<PluginUI>
    {
        public MenuButton MoreSongsButton;
        internal static SongPreviewPlayer SongPreviewPlayer;
        private MoreSongsFlowCoordinator _moreSongsFlowCooridinator;
        public static GameObject LevelDetailClone;
        private static BookmarksApi _bookmarksApi;
        private static QueueManager _queueManager;
        private static Action<LevelCollectionViewController,BeatmapLevel> _handler;

        internal void Setup(BookmarksApi bookmarksApi, QueueManager queueManager)
        {
            _bookmarksApi = bookmarksApi;
            _queueManager = queueManager;

            MoreSongsButton = new MenuButton("More Songs", "Download More Songs from here!", MoreSongsButtonPressed, false);
            MenuButtons.instance.RegisterButton(MoreSongsButton);
        }

        private static IEnumerator FixFavouriteButton(StandardLevelDetailView levelDetail)
        {
            yield return new WaitForSeconds(0.1f);

            var favouriteButton = levelDetail.GetComponentsInChildren<ToggleWithCallbacks>().First(x => x.name == "FavoriteToggle");
            var favTransform = (RectTransform) favouriteButton.transform;

            favTransform.sizeDelta = new Vector2(12, 14);

            var bookmarkButton = UnityEngine.Object.Instantiate(favouriteButton, favTransform.parent);
            bookmarkButton.name = "BookmarkToggle";
            bookmarkButton.transform.localPosition = new Vector3(26, -2, 0);
            var graphics = bookmarkButton.GetComponentsInChildren<ImageView>();
            foreach (var imageView in graphics)
                if (imageView.transform is RectTransform rt)
                    rt.localPosition = new Vector3((rt.sizeDelta.x - 8) / 2, -7, 0);

            var checkmark = graphics.First(x => x.name == "Checkmark");
            var placeholder = graphics.First(x => x.name == "Placeholder");
            checkmark.sprite = Sprites.RemoveFromFavorites;
            placeholder.sprite = Sprites.AddToFavorites;
            bookmarkButton.graphic = checkmark;

            bookmarkButton.onValueChanged.AddListener(toggle => BookmarkButtonPressed(bookmarkButton, toggle));

            if (_handler != null)
                BSEvents.levelSelected -= _handler;

            _handler = (controller, level) => LevelSelected(bookmarkButton, level);
            BSEvents.levelSelected += _handler;
        }

        internal static void SetupLevelDetailClone()
        {
            var levelDetail = Resources.FindObjectsOfTypeAll<StandardLevelDetailView>().First(x => x.gameObject.name == "LevelDetail");

            // Setup extra buttons
            SongPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();
            SongPreviewPlayer.StartCoroutine(FixFavouriteButton(levelDetail));

            // Get DI Container
            var bdscc = levelDetail.GetField<BeatmapDifficultySegmentedControlController, StandardLevelDetailView>("_beatmapDifficultySegmentedControlController");
            var tsc = bdscc.GetField<TextSegmentedControl, BeatmapDifficultySegmentedControlController>("_difficultySegmentedControl");
            var di = tsc.GetField<DiContainer, SegmentedControl>("_container");

            // Clone Level Detail View for use in song download screen
            LevelDetailClone = UnityEngine.Object.Instantiate(levelDetail.gameObject);
            var clonedLdv = LevelDetailClone.GetComponent<StandardLevelDetailView>();
            di.Inject(clonedLdv);
            LevelDetailClone.gameObject.SetActive(false);

            // Remove UI we don't want
            UnityEngine.Object.Destroy(clonedLdv);
            var bsmlObjects = LevelDetailClone.GetComponentsInChildren<RectTransform>().Where(x => x.gameObject.name.StartsWith("BSML"));
            var hoverhints = LevelDetailClone.GetComponentsInChildren<HoverHint>();
            var localHoverHints = LevelDetailClone.GetComponentsInChildren<LocalizedHoverHint>();
            foreach (var bsmlObject in bsmlObjects)
                UnityEngine.Object.Destroy(bsmlObject.gameObject);
            foreach (var hoverhint in hoverhints)
                UnityEngine.Object.Destroy(hoverhint);
            foreach (var hoverhint in localHoverHints)
                UnityEngine.Object.Destroy(hoverhint);
            UnityEngine.Object.Destroy(LevelDetailClone.transform.Find("FavoriteToggle").gameObject);
            UnityEngine.Object.Destroy(LevelDetailClone.transform.Find("ActionButtons").gameObject);
        }

        private static string _selectedHash;

        private static async void BookmarkButtonPressed(Toggle bookmarkButton, bool bookmarked)
        {
            bookmarkButton.interactable = false;

            var result = await _bookmarksApi.SetBookmarkByHash(_selectedHash, bookmarked);
            if (result == null)
            {
                bookmarkButton.SetIsOnWithoutNotify(false);
                return;
            }

            if (!result.Value)
            {
                bookmarkButton.SetIsOnWithoutNotify(!bookmarked);
            }

            bookmarkButton.interactable = true;
        }

        private static void LevelSelected(Toggle bookmarkButton, BeatmapLevel level)
        {
            bookmarkButton.interactable = true;
            bookmarkButton.gameObject.SetActive(level.levelID.StartsWith("custom_level_") && PluginConfig.UserTokens?.CouldBeValid == true);

            if (bookmarkButton.gameObject.activeSelf)
            {
                _selectedHash = SongCore.Utilities.Hashing.GetCustomLevelHash(level);
            }

            bookmarkButton.SetIsOnWithoutNotify(_bookmarksApi.IsBookmarked(level));
        }

        private void MoreSongsButtonPressed()
        {
            ShowMoreSongsFlow();
        }

        private void ShowMoreSongsFlow()
        {
            if (_moreSongsFlowCooridinator == null)
            {
                _moreSongsFlowCooridinator = BeatSaberUI.CreateFlowCoordinator<MoreSongsFlowCoordinator>();
                _moreSongsFlowCooridinator.AddQueueManager(_queueManager, _bookmarksApi);
            }

            _moreSongsFlowCooridinator.SetParentFlowCoordinator(BeatSaberUI.MainFlowCoordinator);
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_moreSongsFlowCooridinator, null, ViewController.AnimationDirection.Horizontal, true);
        }
    }
}
