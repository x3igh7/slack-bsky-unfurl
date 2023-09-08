using SlackNet.Events;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface ISlackService {
        Task HandleIncomingEvent(dynamic dynamicSlackEvent, string jsonEvent);
        Task HandleLinkSharedAsync(LinkShared message);
    }
}
