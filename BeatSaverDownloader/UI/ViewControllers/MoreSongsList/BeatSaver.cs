using System;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Components;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaverDownloader.UI.ViewControllers.MoreSongsList
{
    public class BeatSaverCustomSongCellInfo : CustomListTableData.CustomCellInfo
    {
        private readonly BeatSaverSharp.Models.Beatmap _song;
        private readonly Action<CustomListTableData.CustomCellInfo> _callback;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private AudioClip _clip;

        public BeatSaverCustomSongCellInfo(BeatSaverSharp.Models.Beatmap song, Action<CustomListTableData.CustomCellInfo> callback, string text, string subtext = null) :
            base(text, subtext)
        {
            _song = song;
            _callback = callback;
            LoadImage();
        }

        private async void LoadImage()
        {
            var image = await _song.LatestVersion.DownloadCoverImage();
            icon = Misc.Sprites.LoadSpriteRaw(image);

            _callback(this);
        }

        private static async Task<bool> Download(UnityWebRequest www)
        {
            www.SetRequestHeader("User-Agent", Plugin.UserAgent);

            var req = www.SendWebRequest();
            var startThread = SynchronizationContext.Current;

            Action<Action> post;

            if (startThread == null)
            {
                post = a => a();
            }
            else
            {
                post = a => startThread.Send(_ => a(), null);
            }

            await Task.Run(() =>
            {
                var lastState = 0f;
                var timeouter = new System.Diagnostics.Stopwatch();
                timeouter.Start();

                while (!req.isDone)
                {
                    if (timeouter.ElapsedMilliseconds > 50000 || (lastState == 0 && timeouter.ElapsedMilliseconds > 6000))
                    {
                        post(www.Abort);
                        throw new TimeoutException();
                    }

                    Thread.Sleep(20);

                    lastState = www.downloadProgress;
                }
            });

            return www.isDone && !www.isHttpError && !www.isNetworkError;
        }

        internal async Task<AudioClip> LoadPreview()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                if (_clip == null)
                {
                    using (var www = UnityWebRequestMultimedia.GetAudioClip(_song.LatestVersion.PreviewURL, AudioType.UNKNOWN))
                    {
                        if (await Download(www))
                        {
                            if (www.downloadedBytes > 0)
                            {
                                _clip = DownloadHandlerAudioClip.GetContent(www);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If there's a problem loading audio we just don't play any
                // Plugin.log.Critical(e);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return _clip;
        }
    }
}
