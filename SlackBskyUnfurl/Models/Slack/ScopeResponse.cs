using SlackNet;

namespace SlackBskyUnfurl.Models.Slack
{
    public class ScopeResponse
    {
        public bool Ok { get; set; }
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string Scope { get; set; }
        public string BotUserId { get; set; }
        public string AppId { get; set; }
        public Team Team { get; set; }
        public AuthedUser AuthedUser { get; set; }
    }
}
