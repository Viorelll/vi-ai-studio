using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationValidationIssueConfiguration : IEntityTypeConfiguration<SpecificationValidationIssue>
{
    public void Configure(EntityTypeBuilder<SpecificationValidationIssue> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.SpecificationId);
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(10);
        builder.Property(i => i.Code).HasMaxLength(40);
    }
}
