namespace SlackBskyUnfurl.Models.Bsky
{
    public abstract class InvalidThreadPost
    {
        public string Uri { get; set; }
        public bool Blocked { get; set; }
        public bool NotFound { get; set; }
    }
}
