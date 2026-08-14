using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViAiStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationDocumentsArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentPaths",
                table: "Specifications",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "DocumentsArchiveStorageKey",
                table: "Specifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentPaths",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "DocumentsArchiveStorageKey",
                table: "Specifications");
        }
    }
}
