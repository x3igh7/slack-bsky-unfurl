namespace SlackBskyUnfurl.Models.Bsky
{
    public class Media {
        public string Uri { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Blob Thumb { get; set; }
        public Blob Image { get; set; }
        public string Alt { get; set; }
    }
}
