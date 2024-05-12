using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.ViewControllers.DownloadQueue;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using BeatSaverSharp.Models.Pages;
using IPA.Utilities;
using Newtonsoft.Json;

namespace BeatSaverDownloader.Bookmarks
{
    internal class BookmarksApi
    {
        private static readonly string BookmarkedSongsPath = $"{UnityGame.UserDataPath}/bookmarkedSongs.json";

        private const int PageSize = 100;
        private readonly HttpClient _bookmarksClient;
        private readonly TokenApi _tokenApi;
        private HashSet<string> _bookmarkHashes = new HashSet<string>();
        private readonly QueueManager _queueManager;

        public BookmarksApi(TokenApi tokenApi, QueueManager queueManager)
        {
            _tokenApi = tokenApi;
            _queueManager = queueManager;

            _bookmarksClient = new HttpClient();
            _bookmarksClient.BaseAddress = new Uri(OauthConfig.Current.ApiBase, UriKind.Absolute);
            _bookmarksClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Plugin.UserAgent);

            if (File.Exists(BookmarkedSongsPath))
            {
                _bookmarkHashes = JsonConvert.DeserializeObject<HashSet<string>>(File.ReadAllText(BookmarkedSongsPath, Encoding.UTF8));
            }
        }

        public void Store()
        {
            File.WriteAllText(BookmarkedSongsPath, JsonConvert.SerializeObject(_bookmarkHashes), Encoding.UTF8);
        }

        public async Task Sync(bool interactive, Func<Task> cb = null)
        {
            try
            {
                var bookmarks = await GetBookmarks(interactive, cb ?? (() => Task.CompletedTask));
                _bookmarkHashes = bookmarks.Select(x => x.LatestVersion.Hash.ToUpper()).ToHashSet();

                var toDownload = bookmarks.Where(b => !SongDownloader.IsSongDownloaded(b.LatestVersion.Hash.ToUpper())).ToList();
                Plugin.LOG.Info($"Got {bookmarks.Count} bookmarks. {toDownload.Count} to download");

                foreach (var beatmap in toDownload)
                {
                    var icon = Sprites.RemoveFromFavorites;

                    try
                    {
                        var image = await beatmap.LatestVersion.DownloadCoverImage();
                        icon = Sprites.LoadSpriteRaw(image);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    _queueManager.AddToQueue(beatmap, icon);
                }
            }
            catch (TokenApi.InvalidOauthCredentialsException e)
            {
                Plugin.LOG.Warn(e.Message);

                throw;
            }
            catch (Exception e)
            {
                Plugin.LOG.Critical(e);
            }
        }

        public bool IsBookmarked(BeatmapLevel customLevel)
        {
            if (!customLevel.levelID.StartsWith("custom_level_")) return false;

            var hash = SongCore.Utilities.Hashing.GetCustomLevelHash(customLevel);
            return IsBookmarked(hash);
        }

        private bool IsBookmarked(string hash) => _bookmarkHashes?.Contains(hash.ToUpper()) == true;

        private async Task<List<Beatmap>> GetBookmarks(bool interactive, Func<Task> cb)
        {
            bool morePages;
            var result = new List<Beatmap>();
            Page page = null;

            do
            {
                page = await (page?.Next() ?? GetBookmarksPage(interactive, cb));
                morePages = page != null && page.Beatmaps.Count >= PageSize;

                if (page != null)
                {
                    foreach (var pageBeatmap in page.Beatmaps)
                    {
                        ((BeatSaverObject) pageBeatmap).SetProperty("Client", Plugin.BeatSaver);
                        ((BeatSaverObject) pageBeatmap.LatestVersion).SetProperty("Client", Plugin.BeatSaver); 
                    }

                    result.AddRange(page.Beatmaps);
                }
            } while (morePages);

            return result;
        }

        private async Task<Page> GetBookmarksPage(bool interactive, Func<Task> cb, int page = 0, CancellationToken token = new CancellationToken())
        {
            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"bookmarks/{page}?pagesize={PageSize}", UriKind.Relative)
            };

