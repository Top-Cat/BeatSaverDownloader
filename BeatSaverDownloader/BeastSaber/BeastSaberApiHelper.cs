using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace BeatSaverDownloader.BeastSaber
{
    public static class BeastSaberApiHelper
    {
        private static HttpClient _beastSaberRequestClient;
        public static string UserAgent;
        private static JsonSerializer _serializer = new JsonSerializer();

        internal static void InitializeBeastSaberHttpClient(IPA.Loader.PluginMetadata metadata)
        {
            UserAgent = $"BeatSaverDownloader/{metadata.HVersion}";
            _beastSaberRequestClient = new HttpClient() { BaseAddress = new Uri("https://bsaber.com/wp-json/bsaber-api/") };
            _beastSaberRequestClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        public static async Task<BeastSaberApiResult> GetPage(uint page, uint itemsPerPage, CancellationToken cancellationToken)
        {
            var apiUrl = $"songs?bookmarked_by=curatorrecommended&page={page + 1}&count={itemsPerPage}";
            try
            {
                var response = await _beastSaberRequestClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStreamAsync();
                var reader = new StreamReader(result);
                var jsonReader = new JsonTextReader(reader);
                return _serializer.Deserialize<BeastSaberApiResult>(jsonReader);
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Failed to get BeastSaber api page: {ex}");
                return new BeastSaberApiResult { songs = new List<BeastSaberSong>(), next_page = -1 };
            }


        }
    }
}
