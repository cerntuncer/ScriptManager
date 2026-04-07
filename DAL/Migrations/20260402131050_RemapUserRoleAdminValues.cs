using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemapUserRoleAdminValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski: Developer=1, DBA=2, Admin=3 → Yeni: Developer=1, Admin=2
            migrationBuilder.Sql("UPDATE Users SET Role = 2 WHERE Role = 3;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınamaz: eski Admin (3) ile yeni Admin (2) ayrılamaz.
        }
    }
}
