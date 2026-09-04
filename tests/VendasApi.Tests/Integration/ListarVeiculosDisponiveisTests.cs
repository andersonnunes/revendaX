using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da listagem pública de veículos à venda (`GET /veiculos`, US2.3) contra
/// Postgres real — não mockado.
///
/// A suíte inteira compartilha um único Postgres (ver <see cref="VendasApiTestEnvironment"/>),
/// então "lista vazia quando não há nenhum veículo `Disponivel`" não é um cenário testável de
/// forma confiável aqui — outros testes já terão cadastrado veículos antes deste rodar, sem
/// ordem garantida entre classes. Os testes abaixo verificam as mesmas propriedades de um
/// jeito robusto a esse estado compartilhado: filtram a resposta pelos ids que o próprio
/// teste criou, em vez de assumir que a lista inteira é só o que ele cadastrou.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ListarVeiculosDisponiveisTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ListarVeiculosDisponiveisTests(VendasApiTestEnvironment env)
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
    public async Task ListarDisponiveis_SemToken_Retorna200()
    {
        var response = await _client.GetAsync("/veiculos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListarDisponiveis_ListaCompleta_SempreOrdenadaPorPrecoAscendente()
    {
        await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });

        var veiculos = await ListarAsync();

        var precos = veiculos.Select(v => v.Preco).ToList();
        Assert.Equal(precos.OrderBy(p => p), precos);
    }

    [Fact]
    public async Task ListarDisponiveis_TresVeiculosComPrecosDistintos_AparecemEmOrdemCrescenteEntreSi()
    {
        var precoBaixo = TestData.GerarPrecoUnico();
        var precoMedio = precoBaixo + 10_000;
        var precoAlto = precoBaixo + 20_000;

        var idAlto = await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = precoAlto });
        var idBaixo = await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = precoBaixo });
        var idMedio = await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = precoMedio });

        var veiculos = await ListarAsync();

        var ordemDosTres = veiculos.Where(v => v.Id == idBaixo || v.Id == idMedio || v.Id == idAlto).Select(v => v.Id).ToList();
        Assert.Equal([idBaixo, idMedio, idAlto], ordemDosTres);
    }

    [Fact]
    public async Task ListarDisponiveis_DoisComMesmoPreco_DesempatamPorCriadoEmAscendente()
    {
        var precoEmpatado = TestData.GerarPrecoUnico();

        var idPrimeiro = await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = precoEmpatado });
        var idSegundo = await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = precoEmpatado });

        var veiculos = await ListarAsync();

        var ordemDosDois = veiculos.Where(v => v.Id == idPrimeiro || v.Id == idSegundo).Select(v => v.Id).ToList();
        Assert.Equal([idPrimeiro, idSegundo], ordemDosDois);
    }

    [Fact]
    public async Task ListarDisponiveis_VeiculoDisponivel_Aparece()
    {
        var id = await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });

        var veiculos = await ListarAsync();

        Assert.Contains(veiculos, v => v.Id == id);
    }

    [Theory]
    [InlineData(StatusVeiculo.Reservado)]
    [InlineData(StatusVeiculo.Vendido)]
    public async Task ListarDisponiveis_VeiculoNaoDisponivel_NaoAparece(StatusVeiculo status)
    {
        var id = await CadastrarComoVendedorAsync(TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });
        await AjustarStatusAsync(id, status);

        var veiculos = await ListarAsync();

        Assert.DoesNotContain(veiculos, v => v.Id == id);
    }

    private async Task<Guid> CadastrarComoVendedorAsync(VeiculoRequestDto request)
    {
        var token = await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.PostAsJsonAsync("/veiculos", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();

        _client.DefaultRequestHeaders.Authorization = null; // GET /veiculos é público — não usa token nenhum
        return body!.Id;
    }

    private async Task<List<VeiculoResult>> ListarAsync()
    {
        var response = await _client.GetAsync("/veiculos");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<VeiculoResult>>() ?? [];
    }

    /// <summary>
    /// Não há (ainda) endpoint que leve um veículo a `Reservado`/`Vendido` — só chega no
    /// Épico 3. Ajusta o status direto no banco de teste, mesmo padrão já usado em
    /// `EditarVeiculoTests`.
    /// </summary>
    private async Task AjustarStatusAsync(Guid id, StatusVeiculo status)
    {
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        dbContext.Entry(veiculo).Property(nameof(Veiculo.Status)).CurrentValue = status;
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
