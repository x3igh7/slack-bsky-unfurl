using Microsoft.EntityFrameworkCore;

namespace SlackBskyUnfurl.Data.Models
{
    [PrimaryKey(nameof(Id))]
    public class AuthorizedWorkspaceEntity
    {
        public Guid Id { get; set; }
        public string TeamId { get; set; }
        public string AccessToken { get; set; }
    }
}
