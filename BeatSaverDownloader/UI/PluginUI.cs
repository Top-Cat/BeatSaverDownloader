using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using HMUI;
using System.Linq;
using UnityEngine;

namespace BeatSaverDownloader.UI
{
    public class PluginUI : PersistentSingleton<PluginUI>
    {
        public MenuButton MoreSongsButton;
        internal static SongPreviewPlayer SongPreviewPlayer;
        private MoreSongsFlowCoordinator _moreSongsFlowCooridinator;
        public static GameObject LevelDetailClone;

        internal void Setup()
        {
            MoreSongsButton = new MenuButton("More Songs", "Download More Songs from here!", MoreSongsButtonPressed, false);
            MenuButtons.instance.RegisterButton(MoreSongsButton);
        }

        internal static void SetupLevelDetailClone()
        {
            SongPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();

            LevelDetailClone = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailView>().First(x => x.gameObject.name == "LevelDetail").gameObject);
            LevelDetailClone.gameObject.SetActive(false);

            Destroy(LevelDetailClone.GetComponent<StandardLevelDetailView>());
            var bsmlObjects = LevelDetailClone.GetComponentsInChildren<RectTransform>().Where(x => x.gameObject.name.StartsWith("BSML"));
            var hoverhints = LevelDetailClone.GetComponentsInChildren<HoverHint>();
            var localHoverHints = LevelDetailClone.GetComponentsInChildren<LocalizedHoverHint>();
            foreach (var bsmlObject in bsmlObjects)
                Destroy(bsmlObject.gameObject);
            foreach (var hoverhint in hoverhints)
                Destroy(hoverhint);
            foreach (var hoverhint in localHoverHints)
                Destroy(hoverhint);
            Destroy(LevelDetailClone.transform.Find("FavoriteToggle").gameObject);
            Destroy(LevelDetailClone.transform.Find("ActionButtons").gameObject);
        }

        private void MoreSongsButtonPressed()
        {
            ShowMoreSongsFlow();
        }

        private void ShowMoreSongsFlow()
        {
            if (_moreSongsFlowCooridinator == null)
                _moreSongsFlowCooridinator = BeatSaberUI.CreateFlowCoordinator<MoreSongsFlowCoordinator>();
            _moreSongsFlowCooridinator.SetParentFlowCoordinator(BeatSaberUI.MainFlowCoordinator);
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_moreSongsFlowCooridinator, null, ViewController.AnimationDirection.Horizontal, true); // ("PresentFlowCoordinator", _moreSongsFlowCooridinator, null, false, false);
        }
    }
}
