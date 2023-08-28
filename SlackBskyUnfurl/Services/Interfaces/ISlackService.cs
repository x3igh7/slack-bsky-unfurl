using SlackNet.Events;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface ISlackService
    {
        void HandleMessage(MessageEvent message);
    }
}
