using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViAiStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationAuthoringPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpecificationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: false),
                    SpecId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Component = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DependsOn = table.Column<string>(type: "jsonb", nullable: false),
                    Provides = table.Column<string>(type: "jsonb", nullable: false),
                    Generates = table.Column<string>(type: "jsonb", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationDocuments_Specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "Specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationGenerationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationGenerationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationGenerationRuns_Specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "Specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationIntakeSheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductShape = table.Column<string>(type: "text", nullable: false),
                    TenantIsolation = table.Column<string>(type: "text", nullable: false),
                    IdentityModel = table.Column<string>(type: "text", nullable: false),
                    PrimaryDatabase = table.Column<string>(type: "text", nullable: false),
                    Frontend = table.Column<string>(type: "text", nullable: false),
                    Rigour = table.Column<string>(type: "text", nullable: false),
                    SpecScope = table.Column<string>(type: "text", nullable: false),
                    Team = table.Column<string>(type: "text", nullable: false),
                    Deployables = table.Column<string>(type: "jsonb", nullable: false),
                    IdentityFeatures = table.Column<string>(type: "jsonb", nullable: false),
                    SupportingInfrastructure = table.Column<string>(type: "jsonb", nullable: false),
                    FrontendRequirements = table.Column<string>(type: "jsonb", nullable: false),
                    FunctionalAreas = table.Column<string>(type: "jsonb", nullable: false),
                    Compliance = table.Column<string>(type: "jsonb", nullable: false),
                    Environments = table.Column<string>(type: "jsonb", nullable: false),
                    ImpliedDecisions = table.Column<string>(type: "jsonb", nullable: false),
                    ConflictsResolved = table.Column<string>(type: "jsonb", nullable: false),
                    StillUnknown = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationIntakeSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationIntakeSheets_Specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "Specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationInterviewAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundIndex = table.Column<int>(type: "integer", nullable: false),
                    QuestionIndex = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    DefaultHint = table.Column<string>(type: "text", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    UsedDefault = table.Column<bool>(type: "boolean", nullable: false),
                    AiExpandedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationInterviewAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationInterviewAnswers_Specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "Specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationPromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationPromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationValidationIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    DocumentPath = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationValidationIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationGenerationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchIndex = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FilesWritten = table.Column<int>(type: "integer", nullable: false),
                    AllocatedIds = table.Column<string>(type: "jsonb", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationGenerationBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationGenerationBatches_SpecificationGenerationRuns_~",
                        column: x => x.RunId,
                        principalTable: "SpecificationGenerationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationDocuments_SpecificationId_Path",
                table: "SpecificationDocuments",
                columns: new[] { "SpecificationId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationGenerationBatches_RunId_BatchIndex",
                table: "SpecificationGenerationBatches",
                columns: new[] { "RunId", "BatchIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationGenerationRuns_SpecificationId",
                table: "SpecificationGenerationRuns",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationIntakeSheets_SpecificationId",
                table: "SpecificationIntakeSheets",
                column: "SpecificationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationInterviewAnswers_SpecificationId_RoundIndex_Qu~",
                table: "SpecificationInterviewAnswers",
                columns: new[] { "SpecificationId", "RoundIndex", "QuestionIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationPromptTemplates_Key",
                table: "SpecificationPromptTemplates",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationValidationIssues_SpecificationId",
                table: "SpecificationValidationIssues",
                column: "SpecificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecificationDocuments");

            migrationBuilder.DropTable(
                name: "SpecificationGenerationBatches");

            migrationBuilder.DropTable(
                name: "SpecificationIntakeSheets");

            migrationBuilder.DropTable(
                name: "SpecificationInterviewAnswers");

            migrationBuilder.DropTable(
                name: "SpecificationPromptTemplates");

            migrationBuilder.DropTable(
                name: "SpecificationValidationIssues");

            migrationBuilder.DropTable(
                name: "SpecificationGenerationRuns");
        }
    }
}
