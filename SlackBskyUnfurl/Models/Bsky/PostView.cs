namespace SlackBskyUnfurl.Models.Bsky; 

public class PostView {
    public string Uri { get; set; }
    public string Cid { get; set; }
    public Author Author { get; set; }
    public EmbedView? Embed { get; set; }
    public Post Record { get; set; }
    public int ReplyCount { get; set; }
    public int RepostCount { get; set; }
    public int LikeCount { get; set; }
}