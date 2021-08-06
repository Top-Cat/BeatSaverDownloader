using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    public class SongDownloader : MonoBehaviour
    {
        public event Action<BeatSaverSharp.Models.Beatmap> songDownloaded;

        private static SongDownloader _instance = null;

        public static SongDownloader Instance
        {
            get
            {
                if (!_instance)
                    _instance = new GameObject("SongDownloader").AddComponent<SongDownloader>();
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private HashSet<string> _alreadyDownloadedSongs;
        private static bool _extractingZip;

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (!SongCore.Loader.AreSongsLoaded)
            {
                SongCore.Loader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            }
            else
            {
                SongLoader_SongsLoadedEvent(null, SongCore.Loader.CustomLevels);
            }
        }

        private void SongLoader_SongsLoadedEvent(SongCore.Loader sender, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            Plugin.log.Debug("Establishing Already Downloaded Songs");
            _alreadyDownloadedSongs = new HashSet<string>(levels.Values.Select(x => SongCore.Collections.hashForLevelID(x.levelID)));
        }

        public async Task DownloadSong(BeatSaverSharp.Models.Beatmap song, System.Threading.CancellationToken token, IProgress<double> progress = null, bool direct = false)
        {
            try
            {
                string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
                if (!Directory.Exists(customSongsPath))
                {
                    Directory.CreateDirectory(customSongsPath);
                }
                var zip = await song.LatestVersion.DownloadZIP(token, progress);
                Plugin.log.Info("Downloaded zip!");
                await ExtractZipAsync(song, zip, customSongsPath).ConfigureAwait(false);
                songDownloaded?.Invoke(song);

            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    Plugin.log.Warn("Song Download Aborted.");
                else
                    Plugin.log.Critical("Failed to download Song!");
                if (_alreadyDownloadedSongs.Contains(song.LatestVersion.Hash.ToUpper()))
                    _alreadyDownloadedSongs.Remove(song.LatestVersion.Hash.ToUpper());
            }
        }

        private async Task ExtractZipAsync(BeatSaverSharp.Models.Beatmap songInfo, byte[] zip, string customSongsPath, bool overwrite = false)
        {
            Stream zipStream = new MemoryStream(zip);
            try
            {
                Plugin.log.Info("Extracting...");
                _extractingZip = true;
                ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                string basePath = songInfo.ID + " (" + songInfo.Metadata.SongName + " - " + songInfo.Metadata.LevelAuthorName + ")";
                basePath = string.Join("", basePath.Split((Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray())));
                string path = customSongsPath + "/" + basePath;
                if (!overwrite && Directory.Exists(path))
                {
                    int pathNum = 1;
                    while (Directory.Exists(path + $" ({pathNum})")) ++pathNum;
                    path += $" ({pathNum})";
                }
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                Plugin.log.Info(path);
                await Task.Run(() =>
                {
                    foreach (var entry in archive.Entries)
                    {
                        var entryPath = Path.Combine(path, entry.Name); // Name instead of FullName for better security and because song zips don't have nested directories anyway
                        if (overwrite || !File.Exists(entryPath)) // Either we're overwriting or there's no existing file
                            entry.ExtractToFile(entryPath, overwrite);
                    }
                }).ConfigureAwait(false);
                archive.Dispose();
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to extract ZIP! Exception: {e}");
                _extractingZip = false;
                return;
            }
            zipStream.Close();
        }

        public void QueuedDownload(string hash)
        {
            if (!Instance._alreadyDownloadedSongs.Contains(hash))
                Instance._alreadyDownloadedSongs.Add(hash);
        }

        public bool IsSongDownloaded(string hash)
        {
            return Instance._alreadyDownloadedSongs.Contains(hash.ToUpper());
        }
    }
}