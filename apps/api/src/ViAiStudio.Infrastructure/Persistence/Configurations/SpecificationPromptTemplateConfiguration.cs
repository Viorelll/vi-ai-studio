using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationPromptTemplateConfiguration : IEntityTypeConfiguration<SpecificationPromptTemplate>
{
    public void Configure(EntityTypeBuilder<SpecificationPromptTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.Key).IsUnique();
        builder.Property(t => t.Key).HasMaxLength(80);
        builder.Property(t => t.Stage).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Category).HasMaxLength(40);
        builder.Property(t => t.Title).HasMaxLength(200);
    }
}
