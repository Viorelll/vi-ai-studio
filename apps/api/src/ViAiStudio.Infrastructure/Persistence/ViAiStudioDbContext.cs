using Microsoft.EntityFrameworkCore;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class ViAiStudioDbContext(DbContextOptions<ViAiStudioDbContext> options) : DbContext(options)
{
    public DbSet<Specification> Specifications => Set<Specification>();
    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<SpecificationIntakeSheet> SpecificationIntakeSheets => Set<SpecificationIntakeSheet>();
    public DbSet<SpecificationInterviewAnswer> SpecificationInterviewAnswers => Set<SpecificationInterviewAnswer>();
    public DbSet<SpecificationDocument> SpecificationDocuments => Set<SpecificationDocument>();
    public DbSet<SpecificationGenerationRun> SpecificationGenerationRuns => Set<SpecificationGenerationRun>();
    public DbSet<SpecificationGenerationBatch> SpecificationGenerationBatches => Set<SpecificationGenerationBatch>();
    public DbSet<SpecificationValidationIssue> SpecificationValidationIssues => Set<SpecificationValidationIssue>();
    public DbSet<SpecificationPromptTemplate> SpecificationPromptTemplates => Set<SpecificationPromptTemplate>();
    public DbSet<AiModelConfig> AiModelConfigs => Set<AiModelConfig>();
    public DbSet<TaskRouting> TaskRoutings => Set<TaskRouting>();
    public DbSet<AiCallLog> AiCallLogs => Set<AiCallLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ViAiStudioDbContext).Assembly);
    }
}
