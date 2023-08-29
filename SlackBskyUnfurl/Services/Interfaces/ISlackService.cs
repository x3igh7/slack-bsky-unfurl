using SlackNet.Events;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface ISlackService
    {
        void HandleMessageAsync(MessageEvent message);
    }
}
