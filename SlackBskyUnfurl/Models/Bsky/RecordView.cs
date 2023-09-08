namespace SlackBskyUnfurl.Models.Bsky; 

public class RecordView {
    public string Uri { get; set; }
    public string Cid { get; set; }
    public Author Author { get; set; }
    public Post Value { get; set; }
    public IEnumerable<EmbedView> Embeds { get; set; }
}