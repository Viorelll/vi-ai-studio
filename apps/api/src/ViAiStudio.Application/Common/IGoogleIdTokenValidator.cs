namespace ViAiStudio.Application.Common;

public sealed record GoogleUserInfo(string Email, string? FullName, string? AvatarUrl);

public interface IGoogleIdTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}