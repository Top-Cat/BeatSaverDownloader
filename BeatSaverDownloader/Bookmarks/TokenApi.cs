using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using BeatSaverDownloader.Misc;
using Newtonsoft.Json;

namespace BeatSaverDownloader.Bookmarks
{
    internal class TokenApi
    {
        private readonly HttpClient _authClient;

        internal TokenApi(IPA.Loader.PluginMetadata metadata)
        {
            _authClient = new HttpClient();
            _authClient.BaseAddress = new Uri(OauthConfig.Current.TokenUrl, UriKind.Absolute);
            _authClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"BeatSaverDownlaoder/{metadata.HVersion}");
        }

        public async Task ExchangeCode(string code, string state)
        {
            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("", UriKind.Relative),
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", OauthConfig.Current.ClientId),
                    new KeyValuePair<string, string>("client_secret", OauthConfig.Current.ClientSecret),
                    new KeyValuePair<string, string>("redirect_uri", $"{CallbackListener.CallbackBase}cb"),
                    new KeyValuePair<string, string>("scope", OauthConfig.Scopes)
                })
            };

            OauthConfig.Current.CustomiseRequest(req);
            var response = await _authClient.SendAsync(req);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Plugin.LOG.Debug("Successfully exchanged code for access token");

                if (!_storedState.ContainsKey(state))
                {
                    throw new InvalidOauthCredentialsException("Invalid state in exchange");
                }

                PluginConfig.UserTokens = JsonConvert.DeserializeObject<OauthResponse>(json);
                PluginConfig.SaveConfig();

                await TriggerCallback(state);
            }
            else
            {
                throw new InvalidOauthCredentialsException("Code exchange failed");
            }
        }

        private readonly Dictionary<string, Func<Task>> _storedState = new Dictionary<string, Func<Task>>();

        private string GenerateState(Func<Task> cb)
        {
            var state = Guid.NewGuid().ToString();
            _storedState[state] = cb;

            return state;
        }

        private async Task TriggerCallback(string state)
        {
            if (_storedState.TryGetValue(state, out var cb))
                await cb();
        }

        public async Task RefreshToken(Func<Task> cb, bool interactive = true)
        {
            if (!string.IsNullOrEmpty(PluginConfig.UserTokens?.RefreshToken))
            {
                var req = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("", UriKind.Relative),
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", PluginConfig.UserTokens?.RefreshToken),
                        new KeyValuePair<string, string>("client_id", OauthConfig.Current.ClientId),
                        new KeyValuePair<string, string>("client_secret", OauthConfig.Current.ClientSecret)
                    })
                };

                OauthConfig.Current.CustomiseRequest(req);
                var response = await _authClient.SendAsync(req);

                if (response.IsSuccessStatusCode)
                {
                    Plugin.LOG.Debug("Successfully refreshed access token");

                    var json = await response.Content.ReadAsStringAsync();
                    PluginConfig.UserTokens = JsonConvert.DeserializeObject<OauthResponse>(json);
                    PluginConfig.SaveConfig();

                    return;
                }
            }

            // Don't try and open a new flow from automatated actions
            if (interactive)
            {
                Plugin.LOG.Debug("Launching Oauth flow");

                var state = GenerateState(cb);
                Process.Start($"{OauthConfig.Current.AuthUrl}" +
                              $"?client_id={OauthConfig.Current.ClientId}" +
                              $"&scope={OauthConfig.Scopes}" +
                              "&response_type=code" +
                              $"&state={state}" +
                              $"&redirect_uri={CallbackListener.CallbackBase}cb"
                );
            }

            throw new InvalidOauthCredentialsException("Oauth token invalid");
        }

        internal class InvalidOauthCredentialsException : Exception
        {
            public InvalidOauthCredentialsException(string message) : base(message) { }
        }
    }
}
