using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseTree_NoReleaseScript_TransferTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseScripts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Conflicts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Conflicts");

            migrationBuilder.AddColumn<int>(
                name: "TransferTarget",
                table: "Scripts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ReleaseId",
                table: "Batches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_ReleaseId_Name",
                table: "Batches",
                columns: new[] { "ReleaseId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Releases_ReleaseId",
                table: "Batches",
                column: "ReleaseId",
                principalTable: "Releases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Releases_ReleaseId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_ReleaseId_Name",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "TransferTarget",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "ReleaseId",
                table: "Batches");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Releases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "Conflicts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Conflicts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReleaseScripts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReleaseId = table.Column<long>(type: "bigint", nullable: false),
                    ScriptId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseScripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseScripts_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReleaseScripts_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseScripts_ReleaseId",
                table: "ReleaseScripts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseScripts_ScriptId",
                table: "ReleaseScripts",
                column: "ScriptId");
        }
    }
}
