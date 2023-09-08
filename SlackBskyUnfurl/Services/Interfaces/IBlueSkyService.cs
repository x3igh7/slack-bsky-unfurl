using SlackBskyUnfurl.Models.Bsky.Responses;

namespace SlackBskyUnfurl.Services.Interfaces
{
    public interface IBlueSkyService {
        Task<GetPostThreadResponse> HandleGetPostThreadRequest(string url);
    }
}
