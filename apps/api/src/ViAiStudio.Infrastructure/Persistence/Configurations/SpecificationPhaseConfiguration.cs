using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationPhaseConfiguration : IEntityTypeConfiguration<SpecificationPhase>
{
    public void Configure(EntityTypeBuilder<SpecificationPhase> builder)
    {
        builder.ToTable("SpecificationPhases");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.SpecificationId, p.PhaseIndex }).IsUnique();

        builder.Property(p => p.CheckedItems)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");

        builder.Property(p => p.SelectedKeywords)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");
    }
}
