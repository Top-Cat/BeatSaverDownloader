using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaverDownloader.Misc;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeatSaverDownloader.UI.ViewControllers.DownloadQueue;
using BeatSaverSharp.Models;

namespace BeatSaverDownloader.UI.ViewControllers
{
    public class DownloadQueueViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.downloadQueue.bsml";
        internal static Action<DownloadQueueItem> DidAbortDownload;
        internal static Action<DownloadQueueItem> DidFinishDownloadingItem;
        internal CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        [UIValue("download-queue")]
        private readonly List<object> _queueItems = new List<object>();

        [UIComponent("download-list")]
        private CustomCellListTableData _downloadList;

        [UIAction("#post-parse")]
        internal void Setup()
        {
            Reload();
            DidAbortDownload += DownloadAborted;
            DidFinishDownloadingItem += UpdateDownloadingState;
        }

        private void Reload()
        {
            if (_downloadList == null) return;
            if (_downloadList.tableView == null) return;

            _downloadList.tableView.ReloadData();
        }

        internal void EnqueueSong(Beatmap song, Sprite cover)
        {
            var queuedSong = new DownloadQueueItem(song, cover);
            _queueItems.Add(queuedSong);
            SongDownloader.Instance.QueuedDownload(song.LatestVersion.Hash.ToUpper());
            Reload();
            UpdateDownloadingState(queuedSong);
        }

        internal void AbortAllDownloads()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            foreach (DownloadQueueItem item in _queueItems.ToArray())
            {
                item.AbortDownload();
            }
        }

        internal void EnqueueSongs(IEnumerable<Tuple<Beatmap, Sprite>> songs, CancellationToken cancellationToken)
        {
            foreach (var (map, sprite) in songs)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var inQueue = _queueItems.Any(x => (x as DownloadQueueItem)?.beatmap == map);
                var downloaded = SongDownloader.IsSongDownloaded(map.LatestVersion.Hash);
                if (!inQueue & !downloaded) EnqueueSong(map, sprite);
            }
        }

        private void UpdateDownloadingState(DownloadQueueItem item)
        {
            foreach (DownloadQueueItem inQueue in _queueItems.Where(x => (x as DownloadQueueItem)?.queueState == SongQueueState.Queued).ToArray())
            {
                if (PluginConfig.MaxSimultaneousDownloads > _queueItems.Where(x => (x as DownloadQueueItem)?.queueState == SongQueueState.Downloading).ToArray().Length)
                    inQueue.Download();
            }
            foreach (DownloadQueueItem downloaded in _queueItems.Where(x => (x as DownloadQueueItem)?.queueState == SongQueueState.Downloaded).ToArray())
            {
                _queueItems.Remove(downloaded);
                Reload();
            }
            if (_queueItems.Count == 0)
                SongCore.Loader.Instance.RefreshSongs(false);
        }

        private void DownloadAborted(DownloadQueueItem download)
        {
            if (_queueItems.Contains(download))
                _queueItems.Remove(download);

            if (_queueItems.Count == 0)
                SongCore.Loader.Instance.RefreshSongs(false);

            Reload();
        }
    }
}