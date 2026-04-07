using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class Batch_ReleaseId_Nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Batches_ReleaseId_Name",
                table: "Batches");

            migrationBuilder.AlterColumn<long>(
                name: "ReleaseId",
                table: "Batches",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_ReleaseId",
                table: "Batches",
                column: "ReleaseId");

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IX_Batches_ReleaseId_Name
                ON Batches (ReleaseId, [Name])
                WHERE ReleaseId IS NOT NULL AND IsDeleted = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS IX_Batches_ReleaseId_Name ON Batches;");

            migrationBuilder.DropIndex(
                name: "IX_Batches_ReleaseId",
                table: "Batches");

            migrationBuilder.AlterColumn<long>(
                name: "ReleaseId",
                table: "Batches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_ReleaseId_Name",
                table: "Batches",
                columns: new[] { "ReleaseId", "Name" },
                unique: true);
        }
    }
}
