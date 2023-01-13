using System;
using System.ComponentModel;
using System.Threading;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using TMPro;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers.DownloadQueue
{
    internal class DownloadQueueItem : INotifyPropertyChanged
    {
        private QueueManager.QueueItem _item;
        internal readonly BeatSaverSharp.Models.Beatmap Beatmap;
        private UnityEngine.UI.Image _bgImage;
        private float _downloadingProgess;
        public event PropertyChangedEventHandler PropertyChanged;

        [UIComponent("coverImage")]
        private HMUI.ImageView _coverImage;

        [UIComponent("songNameText")]
        private TextMeshProUGUI _songNameText;

        [UIComponent("authorNameText")]
        private TextMeshProUGUI _authorNameText;

        [UIAction("abortClicked")]
        internal void AbortDownload()
        {
            _item.Cancel();
        }

        private string _songName;
        private string _authorName;
        private Sprite _cover;

        public DownloadQueueItem(QueueManager.QueueItem item)
        {
            _item = item;

            Beatmap = item.Beatmap;
            _songName = Beatmap.Metadata.SongName;
            _cover = item.Sprite;
            _authorName = $"{Beatmap.Metadata.SongAuthorName} <size=80%>[{Beatmap.Metadata.LevelAuthorName}]";

            item.DownloadCancelled += () => DownloadQueueViewController.DidAbortDownload?.Invoke(this);
            item.DownloadProgress += ProgressUpdate;
            item.DownloadCompleted += () => DownloadQueueViewController.DidFinishDownloadingItem?.Invoke(this);
        }

        [UIAction("#post-parse")]
        internal void Setup()
        {
            if (!_coverImage || !_songNameText || !_authorNameText) return;

            var filter = _coverImage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            filter.aspectRatio = 1f;
            filter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.HeightControlsWidth;
            _coverImage.sprite = _cover;
            //_coverImage.texture.wrapMode = TextureWrapMode.Clamp;
            _coverImage.rectTransform.sizeDelta = new Vector2(8, 0);
            _songNameText.text = _songName;
            _authorNameText.text = _authorName;

            _bgImage = _coverImage.transform.parent.gameObject.AddComponent<HMUI.ImageView>();
            _bgImage.enabled = true;
            _bgImage.sprite = Sprite.Create((new Texture2D(1, 1)), new Rect(0, 0, 1, 1), Vector2.one / 2f);
            _bgImage.type = UnityEngine.UI.Image.Type.Filled;
            _bgImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            _bgImage.fillAmount = 0;
            _bgImage.material = Utilities.ImageResources.NoGlowMat;
        }

        internal void ProgressUpdate(double progress)
        {
            _downloadingProgess = (float)progress;
            Color color = SongCore.Utilities.HSBColor.ToColor(new SongCore.Utilities.HSBColor(Mathf.PingPong(_downloadingProgess * 0.35f, 1), 1, 1));
            color.a = 0.35f;
            _bgImage.color = color;
            _bgImage.fillAmount = _downloadingProgess;
        }
    }
}