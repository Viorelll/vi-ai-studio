using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence.Configurations;

public sealed class SpecificationInterviewAnswerConfiguration : IEntityTypeConfiguration<SpecificationInterviewAnswer>
{
    public void Configure(EntityTypeBuilder<SpecificationInterviewAnswer> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.SpecificationId, a.RoundIndex, a.QuestionIndex }).IsUnique();
    }
}
