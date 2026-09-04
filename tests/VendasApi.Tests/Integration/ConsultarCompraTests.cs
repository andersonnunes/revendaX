using System.Net;
using System.Net.Http.Json;
using VendasApi.Application.Compras;
using VendasApi.Application.Veiculos;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da consulta de status da compra (`GET /compras/{id}`, US3.4) contra
/// Keycloak e Postgres reais — não mockados.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ConsultarCompraTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ConsultarCompraTests(VendasApiTestEnvironment env)
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
    public async Task Consultar_DonoDeCompraPendente_Retorna200ComStatusPendente()
    {
        var (compraId, _, tokenDono) = await CriarCompraPendenteAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenDono);

        var response = await _client.GetAsync($"/compras/{compraId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var compra = await response.Content.ReadFromJsonAsync<CompraResult>();
        Assert.Equal("Pendente", compra!.Status);
        Assert.Equal(compraId, compra.Id);
    }

    [Fact]
    public async Task Consultar_DonoDeCompraConcluida_Retorna200ComStatusConcluida()
    {
        var (compraId, _, tokenDono) = await CriarCompraPendenteAsync();
        using (var confirmacao = new HttpRequestMessage(HttpMethod.Post, $"/compras/{compraId}/confirmar-pagamento"))
        {
            confirmacao.Headers.Add("X-Webhook-Secret", VendasApiFactory.WebhookSecret);
            (await _client.SendAsync(confirmacao)).EnsureSuccessStatusCode();
        }

        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenDono);
        var response = await _client.GetAsync($"/compras/{compraId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var compra = await response.Content.ReadFromJsonAsync<CompraResult>();
        Assert.Equal("Concluida", compra!.Status);
    }

    [Fact]
    public async Task Consultar_IdInexistente_Retorna404()
    {
        var (_, _, tokenDono) = await CriarCompraPendenteAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenDono);

        var response = await _client.GetAsync($"/compras/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Consultar_ClienteDiferenteDoDono_Retorna404()
    {
        var (compraId, _, _) = await CriarCompraPendenteAsync();
        var email = $"consulta-teste.{Guid.NewGuid():N}@example.com";
        const string senha = "SenhaForte123";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
        var tokenOutroCliente = await _keycloakHelper.LoginAsync(email, senha);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenOutroCliente);

        var response = await _client.GetAsync($"/compras/{compraId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Consultar_SemToken_Retorna401()
    {
        var (compraId, _, _) = await CriarCompraPendenteAsync();

        var response = await _client.GetAsync($"/compras/{compraId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Mesma ressalva já registrada em <see cref="ComprasControllerTests"/>: o usuário
    /// `vendedor` semeado também carrega a role `cliente` (default role de todo usuário do
    /// realm), então este cenário remove `cliente` do composite do realm temporariamente em
    /// vez de usar o token de vendedor sozinho.
    /// </summary>
    [Fact]
    public async Task Consultar_TokenSemRoleCliente_Retorna403()
    {
        var (compraId, _, _) = await CriarCompraPendenteAsync();
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.RemoverClienteDoDefaultRoleAsync(adminToken);
        try
        {
            var email = $"consulta-sem-role.{Guid.NewGuid():N}@example.com";
            const string senha = "SenhaForte123";
            await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
            var token = await _keycloakHelper.LoginAsync(email, senha);
            _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var response = await _client.GetAsync($"/compras/{compraId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await _keycloakHelper.RestaurarClienteNoDefaultRoleAsync(adminToken);
        }
    }

    private async Task<(Guid CompraId, Guid VeiculoId, string TokenDono)> CriarCompraPendenteAsync()
    {
        _client.DefaultRequestHeaders.Authorization = new(
            "Bearer", await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123"));
        var veiculoResponse = await _client.PostAsJsonAsync(
            "/veiculos", TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });
        veiculoResponse.EnsureSuccessStatusCode();
        var veiculo = await veiculoResponse.Content.ReadFromJsonAsync<VeiculoResult>();

        var email = $"consulta-dono.{Guid.NewGuid():N}@example.com";
        const string senha = "SenhaForte123";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
        var tokenDono = await _keycloakHelper.LoginAsync(email, senha);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenDono);

        var compraResponse = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculo!.Id));
        compraResponse.EnsureSuccessStatusCode();
        var compra = await compraResponse.Content.ReadFromJsonAsync<CompraResult>();

        _client.DefaultRequestHeaders.Authorization = null;
        return (compra!.Id, veiculo.Id, tokenDono);
    }
}
