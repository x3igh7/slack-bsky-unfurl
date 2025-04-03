using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SlackBskyUnfurl.Data.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(TeamId), IsUnique = true)]
public class AuthorizedWorkspaceEntity {
    public Guid Id { get; set; }

    [Required] public string TeamId { get; set; }

    [Required] public string AccessToken { get; set; }
}