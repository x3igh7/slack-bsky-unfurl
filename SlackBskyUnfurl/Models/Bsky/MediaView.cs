namespace SlackBskyUnfurl.Models.Bsky
{
    public class MediaView {
        public IEnumerable<ImageView>? Images { get; set; }

        public VideoView? Video { get; set; }
    }
}
