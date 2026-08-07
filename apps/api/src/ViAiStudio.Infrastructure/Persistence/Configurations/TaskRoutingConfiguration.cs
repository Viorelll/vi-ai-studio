using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class TaskRoutingConfiguration : IEntityTypeConfiguration<TaskRouting>
{
    public void Configure(EntityTypeBuilder<TaskRouting> builder)
    {
        builder.HasKey(r => r.Task);
        builder.Property(r => r.Task).HasConversion<string>().HasMaxLength(30);

        builder.HasOne<AiModelConfig>()
            .WithMany()
            .HasForeignKey(r => r.AiModelConfigId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
