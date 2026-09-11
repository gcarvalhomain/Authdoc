using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authdoc.Migrations
{
    /// <inheritdoc />
    public partial class ColumnConfirmationPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmationPassword",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmationPassword",
                table: "Users");
        }
    }
}
