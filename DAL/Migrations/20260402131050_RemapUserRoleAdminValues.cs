using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class RemapUserRoleAdminValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski: Developer=1, DBA=2, Admin=3 → Yeni: Developer=1, Admin=2
            migrationBuilder.Sql("UPDATE Users SET Role = 2 WHERE Role = 3;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınamaz: eski Admin (3) ile yeni Admin (2) ayrılamaz.
        }
    }
}
