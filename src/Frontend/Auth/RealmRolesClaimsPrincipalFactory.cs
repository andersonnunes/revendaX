using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace Frontend.Auth;

/// <summary>
/// Mesmo problema já resolvido no backend (<c>RealmRolesClaimsTransformation</c>, em
/// `vendas-api`): o Keycloak coloca as roles do usuário em `realm_access.roles` (claim
/// aninhada), não no formato que `ClaimTypes.Role`/`&lt;AuthorizeView Roles="..."&gt;`
/// entendem de fábrica. Sem isso, toda checagem de role no Blazor falharia silenciosamente
/// (usuário autenticado, mas nenhuma role reconhecida) — este factory faz o mesmo
/// achatamento, do lado do cliente.
/// </summary>
public class RealmRolesClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor)
    : AccountClaimsPrincipalFactory<RemoteUserAccount>(accessor)
{
    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        if (user.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return user;
        }

        var realmAccessJson = user.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccessJson))
        {
            return user;
        }

        using var doc = JsonDocument.Parse(realmAccessJson);
        if (doc.RootElement.TryGetProperty("roles", out var roles))
        {
            foreach (var role in roles.EnumerateArray())
            {
                var nome = role.GetString();
                if (!string.IsNullOrEmpty(nome))
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, nome));
                }
            }
        }

        return user;
    }
}
