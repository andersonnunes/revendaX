using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da listagem restrita de veículos vendidos (`GET /veiculos/vendidos`,
/// US2.4) contra Keycloak e Postgres reais — não mockados.
///
/// Mesma ressalva de estado compartilhado já registrada em
/// <see cref="ListarVeiculosDisponiveisTests"/>: os testes filtram a resposta pelos ids que o
/// próprio teste criou, não assumem que a lista inteira é só o que ele cadastrou.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ListarVeiculosVendidosTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ListarVeiculosVendidosTests(VendasApiTestEnvironment env)
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
    public async Task ListarVendidos_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/veiculos/vendidos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListarVendidos_TokenDeComprador_Retorna403()
    {
        var email = $"vendas-teste.{Guid.NewGuid():N}@example.com";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, "SenhaForte123");
        var token = await _keycloakHelper.LoginAsync(email, "SenhaForte123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.GetAsync("/veiculos/vendidos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListarVendidos_TokenDeVendedor_Retorna200()
    {
        await AutenticarComoVendedorAsync();

        var response = await _client.GetAsync("/veiculos/vendidos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListarVendidos_VeiculoVendido_Aparece()
    {
        var id = await CadastrarVeiculoAsync(TestData.GerarPrecoUnico());
        await MarcarComoVendidoAsync(id);
        await AutenticarComoVendedorAsync();

        var veiculos = await ListarVendidosAsync();

        Assert.Contains(veiculos, v => v.Id == id);
    }

    [Theory]
    [InlineData(StatusVeiculo.Disponivel)]
    [InlineData(StatusVeiculo.Reservado)]
    public async Task ListarVendidos_VeiculoNaoVendido_NaoAparece(StatusVeiculo status)
    {
        var id = await CadastrarVeiculoAsync(TestData.GerarPrecoUnico());
        if (status != StatusVeiculo.Disponivel)
        {
            await AjustarStatusAsync(id, status);
        }
        await AutenticarComoVendedorAsync();

        var veiculos = await ListarVendidosAsync();

        Assert.DoesNotContain(veiculos, v => v.Id == id);
    }

    [Fact]
    public async Task ListarVendidos_TresVeiculosComPrecosDistintos_AparecemEmOrdemCrescenteEntreSi()
    {
        var precoBaixo = TestData.GerarPrecoUnico();
        var precoMedio = precoBaixo + 10_000;
        var precoAlto = precoBaixo + 20_000;

        var idAlto = await CadastrarEMarcarComoVendidoAsync(precoAlto);
        var idBaixo = await CadastrarEMarcarComoVendidoAsync(precoBaixo);
        var idMedio = await CadastrarEMarcarComoVendidoAsync(precoMedio);
        await AutenticarComoVendedorAsync();

        var veiculos = await ListarVendidosAsync();

        var ordemDosTres = veiculos.Where(v => v.Id == idBaixo || v.Id == idMedio || v.Id == idAlto).Select(v => v.Id).ToList();
        Assert.Equal([idBaixo, idMedio, idAlto], ordemDosTres);
    }

    private async Task<Guid> CadastrarEMarcarComoVendidoAsync(decimal preco)
    {
        var id = await CadastrarVeiculoAsync(preco);
        await MarcarComoVendidoAsync(id);
        return id;
    }

    private async Task<Guid> CadastrarVeiculoAsync(decimal preco)
    {
        var token = await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.PostAsJsonAsync("/veiculos", TestData.NovoVeiculoValido() with { Preco = preco });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        return body!.Id;
    }

    private async Task<List<VeiculoResult>> ListarVendidosAsync()
    {
        var response = await _client.GetAsync("/veiculos/vendidos");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<VeiculoResult>>() ?? [];
    }

    /// <summary>Não há (ainda) endpoint que leve um veículo a `Vendido`/`Reservado` — só chega no Épico 3.</summary>
    private async Task MarcarComoVendidoAsync(Guid id) => await AjustarStatusAsync(id, StatusVeiculo.Vendido);

    private async Task AjustarStatusAsync(Guid id, StatusVeiculo status)
    {
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        dbContext.Entry(veiculo).Property(nameof(Veiculo.Status)).CurrentValue = status;
        await dbContext.SaveChangesAsync();
    }

    private async Task AutenticarComoVendedorAsync()
    {
        var token = await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    private VendasDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<VendasDbContext>()
            .UseNpgsql(_env.PostgresConnectionString)
            .Options;
        return new VendasDbContext(options);
    }
}
