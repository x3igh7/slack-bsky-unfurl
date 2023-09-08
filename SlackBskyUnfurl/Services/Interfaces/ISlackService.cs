using SlackNet.Events;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface ISlackService {
        Task HandleIncomingEvent(dynamic slackEvent);
        Task HandleLinkSharedAsync(LinkShared message);
    }
}
