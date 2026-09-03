using System.Net;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da validação de token (JWT Bearer + JWKS) contra um Keycloak real
/// efêmero — não mockado.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class WhoAmITests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public WhoAmITests(VendasApiTestEnvironment env)
    {
        _env = env;
    }

    public Task InitializeAsync()
    {
        _factory = new VendasApiFactory(_env.KeycloakBaseUrl, _env.PostgresConnectionString);
        _client = _factory.CreateClient();
        _keycloakHelper = new KeycloakTestHelper(new HttpClient { BaseAddress = new Uri(_env.KeycloakBaseUrl) });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task WhoAmI_TokenValido_Retorna200()
    {
        var token = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WhoAmI_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WhoAmI_TokenComAssinaturaInvalida_Retorna401()
    {
        var token = await CriarClienteEFazerLoginAsync();
        var tokenAdulterado = KeycloakTestHelper.AdulterarAssinatura(token);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenAdulterado);

        var response = await _client.GetAsync("/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WhoAmICliente_TokenComRoleCliente_Retorna200()
    {
        var token = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync("/whoami/cliente");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WhoAmICliente_TokenValidoSemRoleCliente_Retorna403()
    {
        // Token real, assinado pelo mesmo realm, de um usuário normal — só sem a role
        // `cliente` (removida do default role temporariamente, ver
        // KeycloakTestHelper.RemoverClienteDoDefaultRoleAsync; é o único jeito de tirar
        // "cliente" sem também derrubar a audience `account` do token). Prova que 403 é por
        // role, não por assinatura/emissor/audience inválidos. Sempre restaura no `finally`
        // — é uma mudança de realm inteiro, não só deste usuário.
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.RemoverClienteDoDefaultRoleAsync(adminToken);
        try
        {
            var token = await CriarClienteEFazerLoginAsync();
            _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var response = await _client.GetAsync("/whoami/cliente");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await _keycloakHelper.RestaurarClienteNoDefaultRoleAsync(adminToken);
        }
    }

    [Fact]
    public async Task WhoAmIVendedor_UsuarioVendedorSemeado_Retorna200()
    {
        // Usuário semeado no export do realm (US1.5), não criado por este teste.
        var token = await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync("/whoami/vendedor");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WhoAmIVendedor_TokenDeComprador_Retorna403()
    {
        var token = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync("/whoami/vendedor");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> CriarClienteEFazerLoginAsync()
    {
        var email = $"vendas-teste.{Guid.NewGuid():N}@example.com";
        const string senha = "SenhaForte123";

        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
        return await _keycloakHelper.LoginAsync(email, senha);
    }
}
