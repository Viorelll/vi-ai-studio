namespace ViAiStudio.Domain.Entities;

public sealed class UserProfile
{
    public int Id { get; init; }
    public required int UserId { get; init; }
    public User? User { get; init; }

    public required string FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
}