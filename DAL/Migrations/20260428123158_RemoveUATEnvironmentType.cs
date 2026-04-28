using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUATEnvironmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UAT (3) olanları Test (2) yap
            migrationBuilder.Sql("UPDATE TargetEnvironments SET EnvironmentType = 2 WHERE EnvironmentType = 3");
            // Prod (4) olanları yeni Prod (3) yap
            migrationBuilder.Sql("UPDATE TargetEnvironments SET EnvironmentType = 3 WHERE EnvironmentType = 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Prod (3) → eski Prod (4)
            migrationBuilder.Sql("UPDATE TargetEnvironments SET EnvironmentType = 4 WHERE EnvironmentType = 3");
            // Test (2) → UAT (3) — tam geri alınamaz ama en yakın değere döndür
            // (UAT kayıtları Test olarak kalmaya devam eder)
        }
    }
}
