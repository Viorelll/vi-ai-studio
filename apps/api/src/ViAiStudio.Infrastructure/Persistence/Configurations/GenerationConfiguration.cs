using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class GenerationConfiguration : IEntityTypeConfiguration<Generation>
{
    public void Configure(EntityTypeBuilder<Generation> builder)
    {
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => new { g.SpecificationId, g.Version }).IsUnique();
        builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(g => g.Stack, stack =>
        {
            stack.Property(t => t.Backend).HasColumnName("stack_backend").HasMaxLength(50);
            stack.Property(t => t.Ui).HasColumnName("stack_ui").HasMaxLength(50);
            stack.Property(t => t.Database).HasColumnName("stack_database").HasMaxLength(50);
            stack.Property(t => t.Infra).HasColumnName("stack_infra").HasMaxLength(50);
            stack.Property(t => t.UiStyle).HasColumnName("stack_ui_style").HasMaxLength(50);
        });

        builder.Property(g => g.FileTree)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");
    }
}
