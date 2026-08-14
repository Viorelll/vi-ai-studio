using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationIntakeSheetConfiguration : IEntityTypeConfiguration<SpecificationIntakeSheet>
{
    public void Configure(EntityTypeBuilder<SpecificationIntakeSheet> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.SpecificationId).IsUnique();

        foreach (var property in new[]
        {
            nameof(SpecificationIntakeSheet.Deployables),
            nameof(SpecificationIntakeSheet.IdentityFeatures),
            nameof(SpecificationIntakeSheet.SupportingInfrastructure),
            nameof(SpecificationIntakeSheet.FrontendRequirements),
            nameof(SpecificationIntakeSheet.FunctionalAreas),
            nameof(SpecificationIntakeSheet.Compliance),
            nameof(SpecificationIntakeSheet.Environments),
            nameof(SpecificationIntakeSheet.ImpliedDecisions),
            nameof(SpecificationIntakeSheet.ConflictsResolved),
            nameof(SpecificationIntakeSheet.StillUnknown),
        })
        {
            builder.Property<List<string>>(property)
                .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
                .HasColumnType("jsonb");
        }
    }
}
