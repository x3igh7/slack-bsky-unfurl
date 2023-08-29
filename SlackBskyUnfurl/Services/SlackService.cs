using SlackBskyUnfurl.Services.Interfaces;
using SlackNet;
using SlackNet.Events;
using System.Text.RegularExpressions;
using SlackNet.WebApi;

namespace SlackBskyUnfurl.Services
{
    public class SlackService : ISlackService {
        public ISlackApiClient Client;
        private readonly IBlueSkyService _blueSky;

        public SlackService(IBlueSkyService blueSkyService, IConfiguration configuration) {
            this._blueSky = blueSkyService;

            var clientSecret = configuration["SlackClientSecret"];
            var signingSecret = configuration["SlackSigningSecret"];
            this.Client = new SlackServiceBuilder().UseApiToken(clientSecret).GetApiClient();
        }

        public async void HandleMessageAsync(MessageEvent message) {
            if (!message.Text.Contains("bsky.app")) {
                return;
            }

            var parser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = parser.Matches(message.Text);
            foreach (Match match in matches) {
                var url = match.Value;
                var unfurlResult = this._blueSky.HandleUnfurl(url);
                this.Client.Chat.PostMessage(new Message {
                    Channel = message.Channel,
                    Text = unfurlResult,
                });
            }
        }
    }
}
