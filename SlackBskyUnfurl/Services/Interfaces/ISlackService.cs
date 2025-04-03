using System.Text.Json;
using SlackBskyUnfurl.Models.Slack;
using SlackNet.Events;
using LinkShared = SlackBskyUnfurl.Models.Slack.LinkShared;

namespace SlackBskyUnfurl.Services.Interfaces;

public interface ISlackService {
    Task<bool> SaveAccessToken(ScopeResponse scopeResponse);
    Task<string> HandleVerification(UrlVerification slackEvent);
    Task HandleIncomingEvent(JsonElement dynamicSlackEvent);
    Task HandleLinkSharedAsync(LinkShared message);
}