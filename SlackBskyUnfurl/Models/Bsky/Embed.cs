using Newtonsoft.Json;

namespace SlackBskyUnfurl.Models.Bsky
{
    public class Embed : RecordWithMedia
    {
        [JsonProperty("$type")]
        public string Type { get; set; }
        public AspectRatio? AspectRatio { get; set; }
        public External External { get; set; }
        public IEnumerable<ImageEmbed>? Images { get; set; }

        public VideoEmbed? Video { get; set; }
    }
}
