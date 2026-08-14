using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationGenerationBatchConfiguration : IEntityTypeConfiguration<SpecificationGenerationBatch>
{
    public void Configure(EntityTypeBuilder<SpecificationGenerationBatch> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.RunId, b.BatchIndex }).IsUnique();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(b => b.AllocatedIds)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");
    }
}
