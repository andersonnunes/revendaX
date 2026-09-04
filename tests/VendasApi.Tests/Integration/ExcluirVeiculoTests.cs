using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da exclusão/soft delete de veículo (`DELETE /veiculos/{id}`, US2.5)
/// contra Keycloak e Postgres reais — não mockados.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ExcluirVeiculoTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ExcluirVeiculoTests(VendasApiTestEnvironment env)
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
    public async Task Excluir_VeiculoDisponivel_Retorna204EMarcaAtivoFalseNoBanco()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var response = await _client.DeleteAsync($"/veiculos/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        Assert.False(veiculo.Ativo);
    }

    [Fact]
    public async Task Excluir_VeiculoDisponivel_SomeDaListagemPublica()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        await _client.DeleteAsync($"/veiculos/{id}");
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/veiculos");
        var veiculos = await response.Content.ReadFromJsonAsync<List<VeiculoResult>>() ?? [];

        Assert.DoesNotContain(veiculos, v => v.Id == id);
    }

    [Fact]
    public async Task Excluir_MesmoVeiculoDuasVezes_Retorna204NasDuasChamadas()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var primeira = await _client.DeleteAsync($"/veiculos/{id}");
        var segunda = await _client.DeleteAsync($"/veiculos/{id}");

        Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
    }

    [Theory]
    [InlineData(StatusVeiculo.Reservado)]
    [InlineData(StatusVeiculo.Vendido)]
    public async Task Excluir_VeiculoNaoDisponivel_Retorna409EAtivoContinuaTrue(StatusVeiculo status)
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();
        await AjustarStatusAsync(id, status);

        var response = await _client.DeleteAsync($"/veiculos/{id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        Assert.True(veiculo.Ativo);
    }

    [Fact]
    public async Task Excluir_IdInexistente_Retorna404()
    {
        await AutenticarComoVendedorAsync();

        var response = await _client.DeleteAsync($"/veiculos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Excluir_SemToken_Retorna401()
    {
        var response = await _client.DeleteAsync($"/veiculos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Excluir_TokenDeComprador_Retorna403()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var email = $"vendas-teste.{Guid.NewGuid():N}@example.com";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, "SenhaForte123");
        var token = await _keycloakHelper.LoginAsync(email, "SenhaForte123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.DeleteAsync($"/veiculos/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> CadastrarVeiculoAsync()
    {
        var response = await _client.PostAsJsonAsync("/veiculos", TestData.NovoVeiculoValido() with { Preco = TestData.GerarPrecoUnico() });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        return body!.Id;
    }

    /// <summary>Não há (ainda) endpoint que leve um veículo a `Reservado`/`Vendido` — só chega no Épico 3.</summary>
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
