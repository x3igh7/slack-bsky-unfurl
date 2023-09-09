using Newtonsoft.Json;
using SlackNet.Events;

namespace SlackBskyUnfurl.Models.Slack
{
    public class LinkShared : Event
    {
        public string Channel { get; set; }

        public string User { get; set; }

        [JsonProperty("message_ts")]
        public string MessageTs { get; set; }

        public string UnfurlId { get; set; }

        public IList<SharedLink> Links { get; set; } = (IList<SharedLink>)new List<SharedLink>();
    }
}
