using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ConflictPairDismissal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Conflicts WHERE ResolvedAt IS NOT NULL;");

            migrationBuilder.Sql(
                """
                UPDATE s
                SET s.Status = COALESCE(s.StatusBeforeConflict, 1),
                    s.StatusBeforeConflict = NULL
                FROM Scripts s
                WHERE s.IsDeleted = 0 AND s.Status = 4
                AND NOT EXISTS (
                    SELECT 1 FROM Conflicts c
                    WHERE c.IsDeleted = 0 AND (c.ScriptId = s.Id OR c.ConflictingScriptId = s.Id)
                );
                """);

            migrationBuilder.CreateTable(
                name: "ConflictPairDismissals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScriptIdMin = table.Column<long>(type: "bigint", nullable: false),
                    ScriptIdMax = table.Column<long>(type: "bigint", nullable: false),
                    SqlFingerprintMin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SqlFingerprintMax = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResolvedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ResolutionKind = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ConflictPairDismissals");
        }
    }
}
