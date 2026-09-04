using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Compras;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Compras;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da confirmação de pagamento (`POST /compras/{id}/confirmar-pagamento`,
/// US3.3) contra Keycloak e Postgres reais — não mockados.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ConfirmarPagamentoTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ConfirmarPagamentoTests(VendasApiTestEnvironment env)
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
    public async Task ConfirmarPagamento_CompraPendente_Retorna200ETornaCompraConcluidaEVeiculoVendido()
    {
        var (compraId, veiculoId) = await CriarCompraPendenteAsync();

        var response = await ConfirmarPagamentoAsync(compraId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var compra = await response.Content.ReadFromJsonAsync<CompraResult>();
        Assert.Equal("Concluida", compra!.Status);

        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId);
        Assert.Equal(StatusVeiculo.Vendido, veiculo.Status);
    }

    [Fact]
    public async Task ConfirmarPagamento_CompraJaConcluida_Retorna200Idempotente()
    {
        var (compraId, _) = await CriarCompraPendenteAsync();
        await ConfirmarPagamentoAsync(compraId);

        var response = await ConfirmarPagamentoAsync(compraId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var compra = await response.Content.ReadFromJsonAsync<CompraResult>();
        Assert.Equal("Concluida", compra!.Status);
    }

    [Fact]
    public async Task ConfirmarPagamento_CompraInexistente_Retorna404()
    {
        var response = await ConfirmarPagamentoAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmarPagamento_CompraCancelada_Retorna409()
    {
        var (compraId, _) = await CriarCompraPendenteAsync();
        await AjustarStatusCompraAsync(compraId, StatusCompra.Cancelada);

        var response = await ConfirmarPagamentoAsync(compraId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmarPagamento_SemHeaderSegredo_Retorna401()
    {
        var (compraId, _) = await CriarCompraPendenteAsync();

        var response = await _client.PostAsync($"/compras/{compraId}/confirmar-pagamento", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmarPagamento_ComHeaderSegredoErrado_Retorna401()
    {
        var (compraId, _) = await CriarCompraPendenteAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/compras/{compraId}/confirmar-pagamento");
        request.Headers.Add("X-Webhook-Secret", "segredo-errado");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmarPagamento_AposConfirmacao_VeiculoApareceEmVendidosENaoEmDisponiveis()
    {
        var (compraId, veiculoId) = await CriarCompraPendenteAsync();
        await ConfirmarPagamentoAsync(compraId);

        _client.DefaultRequestHeaders.Authorization = new("Bearer", await ObterTokenVendedorAsync());
        var vendidos = await _client.GetFromJsonAsync<List<VeiculoResult>>("/veiculos/vendidos");
        var disponiveis = await _client.GetFromJsonAsync<List<VeiculoResult>>("/veiculos");

        Assert.Contains(vendidos!, v => v.Id == veiculoId);
        Assert.DoesNotContain(disponiveis!, v => v.Id == veiculoId);
    }

    private async Task<HttpResponseMessage> ConfirmarPagamentoAsync(Guid compraId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/compras/{compraId}/confirmar-pagamento");
        request.Headers.Add("X-Webhook-Secret", VendasApiFactory.WebhookSecret);
        return await _client.SendAsync(request);
    }

    private async Task<(Guid CompraId, Guid VeiculoId)> CriarCompraPendenteAsync()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", await ObterTokenVendedorAsync());
        var veiculoResponse = await _client.PostAsJsonAsync(
            "/veiculos", TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });
        veiculoResponse.EnsureSuccessStatusCode();
        var veiculo = await veiculoResponse.Content.ReadFromJsonAsync<VeiculoResult>();

        var email = $"pagamento-teste.{Guid.NewGuid():N}@example.com";
        const string senha = "SenhaForte123";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
        var tokenCliente = await _keycloakHelper.LoginAsync(email, senha);
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);

        var compraResponse = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculo!.Id));
        compraResponse.EnsureSuccessStatusCode();
        var compra = await compraResponse.Content.ReadFromJsonAsync<CompraResult>();

        _client.DefaultRequestHeaders.Authorization = null;
        return (compra!.Id, veiculo.Id);
    }

    private async Task<string> ObterTokenVendedorAsync() =>
        await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123");

    /// <summary>Não há (ainda) endpoint que leve uma compra a `Cancelada` — essa transição só chega na US3.5.</summary>
    private async Task AjustarStatusCompraAsync(Guid id, StatusCompra status)
    {
        await using var dbContext = CriarDbContext();
        var compra = await dbContext.Compras.SingleAsync(c => c.Id == id);
        dbContext.Entry(compra).Property(nameof(Compra.Status)).CurrentValue = status;
        await dbContext.SaveChangesAsync();
    }

    private VendasDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<VendasDbContext>()
            .UseNpgsql(_env.PostgresConnectionString)
            .Options;
        return new VendasDbContext(options);
    }
}
