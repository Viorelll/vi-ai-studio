using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user, IReadOnlyList<string> roles);
}