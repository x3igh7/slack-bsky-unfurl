using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlackBskyUnfurl.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnTeamId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TeamId",
                table: "AuthorizedWorkspaces",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizedWorkspaces_TeamId",
                table: "AuthorizedWorkspaces",
                column: "TeamId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuthorizedWorkspaces_TeamId",
                table: "AuthorizedWorkspaces");

            migrationBuilder.AlterColumn<string>(
                name: "TeamId",
                table: "AuthorizedWorkspaces",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
