namespace SlackBskyUnfurl.Models.Bsky; 

public class Post {
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public Embed? Embed { get; set; }
}