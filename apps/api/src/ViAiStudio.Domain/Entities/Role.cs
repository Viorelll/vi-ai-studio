namespace ViAiStudio.Domain.Entities;

public sealed class Role
{
    public int Id { get; init; }
    public required string Name { get; set; }

    public List<UserRole> UserRoles { get; init; } = [];
}