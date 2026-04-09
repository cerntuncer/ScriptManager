using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseIsCancelled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Releases_Version",
                table: "Releases");

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Releases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Releases_Version",
                table: "Releases",
                column: "Version",
                unique: true,
                filter: "[IsCancelled] = CAST(0 AS bit) AND [IsDeleted] = CAST(0 AS bit)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Releases_Version",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Releases");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_Version",
                table: "Releases",
                column: "Version",
                unique: true);
        }
    }
}
