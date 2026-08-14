using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationDocumentConfiguration : IEntityTypeConfiguration<SpecificationDocument>
{
    public void Configure(EntityTypeBuilder<SpecificationDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.SpecificationId, d.Path }).IsUnique();
        builder.Property(d => d.SpecId).HasMaxLength(20);
        builder.Property(d => d.Status).HasMaxLength(20);
        builder.Property(d => d.Version).HasMaxLength(20);

        builder.Property(d => d.DependsOn)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");
        builder.Property(d => d.Provides)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");
        builder.Property(d => d.Generates)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");
    }
}
