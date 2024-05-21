using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlackBskyUnfurl.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                table: "AuthorizedWorkspaces",
                newName: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "AuthorizedWorkspaces",
                newName: "WorkspaceId");
        }
    }
}
