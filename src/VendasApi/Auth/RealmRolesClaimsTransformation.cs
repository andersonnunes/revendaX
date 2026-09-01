using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace VendasApi.Auth;

/// <summary>
/// O Keycloak coloca as roles do usuário em `realm_access.roles` (claim aninhada) — não no
/// formato que `[Authorize(Roles = "...")]` entende de fábrica. Sem essa transformação,
/// autorização por role nunca funciona, mesmo com um token válido.
/// </summary>
public class RealmRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        // Evita duplicar as roles se essa transformação rodar mais de uma vez por requisição.
        if (identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.FromResult(principal);
        }

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccess))
        {
            return Task.FromResult(principal);
        }

        using var doc = JsonDocument.Parse(realmAccess);
        if (doc.RootElement.TryGetProperty("roles", out var roles))
        {
            foreach (var role in roles.EnumerateArray())
            {
                var nome = role.GetString();
                if (!string.IsNullOrEmpty(nome))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, nome));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
