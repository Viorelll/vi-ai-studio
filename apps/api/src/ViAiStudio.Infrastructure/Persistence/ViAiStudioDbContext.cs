using Microsoft.EntityFrameworkCore;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class ViAiStudioDbContext(DbContextOptions<ViAiStudioDbContext> options) : DbContext(options)
{
    public DbSet<Specification> Specifications => Set<Specification>();
    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<AiModelConfig> AiModelConfigs => Set<AiModelConfig>();
    public DbSet<TaskRouting> TaskRoutings => Set<TaskRouting>();
    public DbSet<AiCallLog> AiCallLogs => Set<AiCallLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ViAiStudioDbContext).Assembly);
    }
}
