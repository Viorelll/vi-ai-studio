namespace ViAiStudio.Domain.Entities;

public sealed class UserRole
{
    public int UserId { get; init; }
    public User? User { get; init; }

    public int RoleId { get; init; }
    public Role? Role { get; init; }
}