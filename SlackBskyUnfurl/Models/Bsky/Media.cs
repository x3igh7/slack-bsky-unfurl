namespace SlackBskyUnfurl.Models.Bsky
{
    public class Media
    {
        public Ref External { get; set; }
        public IEnumerable<ImageEmbed> Images { get; set; }
    }
}
