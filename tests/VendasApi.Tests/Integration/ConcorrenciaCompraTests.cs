using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste de concorrência na compra (US3.2), contra Postgres real (Testcontainers)
/// — a maioria via requisições HTTP concorrentes reais; o cenário de edição-vs-compra usa
/// `DbContext` diretamente por não ser confiável forçar a corrida real via HTTP (ver
/// doc-comment do teste).
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ConcorrenciaCompraTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ConcorrenciaCompraTests(VendasApiTestEnvironment env)
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
    public async Task Iniciar_DuasRequisicoesConcorrentesParaOMesmoVeiculo_ApenasUmaCria201()
    {
        var veiculoId = await CadastrarVeiculoComoVendedorAsync();
        var token = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var respostas = await Task.WhenAll(
            _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId)),
            _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId)));

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        await using var dbContext = CriarDbContext();
        var compras = await dbContext.Compras.Where(c => c.VeiculoId == veiculoId).ToListAsync();
        Assert.Single(compras);
    }

    [Fact]
    public async Task Iniciar_CincoRequisicoesConcorrentesParaOMesmoVeiculo_ApenasUmaCria201()
    {
        var veiculoId = await CadastrarVeiculoComoVendedorAsync();
        var token = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId))));

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(4, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Iniciar_VeiculosDiferentesConcorrentemente_AmbasCriam201()
    {
        var veiculo1 = await CadastrarVeiculoComoVendedorAsync();
        var veiculo2 = await CadastrarVeiculoComoVendedorAsync();
        var token = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var respostas = await Task.WhenAll(
            _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculo1)),
            _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculo2)));

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
    }

    /// <summary>
    /// Duas requisições HTTP concorrentes de tipos diferentes (`PUT` de edição e `POST` de
    /// compra) não têm garantia de colidir de fato no tempo — via `Task.WhenAll` uma pode
    /// completar inteira (ler e gravar) antes da outra sequer ler o veículo, sem nenhuma
    /// corrida real acontecer (observado: teste ficava instável, passava só quando as duas
    /// por acaso se sobrepunham). Em vez de depender de timing não determinístico, este teste
    /// força a corrida diretamente: dois `DbContext` leem o mesmo veículo, um grava primeiro
    /// (muda o `xmin` físico da linha), o outro tenta gravar por cima do valor já desatualizado
    /// — prova o mecanismo de concorrência em si, não uma coincidência de agendamento de duas
    /// requisições HTTP. As outras duas requisições reais (compra-vs-compra, acima) já provam
    /// que o mesmo mecanismo aparece como 409 na API quando a corrida de fato acontece.
    /// </summary>
    [Fact]
    public async Task Atualizacoes_ConcorrentesNaMesmaLinha_SegundaLancaDbUpdateConcurrencyException()
    {
        var veiculoId = await CadastrarVeiculoComoVendedorAsync();

        await using var contextoA = CriarDbContext();
        await using var contextoB = CriarDbContext();
        var veiculoLidoPorA = await contextoA.Veiculos.SingleAsync(v => v.Id == veiculoId);
        var veiculoLidoPorB = await contextoB.Veiculos.SingleAsync(v => v.Id == veiculoId);

        // B grava primeiro — muda o xmin físico da linha.
        contextoB.Entry(veiculoLidoPorB).Property("Preco").CurrentValue = TestData.GerarPrecoUnico();
        await contextoB.SaveChangesAsync();

        // A ainda carrega o xmin de antes da gravação de B — a gravação de A deve falhar.
        contextoA.Entry(veiculoLidoPorA).Property("Preco").CurrentValue = TestData.GerarPrecoUnico();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextoA.SaveChangesAsync());
    }

    private async Task<Guid> CadastrarVeiculoComoVendedorAsync()
    {
        var token = await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await _client.PostAsJsonAsync(
            "/veiculos", TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        return body!.Id;
    }

    private async Task<string> CriarClienteEFazerLoginAsync()
    {
        var email = $"concorrencia-teste.{Guid.NewGuid():N}@example.com";
        const string senha = "SenhaForte123";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
        return await _keycloakHelper.LoginAsync(email, senha);
    }

    private VendasDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<VendasDbContext>()
            .UseNpgsql(_env.PostgresConnectionString)
            .Options;
        return new VendasDbContext(options);
    }
}
