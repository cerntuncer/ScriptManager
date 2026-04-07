using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class BatchHierarchy_ReleaseRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RootBatchId",
                table: "Releases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ParentBatchId",
                table: "Batches",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Releases_RootBatchId",
                table: "Releases",
                column: "RootBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_ParentBatchId",
                table: "Batches",
                column: "ParentBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Batches_ParentBatchId",
                table: "Batches",
                column: "ParentBatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Releases_Batches_RootBatchId",
                table: "Releases",
                column: "RootBatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Batches_ReleaseId_Name ON Batches;");

            migrationBuilder.Sql(
                """
                UPDATE r SET r.RootBatchId = (
                    SELECT TOP 1 b.Id FROM Batches b
                    WHERE b.ReleaseId = r.Id AND b.ParentBatchId IS NULL AND b.IsDeleted = 0
                    ORDER BY b.Id)
                FROM Releases r
                WHERE r.IsDeleted = 0
                  AND EXISTS (SELECT 1 FROM Batches b WHERE b.ReleaseId = r.Id AND b.IsDeleted = 0);
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IX_Batches_Release_Parent_Name
                ON Batches (ReleaseId, ParentBatchId, [Name])
                WHERE ReleaseId IS NOT NULL AND IsDeleted = 0;
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IX_Batches_Orphan_Parent_Name
                ON Batches (ParentBatchId, [Name])
                WHERE ReleaseId IS NULL AND ParentBatchId IS NOT NULL AND IsDeleted = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Batches_Orphan_Parent_Name ON Batches;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Batches_Release_Parent_Name ON Batches;");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Batches_ParentBatchId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Releases_Batches_RootBatchId",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Releases_RootBatchId",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Batches_ParentBatchId",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "RootBatchId",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ParentBatchId",
                table: "Batches");

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IX_Batches_ReleaseId_Name
                ON Batches (ReleaseId, [Name])
                WHERE ReleaseId IS NOT NULL AND IsDeleted = 0;
                """);
        }
    }
}
