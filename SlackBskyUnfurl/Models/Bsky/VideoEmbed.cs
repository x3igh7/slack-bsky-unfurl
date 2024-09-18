using Newtonsoft.Json;

namespace SlackBskyUnfurl.Models.Bsky
{
    public class VideoEmbed {
        [JsonProperty("$type")]
        public string Type { get; set; }
        public Ref Ref {get; set; }
        public string MimeType {get; set; }
        public int Size { get; set; }
    }
}
