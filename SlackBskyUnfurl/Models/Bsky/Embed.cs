namespace SlackBskyUnfurl.Models.Bsky
{
    public class Embed : RecordWithMedia
    {
        public External External { get; set; }
        public IEnumerable<ImageEmbed> Images { get; set; }
    }
}
