using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class MergeConflictDismissalsIntoConflicts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SqlFingerprintMax",
                table: "Conflicts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SqlFingerprintMin",
                table: "Conflicts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO [Conflicts] (
                    [ScriptId], [ConflictingScriptId], [TableName], [Severity], [DetectedAt],
                    [ResolvedBy], [ResolvedAt], [ResolutionKind],
                    [CreatedAt], [UpdatedAt], [IsDeleted],
                    [SqlFingerprintMin], [SqlFingerprintMax]
                )
                SELECT
                    [ScriptIdMin], [ScriptIdMax], N'', 1, [CreatedAt],
                    [ResolvedByUserId], [CreatedAt], [ResolutionKind],
                    [CreatedAt], NULL, 0,
                    [SqlFingerprintMin], [SqlFingerprintMax]
                FROM [ConflictPairDismissals]
                WHERE [IsDeleted] = 0;
                """);

            migrationBuilder.DropTable(name: "ConflictPairDismissals");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConflictPairDismissals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResolvedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ScriptIdMax = table.Column<long>(type: "bigint", nullable: false),
                    ScriptIdMin = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionKind = table.Column<int>(type: "int", nullable: true),
                    SqlFingerprintMax = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SqlFingerprintMin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictPairDismissals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConflictPairDismissals_Scripts_ScriptIdMax",
                        column: x => x.ScriptIdMax,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConflictPairDismissals_Scripts_ScriptIdMin",
                        column: x => x.ScriptIdMin,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConflictPairDismissals_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictPairDismissals_ResolvedByUserId",
                table: "ConflictPairDismissals",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictPairDismissals_ScriptIdMax",
                table: "ConflictPairDismissals",
                column: "ScriptIdMax");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictPairDismissals_ScriptIdMin_ScriptIdMax",
                table: "ConflictPairDismissals",
                columns: new[] { "ScriptIdMin", "ScriptIdMax" });

            migrationBuilder.DropColumn(
                name: "SqlFingerprintMax",
                table: "Conflicts");

            migrationBuilder.DropColumn(
                name: "SqlFingerprintMin",
                table: "Conflicts");
        }
    }
}
