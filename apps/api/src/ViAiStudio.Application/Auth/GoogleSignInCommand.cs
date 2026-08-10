using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Auth;

public sealed record GoogleSignInCommand(string IdToken);

public sealed record AuthenticatedUser(int Id, string Email, string? FullName, string? AvatarUrl, IReadOnlyList<string> Roles);

public sealed record AuthResult(string Token, DateTimeOffset ExpiresAt, AuthenticatedUser User);

public sealed class GoogleSignInHandler(
    IGoogleIdTokenValidator googleIdTokenValidator,
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService)
{
    private const string DefaultRoleName = "User";

    public async Task<AuthResult> HandleAsync(GoogleSignInCommand command, CancellationToken cancellationToken)
    {
        var googleUser = await googleIdTokenValidator.ValidateAsync(command.IdToken, cancellationToken)
            ?? throw new InvalidOperationException("Invalid Google ID token.");

        var user = await userRepository.GetByEmailAsync(googleUser.Email, cancellationToken);
        if (user is null)
        {
            user = new User { Email = googleUser.Email, CreatedAt = DateTimeOffset.UtcNow };
            await userRepository.AddAsync(user, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);

            var profile = new UserProfile
            {
                UserId = user.Id,
                FullName = googleUser.FullName ?? googleUser.Email,
                AvatarUrl = googleUser.AvatarUrl,
            };
            await userRepository.AddProfileAsync(profile, cancellationToken);

            var defaultRole = await userRepository.GetOrCreateRoleAsync(DefaultRoleName, cancellationToken);
            await userRepository.AddUserRoleAsync(user.Id, defaultRole.Id, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);
        }

        var roles = await userRepository.GetRoleNamesAsync(user.Id, cancellationToken);
        var (token, expiresAt) = jwtTokenService.GenerateToken(user, roles);

        return new AuthResult(token, expiresAt, new AuthenticatedUser(user.Id, user.Email, googleUser.FullName, googleUser.AvatarUrl, roles));
    }
}