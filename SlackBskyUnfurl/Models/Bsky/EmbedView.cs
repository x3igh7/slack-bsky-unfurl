namespace SlackBskyUnfurl.Models.Bsky; 

public class EmbedView {
    public IEnumerable<ImageView>? Images { get; set; }
    public ExternalView? External { get; set; }
    public RecordView? Record { get; set; }
}