            return await MakeRequest<Page>(req, cb, () => GetBookmarksPage(interactive, cb, page, token), async response =>
            {
                if (response == null) return null;

                var content = await response.Content.ReadAsStringAsync();
                return new BookmarkPage(JsonConvert.DeserializeObject<BookmarkResponse>(content), interactive, cb, page, this);
            }, token, interactive);
        }

        public async Task<bool?> SetBookmarkByKey(string key, bool bookmarked) => await MakeBookmarkRequest(BookmarkRequest.FromKey(key, bookmarked));
        public async Task<bool?> SetBookmarkByHash(string hash, bool bookmarked) => await MakeBookmarkRequest(BookmarkRequest.FromHash(hash, bookmarked));

        private async Task<bool?> MakeBookmarkRequest(BookmarkRequest bReq, CancellationToken token = new CancellationToken())
        {
            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("bookmark", UriKind.Relative),
                Content = new StringContent(JsonConvert.SerializeObject(bReq), Encoding.UTF8, "application/json")
            };

            return await MakeRequest(req, () => Task.CompletedTask, () => MakeBookmarkRequest(bReq, token), response =>
            {
                var result = response?.StatusCode == HttpStatusCode.NotFound ? (bool?) null : response?.IsSuccessStatusCode == true;

                if (bReq.Hash != null && result == true)
                {
                    if (bReq.Bookmarked)
                    {
                        _bookmarkHashes.Add(bReq.Hash);
                    }
                    else
                    {
                        _bookmarkHashes.Remove(bReq.Hash);
                    }
                }

                return Task.FromResult(result);
            }, token, false);
        }

        private async Task<T> MakeRequest<T>(HttpRequestMessage req, Func<Task> cb, Func<Task<T>> retryCallback, Func<HttpResponseMessage, Task<T>> completeCallback, CancellationToken token = new CancellationToken(), bool interactive = true)
        {
            try
            {
                if (PluginConfig.UserTokens?.CouldBeValid == true)
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", PluginConfig.UserTokens.AccessToken);
                    OauthConfig.Current.CustomiseRequest(req);

                    var response = await _bookmarksClient.SendAsync(req, token);

                    if (response.StatusCode != HttpStatusCode.Unauthorized)
                        return await completeCallback.Invoke(response).ConfigureAwait(false);

                    PluginConfig.UserTokens.Invalidate();
                }

                await _tokenApi.RefreshToken(cb, interactive);

                return await retryCallback.Invoke().ConfigureAwait(false);

            }
            catch (Exception e)
            {
                if (e is HttpRequestException || e is ExternalException)
                    return await completeCallback.Invoke(null).ConfigureAwait(false);

                throw;
            }
        }

        private class BookmarkPage : Page
        {
            private readonly int _pageNumber;
            private readonly Func<Task> _cb;
            private readonly BookmarksApi _api;
            private readonly bool _interactive;

            public BookmarkPage(BookmarkResponse res, bool interactive, Func<Task> cb, int page, BookmarksApi api) : base(res.Docs)
            {
                _interactive = interactive;
                _pageNumber = page;
                _api = api;
                _cb = cb;
            }

            public override Task<Page> Previous(CancellationToken token = default) => _pageNumber == 0 ? null : _api.GetBookmarksPage(_interactive, _cb, _pageNumber - 1, token);

            public override Task<Page> Next(CancellationToken token = default) => _api.GetBookmarksPage(_interactive, _cb, _pageNumber + 1, token);
        }

        internal class BookmarkResponse
        {
            [JsonProperty("docs")]
            public ReadOnlyCollection<Beatmap> Docs { get; internal set; }
        }

        internal class BookmarkRequest
        {
            [JsonProperty("key")]
            public readonly string Key;
            [JsonProperty("hash")]
            public readonly string Hash;
            [JsonProperty("bookmarked")]
            public readonly bool Bookmarked;

            private BookmarkRequest(string key, string hash, bool bookmarked)
            {
                Key = key;
                Hash = hash;
                Bookmarked = bookmarked;
            }

            public static BookmarkRequest FromKey(string key, bool bookmarked) => new BookmarkRequest(key, null, bookmarked);
            public static BookmarkRequest FromHash(string hash, bool bookmarked) => new BookmarkRequest(null, hash, bookmarked);
        }
    }
}