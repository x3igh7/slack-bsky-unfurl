using Microsoft.EntityFrameworkCore;
using SlackBskyUnfurl.Data.Models;

namespace SlackBskyUnfurl.Data
{
    public class SlackBskyContext : DbContext
    {
        public SlackBskyContext(DbContextOptions<SlackBskyContext> options)
            : base(options) {
        }

        public virtual DbSet<AuthorizedWorkspaceEntity> AuthorizedWorkspaces { get; set; }
    }
}
