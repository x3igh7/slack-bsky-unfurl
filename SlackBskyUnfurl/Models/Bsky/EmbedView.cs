using Newtonsoft.Json;

namespace SlackBskyUnfurl.Models.Bsky;

// Video properties are inherited from RecordWithMediaView
public class EmbedView : RecordWithMediaView {
    [JsonProperty("$type")]
    public string? Type { get; set; }
    public IEnumerable<ImageView>? Images { get; set; }
    public ExternalView? External { get; set; }
}