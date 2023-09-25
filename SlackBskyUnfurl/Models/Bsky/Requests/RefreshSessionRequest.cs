namespace SlackBskyUnfurl.Models.Bsky.Requests
{
    public class RefreshSessionRequest
    {
        public string AccessJwt { get; set; }
        public string RefreshJwt { get; set; }
        public string Handle { get; set; }
        public string Did { get; set; }
    }
}
