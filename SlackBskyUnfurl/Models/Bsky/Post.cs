using Newtonsoft.Json;

namespace SlackBskyUnfurl.Models.Bsky; 

public class Post {
    public string Type { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public Embed? Embed { get; set; }
    public IEnumerable<Facet>? Facets { get; set; }
}