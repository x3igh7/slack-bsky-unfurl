using Newtonsoft.Json;

namespace SlackBskyUnfurl.Models.Bsky; 

public class Feature {
    /// <summary>
    ///     The type of feature
    /// </summary>
    [JsonProperty("$type")]
    public string Type { get; set; }

    /// <summary>
    ///     URI for Links that aren't embeded
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    ///     DID for actor mentions
    /// </summary>
    public string Did { get; set; }
}