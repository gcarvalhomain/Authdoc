using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authdoc.Src.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenConcurrencyCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No table changes: it only saves the RevokedAt concurrency check in the model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
