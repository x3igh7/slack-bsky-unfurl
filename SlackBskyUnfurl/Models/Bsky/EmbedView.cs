namespace SlackBskyUnfurl.Models.Bsky;

public class EmbedView : RecordWithMediaView {
    public IEnumerable<ImageView>? Images { get; set; }
    public VideoView? Video { get; set; }
    public ExternalView? External { get; set; }
}