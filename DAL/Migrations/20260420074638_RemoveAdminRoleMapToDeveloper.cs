using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdminRoleMapToDeveloper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski Admin (2) artık yok; tüm kayıtlar Geliştirici (1) olarak devam eder.
            migrationBuilder.Sql("UPDATE Users SET Role = 1 WHERE Role = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınamaz: hangi kullanıcının önceden Admin olduğu ayırt edilemez.
        }
    }
}
