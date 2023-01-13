using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BeatSaverDownloader.Misc;
using BeatSaverSharp.Models;
using UnityEngine;

namespace BeatSaverDownloader.UI.ViewControllers.DownloadQueue
{
    public class QueueManager
    {
        private List<QueueItem> _queue = new List<QueueItem>();

        public IEnumerable<QueueItem> QueueItems => _queue;

        public QueueItem AddToQueue(Beatmap beatmap, Sprite sprite)
        {
            if (SongDownloader.IsSongDownloaded(beatmap.LatestVersion.Hash.ToUpper()))
                return null;

            var item = new QueueItem(beatmap, sprite, SongQueueState.Queued);
            item.DownloadCancelled += UpdateState;
            item.DownloadCompleted += UpdateState;

            SongDownloader.Instance.QueuedDownload(beatmap.LatestVersion.Hash.ToUpper());
            _queue.Add(item);
            UpdateState();

            return item;
        }

        private void UpdateState()
        {
            var emptySpots = PluginConfig.MaxSimultaneousDownloads - _queue.Count(x => x.State == SongQueueState.Downloading);

            foreach (var queueItem in _queue
                         .Where(x => x.State == SongQueueState.Queued)
                         .Take(emptySpots))
            {
                queueItem.Download();
            }

            _queue.RemoveAll(item => item.State == SongQueueState.Downloaded || item.State == SongQueueState.Error);

            if (_queue.Count == 0)
                SongCore.Loader.Instance.RefreshSongs(false);
        }

        public class QueueItem
        {
            public event Action DownloadStarted;
            public event Action DownloadCompleted;
            public event Action DownloadCancelled;
            public event Action<double> DownloadProgress;

            public QueueItem(Beatmap beatmap, Sprite sprite, SongQueueState state)
            {
                Sprite = sprite;
                State = state;
                Beatmap = beatmap;
            }

            public Beatmap Beatmap { get; }
            public Sprite Sprite { get; }
            public SongQueueState State { get; private set; }
            private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

            public void Cancel()
            {
                _tokenSource.Cancel();
                State = SongQueueState.Error;
                DownloadCancelled?.Invoke();
            }

            internal async void Download()
            {
                State = SongQueueState.Downloading;
                DownloadStarted?.Invoke();

                await SongDownloader.Instance.DownloadSong(Beatmap, _tokenSource.Token, new Progress<double>(p => DownloadProgress?.Invoke(p)));

                State = SongQueueState.Downloaded;
                DownloadCompleted?.Invoke();
            }
        }
    }
}