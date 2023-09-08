namespace SlackBskyUnfurl.Models.Bsky.Responses
{
    public class SessionResponse
    {
        /// <summary>
        /// The JWT to use for authentication
        /// </summary>
        public string AccessJwt { get; set; }

        /// <summary>
        /// The JWT to use for refreshing the access token
        /// </summary>
        public string RefreshJwt { get; set; }

        /// <summary>
        /// The BlueSky user's handle
        /// </summary>
        public string Handle { get; set; }

        /// <summary>
        /// The BlueSky user's unique identifier
        /// </summary>
        public string Did { get; set; }
    }
}
