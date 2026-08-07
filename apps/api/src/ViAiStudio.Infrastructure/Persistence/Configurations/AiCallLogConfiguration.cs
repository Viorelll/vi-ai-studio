using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class AiCallLogConfiguration : IEntityTypeConfiguration<AiCallLog>
{
    public void Configure(EntityTypeBuilder<AiCallLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.SpecificationId);
        builder.Property(l => l.Task).HasConversion<string>().HasMaxLength(30);
        builder.Property(l => l.Model).HasMaxLength(100);

        builder.HasOne<Specification>()
            .WithMany()
            .HasForeignKey(l => l.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
