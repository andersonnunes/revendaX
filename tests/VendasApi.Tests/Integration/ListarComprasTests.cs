using System.Net;
using System.Net.Http.Json;
using VendasApi.Application.Compras;
using VendasApi.Application.Veiculos;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da listagem de compras do cliente autenticado (`GET /compras`, extensão
/// da US3.4) contra Keycloak e Postgres reais — não mockados.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ListarComprasTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ListarComprasTests(VendasApiTestEnvironment env)
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
    public async Task Listar_ClienteComDuasCompras_RetornaAsDuasMaisRecentePrimeiro()
    {
        var (email, senha) = await CriarClienteAsync();
        var token = await _keycloakHelper.LoginAsync(email, senha);

        var compra1 = await ComprarVeiculoNovoAsync(token);
        var compra2 = await ComprarVeiculoNovoAsync(token);

        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await _client.GetAsync("/compras");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var compras = await response.Content.ReadFromJsonAsync<List<CompraResult>>();
        var ids = compras!.Select(c => c.Id).ToList();

        // Não assume que a lista tem exatamente 2 itens (a suíte compartilha um único Postgres
        // entre classes de teste, mesma ressalva já registrada na US2.3/US2.4) — só que as duas
        // compras deste cliente aparecem, e na ordem certa entre si (mais recente primeiro).
        Assert.Contains(compra1, ids);
        Assert.Contains(compra2, ids);
        Assert.True(ids.IndexOf(compra2) < ids.IndexOf(compra1));
    }

    [Fact]
    public async Task Listar_ClienteSemNenhumaCompra_Retorna200ComListaVazia()
    {
        var (email, senha) = await CriarClienteAsync();
        var token = await _keycloakHelper.LoginAsync(email, senha);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync("/compras");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var compras = await response.Content.ReadFromJsonAsync<List<CompraResult>>();
        Assert.Empty(compras!);
    }

    [Fact]
    public async Task Listar_NaoInclueCompraDeOutroCliente()
    {
        var (emailA, senhaA) = await CriarClienteAsync();
        var tokenA = await _keycloakHelper.LoginAsync(emailA, senhaA);
        var compraDeA = await ComprarVeiculoNovoAsync(tokenA);

        var (emailB, senhaB) = await CriarClienteAsync();
        var tokenB = await _keycloakHelper.LoginAsync(emailB, senhaB);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenB);

        var response = await _client.GetAsync("/compras");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var compras = await response.Content.ReadFromJsonAsync<List<CompraResult>>();
        Assert.DoesNotContain(compras!, c => c.Id == compraDeA);
    }

    [Fact]
    public async Task Listar_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/compras");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Mesma ressalva já registrada em <see cref="ConsultarCompraTests"/>: o usuário `vendedor`
    /// semeado também carrega a role `cliente` (default role de todo usuário do realm), então
    /// este cenário remove `cliente` do composite temporariamente em vez de usar o token de
    /// vendedor sozinho.
    /// </summary>
    [Fact]
    public async Task Listar_TokenSemRoleCliente_Retorna403()
    {
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.RemoverClienteDoDefaultRoleAsync(adminToken);
        try
        {
            var email = $"listar-sem-role.{Guid.NewGuid():N}@example.com";
            const string senha = "SenhaForte123";
            await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
            var token = await _keycloakHelper.LoginAsync(email, senha);
            _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var response = await _client.GetAsync("/compras");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await _keycloakHelper.RestaurarClienteNoDefaultRoleAsync(adminToken);
        }
    }

    private async Task<(string Email, string Senha)> CriarClienteAsync()
    {
        var email = $"listar-compras.{Guid.NewGuid():N}@example.com";
        const string senha = "SenhaForte123";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
        return (email, senha);
    }

    private async Task<Guid> ComprarVeiculoNovoAsync(string tokenCliente)
    {
        _client.DefaultRequestHeaders.Authorization = new(
            "Bearer", await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123"));
        var veiculoResponse = await _client.PostAsJsonAsync(
            "/veiculos", TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });
        veiculoResponse.EnsureSuccessStatusCode();
        var veiculo = await veiculoResponse.Content.ReadFromJsonAsync<VeiculoResult>();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);
        var compraResponse = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculo!.Id));
        compraResponse.EnsureSuccessStatusCode();
        var compra = await compraResponse.Content.ReadFromJsonAsync<CompraResult>();

        _client.DefaultRequestHeaders.Authorization = null;
        return compra!.Id;
    }
}
