using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authdoc.Migrations
{
    /// <inheritdoc />
    public partial class changesValidationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatdAt",
                table: "Users",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Users",
                newName: "CreatdAt");
        }
    }
}
