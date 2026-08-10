namespace ViAiStudio.Domain.Entities;

public sealed class User
{
    public int Id { get; init; }
    public required string Email { get; set; }
    public DateTimeOffset CreatedAt { get; init; }

    public UserProfile? Profile { get; init; }
    public List<UserRole> UserRoles { get; init; } = [];
}