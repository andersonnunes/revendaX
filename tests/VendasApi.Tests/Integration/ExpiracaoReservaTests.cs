using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Application.Compras;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Compras;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da expiração automática de reservas (US3.5) contra Postgres real — não
/// mockado. Chama <see cref="ICancelarComprasExpiradasUseCase"/> diretamente, sem esperar o
/// <c>ExpiracaoReservaBackgroundService</c>/`Task.Delay` real — testar um `BackgroundService`
/// diretamente exigiria esperar timers de verdade, lento e propenso a flakiness; a lógica de
/// negócio já está isolada no caso de uso, então é isso que o teste exercita. `CriadoEm` é
/// ajustado direto no banco para simular uma reserva antiga, mesma técnica de ajuste de estado
/// já usada em outras suítes deste projeto.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ExpiracaoReservaTests : IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(30);

    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ExpiracaoReservaTests(VendasApiTestEnvironment env)
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
    public async Task Executar_CompraPendenteExpirada_TornaCanceladaEVeiculoDisponivel()
    {
        var (compraId, veiculoId) = await CriarCompraPendenteAsync();
        await AjustarCriadoEmAsync(compraId, DateTimeOffset.UtcNow - Timeout - TimeSpan.FromMinutes(1));

        await ExecutarCancelamentoAsync();

        await using var dbContext = CriarDbContext();
        var compra = await dbContext.Compras.SingleAsync(c => c.Id == compraId);
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId);
        Assert.Equal(StatusCompra.Cancelada, compra.Status);
        Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
    }

    [Fact]
    public async Task Executar_CompraPendenteRecente_NaoAltera()
    {
        var (compraId, veiculoId) = await CriarCompraPendenteAsync();

        await ExecutarCancelamentoAsync();

        await using var dbContext = CriarDbContext();
        var compra = await dbContext.Compras.SingleAsync(c => c.Id == compraId);
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId);
        Assert.Equal(StatusCompra.Pendente, compra.Status);
        Assert.Equal(StatusVeiculo.Reservado, veiculo.Status);
    }

    [Fact]
    public async Task Executar_CompraConcluidaAntiga_NaoAltera()
    {
        var (compraId, veiculoId) = await CriarCompraPendenteAsync();
        using (var confirmacao = new HttpRequestMessage(HttpMethod.Post, $"/compras/{compraId}/confirmar-pagamento"))
        {
            confirmacao.Headers.Add("X-Webhook-Secret", VendasApiFactory.WebhookSecret);
            (await _client.SendAsync(confirmacao)).EnsureSuccessStatusCode();
        }
        await AjustarCriadoEmAsync(compraId, DateTimeOffset.UtcNow - Timeout - TimeSpan.FromMinutes(1));

        await ExecutarCancelamentoAsync();

        await using var dbContext = CriarDbContext();
        var compra = await dbContext.Compras.SingleAsync(c => c.Id == compraId);
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId);
        Assert.Equal(StatusCompra.Concluida, compra.Status);
        Assert.Equal(StatusVeiculo.Vendido, veiculo.Status);
    }

    [Fact]
    public async Task Executar_ChamadaRepetidaSobreCompraJaCancelada_NaoAlteraNemLancaErro()
    {
        var (compraId, veiculoId) = await CriarCompraPendenteAsync();
        await AjustarCriadoEmAsync(compraId, DateTimeOffset.UtcNow - Timeout - TimeSpan.FromMinutes(1));
        await ExecutarCancelamentoAsync();

        await ExecutarCancelamentoAsync(); // idempotente — não deve lançar nem mudar nada de novo

        await using var dbContext = CriarDbContext();
        var compra = await dbContext.Compras.SingleAsync(c => c.Id == compraId);
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId);
        Assert.Equal(StatusCompra.Cancelada, compra.Status);
        Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
    }

    [Fact]
    public async Task Executar_AposExpiracao_VeiculoApareceNaListagemDisponiveis()
    {
        var (compraId, veiculoId) = await CriarCompraPendenteAsync();
        await AjustarCriadoEmAsync(compraId, DateTimeOffset.UtcNow - Timeout - TimeSpan.FromMinutes(1));

        await ExecutarCancelamentoAsync();

        var disponiveis = await _client.GetFromJsonAsync<List<VeiculoResult>>("/veiculos");
        Assert.Contains(disponiveis!, v => v.Id == veiculoId);
    }

    [Fact]
    public async Task Executar_LoteComMultiplasComprasExpiradas_TodasProcessadas()
    {
        var (compraId1, veiculoId1) = await CriarCompraPendenteAsync();
        var (compraId2, veiculoId2) = await CriarCompraPendenteAsync();
        var expirado = DateTimeOffset.UtcNow - Timeout - TimeSpan.FromMinutes(1);
        await AjustarCriadoEmAsync(compraId1, expirado);
        await AjustarCriadoEmAsync(compraId2, expirado);

        await ExecutarCancelamentoAsync();

        await using var dbContext = CriarDbContext();
        Assert.Equal(StatusCompra.Cancelada, (await dbContext.Compras.SingleAsync(c => c.Id == compraId1)).Status);
        Assert.Equal(StatusCompra.Cancelada, (await dbContext.Compras.SingleAsync(c => c.Id == compraId2)).Status);
        Assert.Equal(StatusVeiculo.Disponivel, (await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId1)).Status);
        Assert.Equal(StatusVeiculo.Disponivel, (await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId2)).Status);
    }

    private async Task ExecutarCancelamentoAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICancelarComprasExpiradasUseCase>();
        await useCase.ExecutarAsync(Timeout, CancellationToken.None);
    }

    private async Task<(Guid CompraId, Guid VeiculoId)> CriarCompraPendenteAsync()
    {
        _client.DefaultRequestHeaders.Authorization = new(
            "Bearer", await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123"));
        var veiculoResponse = await _client.PostAsJsonAsync(
            "/veiculos", TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });
        veiculoResponse.EnsureSuccessStatusCode();
        var veiculo = await veiculoResponse.Content.ReadFromJsonAsync<VeiculoResult>();

        var email = $"expiracao-teste.{Guid.NewGuid():N}@example.com";
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

    private async Task AjustarCriadoEmAsync(Guid compraId, DateTimeOffset criadoEm)
    {
        await using var dbContext = CriarDbContext();
        var compra = await dbContext.Compras.SingleAsync(c => c.Id == compraId);
        dbContext.Entry(compra).Property(nameof(Compra.CriadoEm)).CurrentValue = criadoEm;
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
