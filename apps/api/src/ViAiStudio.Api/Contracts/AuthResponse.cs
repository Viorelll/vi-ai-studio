using ViAiStudio.Application.Auth;

namespace ViAiStudio.Api.Contracts;

public sealed record AuthenticatedUserResponse(int Id, string Email, string? FullName, string? AvatarUrl, IReadOnlyList<string> Roles)
{
    public static AuthenticatedUserResponse FromEntity(AuthenticatedUser user) =>
        new(user.Id, user.Email, user.FullName, user.AvatarUrl, user.Roles);
}

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, AuthenticatedUserResponse User)
{
    public static AuthResponse FromResult(AuthResult result) =>
        new(result.Token, result.ExpiresAt, AuthenticatedUserResponse.FromEntity(result.User));
}