using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using BeatSaverDownloader.Misc;

namespace BeatSaverDownloader.Bookmarks
{
    internal sealed class OauthConfig
    {
        private static readonly OauthConfig Empty = new OauthConfig("", "https://fake.beatsaver.com", "", "", "https://fake.beatsaver.com", null);
        private static readonly Dictionary<string, OauthConfig> Configs = new Dictionary<string, OauthConfig>();

        public static OauthConfig Current
        {
            get
            {
                if (Configs.Count == 0)
                {
                    try
                    {
                        var secrets = Crypto.ExtractSecrets(Sprites.AddToFavorites.texture).Split(',');

                        if (secrets[0].Length > 0)
                        {
                            Configs["LOCAL"] = new OauthConfig(
                                "http://localhost:8080/oauth2/authorize",
                                "http://localhost:8080/api/oauth2/token",
                                "BeatSaverDownloader",
                                secrets[0],
                                "http://localhost:8080/api/",
                                secrets[1].Length > 0 ? secrets[1] : null
                            );
                        }

                        if (secrets[2].Length > 0)
                        {
                            Configs["STAGE"] = new OauthConfig(
                                "https://stg.beatsaver.com/oauth2/authorize",
                                "https://stg.beatsaver.com/api/oauth2/token",
                                "BeatSaverDownloader",
                                secrets[2],
                                "https://stg.beatsaver.com/api/",
                                secrets[3].Length > 0 ? secrets[3] : null
                            );
                        }

                        if (secrets[4].Length > 0)
                        {
                            Configs["PROD"] = new OauthConfig(
                                "https://beatsaver.com/oauth2/authorize",
                                "https://beatsaver.com/api/oauth2/token",
                                "BeatSaverDownloader",
                                secrets[4],
                                "https://beatsaver.com/api/",
                                secrets[5].Length > 0 ? secrets[5] : null
                            );
                        }
                    }
                    catch (Exception)
                    {
                        Plugin.LOG.Error("Error loading Oauth config");
                        Configs["ERROR"] = Empty;
                        // ERROR loading secrets
                    }
                }

                return Configs.TryGetValue(PluginConfig.OauthEnvironment, out var res) ? res : Configs.Values.First();
            }
        }

        public static List<object> Options => Configs.Keys.ToList<object>();

        public const string Scopes = "bookmarks";

        private OauthConfig(string authUrl, string tokenUrl, string clientId, string clientSecret, string apiBase, string appAuth)
        {
            AuthUrl = authUrl;
            TokenUrl = tokenUrl;
            ClientId = clientId;
            ClientSecret = clientSecret;
            ApiBase = apiBase;
            AppAuth = appAuth;
        }

        public string AuthUrl { get; }
        public string TokenUrl { get; }
        public string ClientId { get; }
        // Don't @ me, this isn't your bank
        public string ClientSecret { get; }
        public string ApiBase { get; }
        public string AppAuth { get; }

        public void CustomiseRequest(HttpRequestMessage req)
        {
            if (AppAuth != null)
            {
                req.Headers.Add("X-App-Auth", AppAuth);
            }
        }
    }
}
