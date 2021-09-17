using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaverDownloader.Misc;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System.ComponentModel;
using BeatSaberMarkupLanguage;

namespace BeatSaverDownloader.UI.ViewControllers
{
    public class DownloadQueueViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.downloadQueue.bsml";
        internal static Action<DownloadQueueItem> didAbortDownload;
        internal static Action<DownloadQueueItem> didFinishDownloadingItem;
        [UIValue("download-queue")]
        internal List<object> queueItems = new List<object>();
        internal CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        [UIComponent("download-list")]
        private CustomCellListTableData _downloadList;

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        [UIAction("#post-parse")]
        internal void Setup()
        {
            //   for (int i = 0; i < 8; ++i)
            //       queueItems.Add(new DownloadQueueItem(Texture2D.whiteTexture, "SongName", "AuthorName"));
            _downloadList?.tableView?.ReloadData();
            didAbortDownload += DownloadAborted;
            didFinishDownloadingItem += UpdateDownloadingState;
        }

        internal void EnqueueSong(BeatSaverSharp.Models.Beatmap song, Sprite cover)
        {
            DownloadQueueItem queuedSong = new DownloadQueueItem(song, cover);
            queueItems.Add(queuedSong);
            Misc.SongDownloader.Instance.QueuedDownload(song.LatestVersion.Hash.ToUpper());
            _downloadList?.tableView?.ReloadData();
            UpdateDownloadingState(queuedSong);
        }
        internal void AbortAllDownloads()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            foreach (DownloadQueueItem item in queueItems.ToArray())
            {
                item.AbortDownload();
            }
        }
        internal async void EnqueueSongs(Tuple<BeatSaverSharp.Models.Beatmap, Sprite>[] songs, CancellationToken cancellationToken)
        {

            for (int i = 0; i < songs.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                bool downloaded = false;
                Tuple<BeatSaverSharp.Models.Beatmap, Sprite> pair = songs[i];
                BeatSaverSharp.Models.Beatmap map = pair.Item1;
                /*if (map.Partial)
                {
                    downloaded = SongDownloader.Instance.IsSongDownloaded(map.Hash);
                    if (downloaded) continue;
                    try
                    {
                        await map.Populate();
                    }
                    catch(BeatSaverSharp.Exceptions.InvalidPartialException ex)
                    {
                        Plugin.log.Warn("Map not found on BeatSaver");
                            continue;
                    }
                }*/
                bool inQueue = queueItems.Any(x => (x as DownloadQueueItem).beatmap == map);
                downloaded = SongDownloader.Instance.IsSongDownloaded(map.LatestVersion.Hash);
                if (!inQueue & !downloaded) EnqueueSong(map, pair.Item2);
            }
        }
        internal void UpdateDownloadingState(DownloadQueueItem item)
        {
            foreach (DownloadQueueItem inQueue in queueItems.Where(x => (x as DownloadQueueItem).queueState == SongQueueState.Queued).ToArray())
            {
                if (Misc.PluginConfig.maxSimultaneousDownloads > queueItems.Where(x => (x as DownloadQueueItem).queueState == SongQueueState.Downloading).ToArray().Length)
                    inQueue.Download();
            }
            foreach (DownloadQueueItem downloaded in queueItems.Where(x => (x as DownloadQueueItem).queueState == SongQueueState.Downloaded).ToArray())
            {
                queueItems.Remove(downloaded);
                _downloadList?.tableView?.ReloadData();
            }
            if (queueItems.Count == 0)
                SongCore.Loader.Instance.RefreshSongs(false);
        }

        internal void DownloadAborted(DownloadQueueItem download)
        {
            if (queueItems.Contains(download))
                queueItems.Remove(download);

            if (queueItems.Count == 0)
                SongCore.Loader.Instance.RefreshSongs(false);
            _downloadList?.tableView?.ReloadData();
        }
    }

    internal class DownloadQueueItem : INotifyPropertyChanged
    {
        public SongQueueState queueState = SongQueueState.Queued;
        internal Progress<double> downloadProgress;
        internal BeatSaverSharp.Models.Beatmap beatmap;
        private UnityEngine.UI.Image _bgImage;
        private float _downloadingProgess;
        internal CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
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
            cancellationTokenSource.Cancel();
            DownloadQueueViewController.didAbortDownload?.Invoke(this);
        }

        private string _songName;
        private string _authorName;
        private Sprite _cover;

        public DownloadQueueItem()
        {
        }

        public DownloadQueueItem(BeatSaverSharp.Models.Beatmap song, Sprite cover)
        {
            beatmap = song;
            _songName = song.Metadata.SongName;
            _cover = cover;
            _authorName = $"{song.Metadata.SongAuthorName} <size=80%>[{song.Metadata.LevelAuthorName}]";
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
            downloadProgress = new Progress<double>(ProgressUpdate);

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

        public async void Download()
        {
            queueState = SongQueueState.Downloading;
            await SongDownloader.Instance.DownloadSong(beatmap, cancellationTokenSource.Token, downloadProgress);
            queueState = SongQueueState.Downloaded;
            DownloadQueueViewController.didFinishDownloadingItem?.Invoke(this);
        }

        public void Update()
        {
        }
    }
}