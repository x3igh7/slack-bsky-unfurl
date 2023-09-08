using Newtonsoft.Json;

namespace SlackBskyUnfurl.Models.Bsky
{
    public class Ref
    {
        [JsonProperty(PropertyName = "$link")]
        public string Link { get; set; }
    }
}
