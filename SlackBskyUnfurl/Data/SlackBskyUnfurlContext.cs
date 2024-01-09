using Microsoft.EntityFrameworkCore;

namespace SlackBskyUnfurl.Data
{
    public class SlackBskyUnfurlContext : DbContext
    {
        public SlackBskyUnfurlContext(DbContextOptions<SlackBskyUnfurlContext> options)
            : base(options) {
        }
    }
}
