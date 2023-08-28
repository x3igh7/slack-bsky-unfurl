using SlackBskyUnfurl.Services.Interfaces;

namespace SlackBskyUnfurl.Services
{
    public class BlueSkyService : IBlueSkyService
    {
        public string HandleUnfurl(string url) {
            return "test";
        }
    }
}
