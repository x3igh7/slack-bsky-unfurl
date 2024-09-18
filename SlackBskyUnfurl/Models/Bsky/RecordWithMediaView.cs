namespace SlackBskyUnfurl.Models.Bsky
{
    public class RecordWithMediaView : VideoView
    {
        public RecordView? Record { get; set; }
        public MediaView? Media { get; set; }
    }
}
