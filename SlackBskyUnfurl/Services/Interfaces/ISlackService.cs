using System.Text.Json;
using Newtonsoft.Json.Linq;
using SlackBskyUnfurl.Models.Slack;
using SlackNet.Events;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface ISlackService {
        Task<bool> SaveAccessToken(ScopeResponse scopeResponse);
        Task<string> HandleVerification(UrlVerification slackEvent);
        Task HandleIncomingEvent(JsonElement dynamicSlackEvent);
        Task HandleLinkSharedAsync(Models.Slack.LinkShared message);
    }
}
