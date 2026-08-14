using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViAiStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldPhaseWizardModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecificationPhases");

            migrationBuilder.DropColumn(
                name: "SpecMarkdown",
                table: "Specifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecMarkdown",
                table: "Specifications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SpecificationPhases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedItems = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedText = table.Column<string>(type: "text", nullable: true),
                    PhaseIndex = table.Column<int>(type: "integer", nullable: false),
                    SelectedKeywords = table.Column<string>(type: "jsonb", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationPhases_Specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "Specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationPhases_SpecificationId_PhaseIndex",
                table: "SpecificationPhases",
                columns: new[] { "SpecificationId", "PhaseIndex" },
                unique: true);
        }
    }
}
