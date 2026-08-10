using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Auth;
using ViAiStudio.Infrastructure.Auth;

namespace ViAiStudio.Api.Endpoints;

public sealed record GoogleSignInRequest(string IdToken);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/google", async (GoogleSignInRequest request, GoogleSignInHandler handler, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(new GoogleSignInCommand(request.IdToken), cancellationToken);
                return Results.Ok(AuthResponse.FromResult(result));
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        });

        group.MapGet("/whoami", (ClaimsPrincipal user) =>
        {
            var email = user.FindFirstValue(JwtRegisteredClaimNames.Email);
            var roles = user.FindAll(JwtClaimTypes.Role).Select(c => c.Value).ToArray();
            return Results.Ok(new { authenticated = true, email, roles });
        }).RequireAuthorization();
    }
}