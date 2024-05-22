using System.Text.Json;
using Newtonsoft.Json.Linq;
using SlackBskyUnfurl.Models.Slack;
using SlackNet.Events;
using SlackNet.WebApi;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface ISlackService {
        Task<bool> SaveAccessToken(OauthV2AccessResponse scopeResponse);
        Task<string> HandleVerification(UrlVerification slackEvent);
        Task HandleIncomingEvent(JsonElement dynamicSlackEvent);
        Task HandleLinkSharedAsync(Models.Slack.LinkShared message);
    }
}
