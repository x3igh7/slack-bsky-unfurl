using System.Text.Json;
using Newtonsoft.Json.Linq;
using SlackNet.Events;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface ISlackService {
        Task<string> HandleVerification(UrlVerification slackEvent);
        Task HandleIncomingEvent(JsonElement dynamicSlackEvent);
        Task HandleLinkSharedAsync(LinkShared message);
    }
}
