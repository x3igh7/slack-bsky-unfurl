namespace SlackBskyUnfurl.Models.Bsky
{
    public class Embed
    {
        public External External { get; set; }
        public IEnumerable<ImageEmbed> Images { get; set; }
        public Record Record { get; set; }
        public RecordWithMedia RecordWithMedia { get; set; }
    }
}
