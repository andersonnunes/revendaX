using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Fala direto com o Keycloak (Admin API + endpoints OIDC) para preparar a massa de teste —
/// não passa pelo `identity-api`, porque o que a US1.3 valida é "um token real emitido pelo
/// Keycloak autoriza corretamente", não o fluxo de cadastro em si (já coberto em
/// `tests/IdentityApi.Tests`).
/// </summary>
public class KeycloakTestHelper(HttpClient keycloakClient)
{
    public async Task<string> ObterTokenAdminAsync()
    {
        var response = await keycloakClient.PostAsync(
            "realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = "admin",
                ["password"] = "admin",
            }));
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        return token!.AccessToken;
    }

    /// <summary>Cria um usuário no realm `clientes` — recebe a role `cliente` automaticamente (default role). Retorna o id.</summary>
    public async Task<string> CriarUsuarioAsync(string adminToken, string email, string senha)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "admin/realms/clientes/users");
        request.Headers.Authorization = new("Bearer", adminToken);
        request.Content = JsonContent.Create(new
        {
            username = email,
            email,
            firstName = "Cliente de Teste",
            enabled = true,
            emailVerified = false,
            attributes = new Dictionary<string, string[]> { ["cpf"] = [GerarCpfValido()] },
            credentials = new[] { new { type = "password", value = senha, temporary = false } },
        });

        var response = await keycloakClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response.Headers.Location!.Segments[^1];
    }

    /// <summary>
    /// Remove `cliente` do composite `default-roles-clientes` (realm inteiro, temporário —
    /// ver <see cref="RestaurarClienteNoDefaultRoleAsync"/>). Usada para criar um usuário sem
    /// a role `cliente`, pra testar autorização (403), sem depender do usuário `vendedor` da
    /// US1.5.
    ///
    /// Duas tentativas mais óbvias que **não** funcionam, documentadas aqui pra não repetir o
    /// mesmo caminho errado depois:
    /// 1. Remover "cliente" do mapeamento direto do usuário (`DELETE
    ///    /users/{id}/role-mappings/realm`) — não tem efeito, porque "cliente" nunca é um
    ///    mapeamento direto do usuário, só chega via composite.
    /// 2. Remover o composite inteiro `default-roles-clientes` do usuário — tira "cliente" de
    ///    fato, mas também tira os roles do client `account` (`view-profile` etc.), e isso faz
    ///    o Keycloak parar de incluir `account` no `aud` do token — o teste vira "token sem
    ///    audience válida" (401), não "token válido sem a role" (403), invalidando o cenário.
    ///
    /// A única forma que preserva o resto do token (audience, `offline_access` etc.) e tira
    /// só `cliente` é editar o composite em si.
    /// </summary>
    public async Task RemoverClienteDoDefaultRoleAsync(string adminToken)
    {
        await AlterarComposicaoDefaultRoleAsync(adminToken, HttpMethod.Delete);
    }

    /// <summary>Desfaz <see cref="RemoverClienteDoDefaultRoleAsync"/> — sempre chamar em `finally`.</summary>
    public async Task RestaurarClienteNoDefaultRoleAsync(string adminToken)
    {
        await AlterarComposicaoDefaultRoleAsync(adminToken, HttpMethod.Post);
    }

    private async Task AlterarComposicaoDefaultRoleAsync(string adminToken, HttpMethod method)
    {
        using var getRole = new HttpRequestMessage(HttpMethod.Get, "admin/realms/clientes/roles/cliente");
        getRole.Headers.Authorization = new("Bearer", adminToken);
        var roleResponse = await keycloakClient.SendAsync(getRole);
        roleResponse.EnsureSuccessStatusCode();
        var roleJson = await roleResponse.Content.ReadAsStringAsync();

        using var request = new HttpRequestMessage(method, "admin/realms/clientes/roles/default-roles-clientes/composites")
        {
            Content = new StringContent($"[{roleJson}]", System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new("Bearer", adminToken);

        var response = await keycloakClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> LoginAsync(string email, string senha)
    {
        var response = await keycloakClient.PostAsync(
            "realms/clientes/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "vendas-frontend",
                ["username"] = email,
                ["password"] = senha,
            }));
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        return token!.AccessToken;
    }

    /// <summary>Corrompe a assinatura de um JWT — mesmo header/payload, assinatura inválida.</summary>
    public static string AdulterarAssinatura(string token)
    {
        var partes = token.Split('.');
        var assinaturaInvertida = new string(partes[2].Reverse().ToArray());
        return $"{partes[0]}.{partes[1]}.{assinaturaInvertida}";
    }

    private static string GerarCpfValido()
    {
        var random = Random.Shared;
        int[] digits;
        do
        {
            digits = Enumerable.Range(0, 9).Select(_ => random.Next(0, 10)).ToArray();
        } while (digits.Distinct().Count() == 1);

        var d1 = CalcularDigito(digits, 9);
        var d2 = CalcularDigito([.. digits, d1], 10);
        return string.Concat(digits) + d1 + d2;
    }

    private static int CalcularDigito(int[] numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * (length + 1 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private class TokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }
    }
}
