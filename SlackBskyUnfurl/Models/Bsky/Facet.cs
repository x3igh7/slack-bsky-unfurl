namespace SlackBskyUnfurl.Models.Bsky
{
    public class Facet
    {
        public ByteSlice Index { get; set; }
        public IEnumerable<Feature> Features { get; set; }
    }
}
