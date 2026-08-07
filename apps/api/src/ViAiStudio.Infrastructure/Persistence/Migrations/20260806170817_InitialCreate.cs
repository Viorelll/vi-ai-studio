using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViAiStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiModelConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModelConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Features = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SpecMarkdown = table.Column<string>(type: "text", nullable: true),
                    stack_backend = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_database = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_infra = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_ui = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_ui_style = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskRoutings",
                columns: table => new
                {
                    Task = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AiModelConfigId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskRoutings", x => x.Task);
                    table.ForeignKey(
                        name: "FK_TaskRoutings_AiModelConfigs_AiModelConfigId",
                        column: x => x.AiModelConfigId,
                        principalTable: "AiModelConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AiCallLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenerationVersion = table.Column<int>(type: "integer", nullable: true),
                    Task = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Requests = table.Column<int>(type: "integer", nullable: false),
                    TokensIn = table.Column<int>(type: "integer", nullable: false),
                    TokensOut = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiCallLogs_Specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "Specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Generations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    FileTree = table.Column<string>(type: "jsonb", nullable: false),
                    ArchiveStorageKey = table.Column<string>(type: "text", nullable: true),
                    stack_backend = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_database = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_infra = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_ui = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stack_ui_style = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Generations_Specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "Specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationPhases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhaseIndex = table.Column<int>(type: "integer", nullable: false),
                    CheckedItems = table.Column<string>(type: "jsonb", nullable: false),
                    SelectedKeywords = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedText = table.Column<string>(type: "text", nullable: true)
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
                name: "IX_AiCallLogs_SpecificationId",
                table: "AiCallLogs",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Generations_SpecificationId_Version",
                table: "Generations",
                columns: new[] { "SpecificationId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationPhases_SpecificationId_PhaseIndex",
                table: "SpecificationPhases",
                columns: new[] { "SpecificationId", "PhaseIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskRoutings_AiModelConfigId",
                table: "TaskRoutings",
                column: "AiModelConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCallLogs");

            migrationBuilder.DropTable(
                name: "Generations");

            migrationBuilder.DropTable(
                name: "SpecificationPhases");

            migrationBuilder.DropTable(
                name: "TaskRoutings");

            migrationBuilder.DropTable(
                name: "Specifications");

            migrationBuilder.DropTable(
                name: "AiModelConfigs");
        }
    }
}
