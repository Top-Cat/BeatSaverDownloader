using Newtonsoft.Json;

namespace BeatSaverDownloader.Bookmarks
{
    public class OauthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; private set; }
        [JsonProperty("token_type")]
        public readonly string TokenType;
        [JsonProperty("expires_in")]
        public readonly int ExpiresIn;
        [JsonProperty("refresh_token")]
        public readonly string RefreshToken;

        public bool CouldBeValid => !string.IsNullOrEmpty(AccessToken);

        public void Invalidate()
        {
            AccessToken = null;
        }

        public OauthResponse(string accessToken, string tokenType, int expiresIn, string refreshToken)
        {
            AccessToken = accessToken;
            TokenType = tokenType;
            ExpiresIn = expiresIn;
            RefreshToken = refreshToken;
        }
    }
}
