using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationConfiguration : IEntityTypeConfiguration<Specification>
{
    public void Configure(EntityTypeBuilder<Specification> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(s => s.Stack, stack =>
        {
            stack.Property(t => t.Backend).HasColumnName("stack_backend").HasMaxLength(50);
            stack.Property(t => t.Ui).HasColumnName("stack_ui").HasMaxLength(50);
            stack.Property(t => t.Database).HasColumnName("stack_database").HasMaxLength(50);
            stack.Property(t => t.Infra).HasColumnName("stack_infra").HasMaxLength(50);
            stack.Property(t => t.UiStyle).HasColumnName("stack_ui_style").HasMaxLength(50);
        });

        builder.Property(s => s.DocumentPaths)
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer)
            .HasColumnType("jsonb");

        builder.HasMany(s => s.Generations)
            .WithOne()
            .HasForeignKey(g => g.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Intake)
            .WithOne()
            .HasForeignKey<SpecificationIntakeSheet>(i => i.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.InterviewAnswers)
            .WithOne()
            .HasForeignKey(a => a.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Documents)
            .WithOne()
            .HasForeignKey(d => d.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.GenerationRuns)
            .WithOne()
            .HasForeignKey(r => r.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
