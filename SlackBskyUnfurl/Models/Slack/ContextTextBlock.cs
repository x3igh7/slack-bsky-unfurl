using SlackNet.Blocks;

namespace SlackBskyUnfurl.Models.Slack
{
    public class ContextTextBlock : TextObject, IContextElement
    {
        public ContextTextBlock(string type = "plain_text") : base(type) { }
    }
}
