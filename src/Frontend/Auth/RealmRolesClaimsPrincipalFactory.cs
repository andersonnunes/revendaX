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
///
/// Bug real encontrado na US4.4 (só apareceu quando uma checagem de role passou a existir de
/// verdade: <c>&lt;AuthorizeView Roles="cliente"&gt;</c> na vitrine): <c>realm_access</c> só
/// vem no **access token**, não no **id token** — e é o id token que popula
/// <c>RemoteUserAccount.AdditionalProperties</c>, de onde a classe base
/// (<c>AccountClaimsPrincipalFactory&lt;T&gt;.CreateUserAsync</c>) monta os claims do
/// principal. `user.FindFirst("realm_access")` sempre voltava nulo — mostrando "autenticado
/// sem role" pra todo mundo, silenciosamente, exatamente o cenário que o comentário original
/// deste arquivo já avisava ser o risco. Corrigido lendo o access token
/// (<c>TokenProvider.RequestAccessToken()</c>, exposto pela própria classe base) e decodificando
/// o payload JWT manualmente — não há um `FindFirst` que alcance essa claim por outro caminho.
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

        var tokenResult = await TokenProvider.RequestAccessToken();
        if (!tokenResult.TryGetToken(out var token))
        {
            return user;
        }

        using var payload = ParseJwtPayload(token.Value);
        if (payload is null
            || !payload.RootElement.TryGetProperty("realm_access", out var realmAccess)
            || !realmAccess.TryGetProperty("roles", out var roles))
        {
            return user;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var nome = role.GetString();
            if (!string.IsNullOrEmpty(nome))
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, nome));
            }
        }

        return user;
    }

    /// <summary>Decodifica só o payload (segunda parte) de um JWT — sem validar assinatura:
    /// o token já veio de uma resposta HTTPS confiável do próprio Keycloak
    /// (<see cref="IAccessTokenProvider"/>), a mesma garantia que qualquer outro uso do
    /// access token no Blazor já assume.</summary>
    private static JsonDocument? ParseJwtPayload(string jwt)
    {
        var partes = jwt.Split('.');
        if (partes.Length != 3)
        {
            return null;
        }

        var payloadBase64 = partes[1].Replace('-', '+').Replace('_', '/');
        payloadBase64 = payloadBase64.PadRight(payloadBase64.Length + ((4 - (payloadBase64.Length % 4)) % 4), '=');

        return JsonDocument.Parse(Convert.FromBase64String(payloadBase64));
    }
}
