using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaverDownloader.Misc;
using System;
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
        private QueueManager QueueManager;

        [UIValue("download-queue")]
        private readonly List<object> _queueItems = new List<object>();

        [UIComponent("download-list")]
        private CustomCellListTableData _downloadList;

        [UIAction("#post-parse")]
        internal void Setup()
        {
            DidAbortDownload += UpdateDownloadingState;
            DidFinishDownloadingItem += UpdateDownloadingState;

            Reload();
        }

        internal void AddQueueManager(QueueManager manager)
        {
            QueueManager = manager;
            _queueItems.AddRange(QueueManager.QueueItems.Select(item => new DownloadQueueItem(item)));
            Reload();
        }

        private void Reload()
        {
            if (_downloadList == null) return;
            if (_downloadList.tableView == null) return;

            _downloadList.tableView.ReloadData();
        }

        internal void EnqueueSong(Beatmap song, Sprite cover)
        {
            var queueItem = QueueManager.AddToQueue(song, cover);
            var queuedSong = new DownloadQueueItem(queueItem);

            _queueItems.Add(queuedSong);

            Reload();
        }

        internal void EnqueueSongs(IEnumerable<Tuple<Beatmap, Sprite>> songs)
        {
            foreach (var (map, sprite) in songs)
            {
                var inQueue = _queueItems.Any(x => (x as DownloadQueueItem)?.Beatmap == map);
                var downloaded = SongDownloader.IsSongDownloaded(map.LatestVersion.Hash);
                if (!inQueue & !downloaded) EnqueueSong(map, sprite);
            }
        }

        internal void AbortAllDownloads()
        {
            foreach (DownloadQueueItem item in _queueItems.ToArray())
            {
                item.AbortDownload();
            }
        }

        private void UpdateDownloadingState(DownloadQueueItem item)
        {
            _queueItems.Remove(item);

            Reload();
        }
    }
}