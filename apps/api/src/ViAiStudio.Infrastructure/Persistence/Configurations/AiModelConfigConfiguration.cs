using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class AiModelConfigConfiguration : IEntityTypeConfiguration<AiModelConfig>
{
    public void Configure(EntityTypeBuilder<AiModelConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Provider).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Label).HasMaxLength(100);
        builder.Property(c => c.Model).HasMaxLength(100);
        builder.Property(c => c.BaseUrl).HasMaxLength(300);
    }
}
