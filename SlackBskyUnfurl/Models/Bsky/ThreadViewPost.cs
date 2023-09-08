namespace SlackBskyUnfurl.Models.Bsky
{
    public class ThreadViewPost : InvalidThreadPost
    {
        public PostView Post { get; set; }
        public ThreadViewPost Parent { get; set; }
    }
}
