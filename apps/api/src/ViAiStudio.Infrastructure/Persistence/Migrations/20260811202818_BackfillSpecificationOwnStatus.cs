using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViAiStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillSpecificationOwnStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Specification.Status used to mirror its latest Generation's outcome
            // (Building/Ready/Failed). It's now the specification's own state,
            // set only by FinalizeSpecificationHandler, so recompute existing
            // rows the same way: finalized (SpecMarkdown present) -> Ready,
            // otherwise Draft. Per-build outcomes remain on Generations.Status,
            // untouched by this migration.
            migrationBuilder.Sql(
                """
                UPDATE "Specifications"
                SET "Status" = CASE WHEN "SpecMarkdown" IS NOT NULL THEN 'Ready' ELSE 'Draft' END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: the original per-build Building/Failed values this
            // overwrote are gone.
        }
    }
}
