using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityApi.Application.Clientes;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// Cenários de teste do login: é direto no Keycloak (ROPC via client público
/// `vendas-frontend`) — não passa pelo `identity-api`, que só participa do cadastro. Por
/// isso este teste registra o cliente via `identity-api` mas loga chamando o Keycloak
/// efêmero diretamente, provando o fluxo cadastro→login de ponta a ponta.
/// </summary>
public class LoginTests : IClassFixture<KeycloakContainerFixture>, IAsyncLifetime
{
    private readonly KeycloakContainerFixture _keycloak;
    private IdentityApiFactory _factory = null!;
    private HttpClient _identityClient = null!;
    private HttpClient _keycloakClient = null!;

    public LoginTests(KeycloakContainerFixture keycloak)
    {
        _keycloak = keycloak;
    }

    public Task InitializeAsync()
    {
        _factory = new IdentityApiFactory(_keycloak.BaseUrl);
        _identityClient = _factory.CreateClient();
        _keycloakClient = new HttpClient { BaseAddress = new Uri(_keycloak.BaseUrl) };
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _identityClient.Dispose();
        _keycloakClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_ClienteRecemCadastrado_RetornaTokenComSubIgualAoIdDoCadastro()
    {
        var (email, senha, idCadastrado) = await RegistrarClienteAsync();

        var response = await LoginAsync(email, senha);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var claims = await DecodificarTokenAsync(response);
        Assert.Equal(idCadastrado, claims.Sub);
    }

    [Fact]
    public async Task Login_ClienteRecemCadastrado_TokenContemRoleCliente()
    {
        var (email, senha, _) = await RegistrarClienteAsync();

        var response = await LoginAsync(email, senha);

        var claims = await DecodificarTokenAsync(response);
        Assert.Contains("cliente", claims.Roles);
    }

    [Fact]
    public async Task Login_SenhaErrada_Retorna401()
    {
        var (email, _, _) = await RegistrarClienteAsync();

        var response = await LoginAsync(email, "SenhaErrada999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmailNaoCadastrado_Retorna401()
    {
        var response = await LoginAsync($"naoexiste.{Guid.NewGuid():N}@example.com", "QualquerSenha123");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_RenovarComRefreshToken_RetornaNovoAccessToken()
    {
        var (email, senha, _) = await RegistrarClienteAsync();
        var loginResponse = await LoginAsync(email, senha);
        var token = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

        var refreshResponse = await _keycloakClient.PostAsync(
            "realms/clientes/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = "vendas-frontend",
                ["refresh_token"] = token!.RefreshToken,
            }));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var novoToken = await refreshResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(novoToken!.AccessToken));
    }

    [Fact]
    public async Task Login_UsuarioVendedorSemeado_TokenContemRoleVendedor()
    {
        // Usuário semeado no export do realm (US1.5), não criado via identity-api.
        var response = await LoginAsync("vendedor@revendax.local", "VendedorDev123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var claims = await DecodificarTokenAsync(response);
        Assert.Contains("vendedor", claims.Roles);
    }

    [Fact]
    public async Task Login_ClienteRecemCadastrado_TokenNuncaContemRoleVendedor()
    {
        var (email, senha, _) = await RegistrarClienteAsync();

        var response = await LoginAsync(email, senha);

        var claims = await DecodificarTokenAsync(response);
        Assert.DoesNotContain("vendedor", claims.Roles);
    }

    private async Task<(string Email, string Senha, string Id)> RegistrarClienteAsync()
    {
        var request = TestData.NovoClienteValido();

        var response = await _identityClient.PostAsJsonAsync("/clientes", request);
        response.EnsureSuccessStatusCode();

        var cliente = await response.Content.ReadFromJsonAsync<ClienteResult>();
        return (request.Email, request.Senha, cliente!.Id);
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string senha) =>
        _keycloakClient.PostAsync(
            "realms/clientes/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "vendas-frontend",
                ["username"] = email,
                ["password"] = senha,
            }));

    private static async Task<TokenClaims> DecodificarTokenAsync(HttpResponseMessage response)
    {
        var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        var payloadBase64 = token!.AccessToken.Split('.')[1];
        var padded = payloadBase64.PadRight(payloadBase64.Length + (4 - payloadBase64.Length % 4) % 4, '=');
        var payloadJson = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));

        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        var sub = root.GetProperty("sub").GetString()!;
        var roles = root.GetProperty("realm_access").GetProperty("roles")
            .EnumerateArray().Select(r => r.GetString()!).ToArray();

        return new TokenClaims(sub, roles);
    }

    private record TokenClaims(string Sub, string[] Roles);

    private class TokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; set; }
    }
}
