using Newtonsoft.Json;
using SlackNet;

namespace SlackBskyUnfurl.Models.Slack
{
    public class ScopeResponse
    {
        public bool Ok { get; set; }
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        public string Scope { get; set; }
        [JsonProperty("bot_user_id")]
        public string BotUserId { get; set; }
        [JsonProperty("app_id")]
        public string AppId { get; set; }
        public Team Team { get; set; }
        [JsonProperty("authed_user")]
        public AuthedUser AuthedUser { get; set; }
    }
}
