namespace SlackBskyUnfurl.Models.Bsky
{
    public class VideoView
    {
        // video uri
        public string Playlist { get; set; }
        // thumbnail uri
        public string Thumbnail { get; set; }
        public string Alt { get; set; }
        public AspectRatio AspectRatio { get; set; }
    }
}
