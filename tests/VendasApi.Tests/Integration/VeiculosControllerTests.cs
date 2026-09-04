using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste do cadastro de veículo (`POST /veiculos`) contra Keycloak e Postgres
/// reais — não mockados.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class VeiculosControllerTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public VeiculosControllerTests(VendasApiTestEnvironment env)
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
    public async Task Cadastrar_DadosValidos_Retorna201ComStatusDisponivel()
    {
        await AutenticarComoVendedorAsync();
        var request = TestData.NovoVeiculoValido();

        var response = await _client.PostAsJsonAsync("/veiculos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        Assert.NotNull(body);
        Assert.Equal("Disponivel", body!.Status);
        Assert.Equal(request.Placa, body.Placa);
    }

    [Fact]
    public async Task Cadastrar_DadosValidos_PersisteNoBancoReal()
    {
        await AutenticarComoVendedorAsync();
        var request = TestData.NovoVeiculoValido();

        var response = await _client.PostAsJsonAsync("/veiculos", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();

        // Confere direto no banco (não confia só na resposta HTTP do próprio serviço que
        // gravou) — mesmo racional já usado no identity-api pra confirmar e-mail no Mailpit.
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == body!.Id);
        Assert.Equal(request.Marca, veiculo.Marca);
        Assert.Equal(request.Modelo, veiculo.Modelo);
        Assert.Equal(request.Ano, veiculo.Ano);
        Assert.Equal(request.Cor, veiculo.Cor);
        Assert.Equal(request.Preco, veiculo.Preco);
        Assert.Equal(request.Placa, veiculo.Placa);
        Assert.True(veiculo.Ativo);
    }

    [Fact]
    public async Task Cadastrar_PlacaDuplicada_Retorna409()
    {
        await AutenticarComoVendedorAsync();
        var request = TestData.NovoVeiculoValido();
        var primeira = await _client.PostAsJsonAsync("/veiculos", request);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        var segunda = await _client.PostAsJsonAsync("/veiculos", request with { Modelo = "Mobi" });

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_AnoAnteriorAoMinimo_Retorna422()
    {
        await AutenticarComoVendedorAsync();
        var request = TestData.NovoVeiculoValido() with { Ano = 1800 };

        var response = await _client.PostAsJsonAsync("/veiculos", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_PrecoZero_Retorna422()
    {
        await AutenticarComoVendedorAsync();
        var request = TestData.NovoVeiculoValido() with { Preco = 0 };

        var response = await _client.PostAsJsonAsync("/veiculos", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_PlacaFormatoInvalido_Retorna422()
    {
        await AutenticarComoVendedorAsync();
        var request = TestData.NovoVeiculoValido() with { Placa = "1234ABC" };

        var response = await _client.PostAsJsonAsync("/veiculos", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_CampoObrigatorioAusente_Retorna400()
    {
        await AutenticarComoVendedorAsync();
        var request = TestData.NovoVeiculoValido() with { Marca = "" };

        var response = await _client.PostAsJsonAsync("/veiculos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_SemToken_Retorna401()
    {
        var response = await _client.PostAsJsonAsync("/veiculos", TestData.NovoVeiculoValido());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_TokenDeComprador_Retorna403()
    {
        var email = $"vendas-teste.{Guid.NewGuid():N}@example.com";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, "SenhaForte123");
        var token = await _keycloakHelper.LoginAsync(email, "SenhaForte123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.PostAsJsonAsync("/veiculos", TestData.NovoVeiculoValido());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Usuário semeado no export do realm (US1.5), não criado por este teste.</summary>
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
