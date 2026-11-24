using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authdoc.Src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenRevokeReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RevokeReason",
                table: "RefreshTokens",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevokeReason",
                table: "RefreshTokens");
        }
    }
}
