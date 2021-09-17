using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using System;
using TMPro;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class MultiSelectDetailViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.multiSelectDetailView.bsml";
        [UIValue("multiDescription")]
        private string _multiDescription = "\n <size=135%><b>Multi-Select Activated!</b></size>\n New Pages will not be fetched while this mode is on.\nSongs will not be added to queue if already downloaded.\n\n Press the \"Add Songs to Queue\" Button to download all of your selected songs, and press the clear button to clear your selection. </align>";
        [UIComponent("textPage")]
        private TextPageScrollView _multiTextPage;
        public Action multiSelectClearPressed;
        public Action multiSelectDownloadPressed;

        private string _multiDownloadText = "Add Songs To Queue";
        [UIValue("multiDownloadText")]
        public string MultiDownloadText
        {
            get => _multiDownloadText;
            set
            {
                _multiDownloadText = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("clearPressed")]
        internal void ClearButtonPressed()
        {
            multiSelectClearPressed?.Invoke();
            MultiDownloadText = "Add Songs To Queue";
        }
        [UIAction("downloadPressed")]
        internal void DownloadButtonPressed()
        {
            multiSelectDownloadPressed?.Invoke();
            MultiDownloadText = "Add Songs To Queue";
        }
        [UIAction("#post-parse")]
        internal void Setup()
        {
            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);
            
            _multiTextPage.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

    }
}
