using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationGenerationRunConfiguration : IEntityTypeConfiguration<SpecificationGenerationRun>
{
    public void Configure(EntityTypeBuilder<SpecificationGenerationRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(r => r.Batches)
            .WithOne()
            .HasForeignKey(b => b.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
