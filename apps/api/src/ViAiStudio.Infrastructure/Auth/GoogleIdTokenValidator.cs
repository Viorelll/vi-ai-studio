using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Infrastructure.Auth;

public sealed class GoogleIdTokenValidator(IOptions<GoogleOptions> options) : IGoogleIdTokenValidator
{
    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [options.Value.ClientId],
            });

            return new GoogleUserInfo(payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}