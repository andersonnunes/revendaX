using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Cenários de teste da edição de veículo (`PUT /veiculos/{id}`) contra Keycloak e Postgres
/// reais — não mockados.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class EditarVeiculoTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public EditarVeiculoTests(VendasApiTestEnvironment env)
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
    public async Task Editar_VeiculoDisponivelComDadosValidos_Retorna200ComDadosAtualizados()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        Assert.NotNull(body);
        Assert.Equal("Prata", body!.Cor);
        Assert.Equal(85000.00m, body.Preco);
    }

    [Fact]
    public async Task Editar_VeiculoDisponivel_PersisteNoBancoReal()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida());
        response.EnsureSuccessStatusCode();

        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        Assert.Equal("Mobi", veiculo.Modelo);
        Assert.Equal(2025, veiculo.Ano);
        Assert.Equal("Prata", veiculo.Cor);
        Assert.Equal(85000.00m, veiculo.Preco);
    }

    [Fact]
    public async Task Editar_VeiculoDisponivel_NaoAlteraPlacaNemStatus()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();
        var placaOriginal = (await CadastroAtualAsync(id)).Placa;

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida());

        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        Assert.Equal(placaOriginal, body!.Placa);
        Assert.Equal("Disponivel", body.Status);
    }

    [Fact]
    public async Task Editar_VeiculoVendido_Retorna409ENaoAlteraDados()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();
        await MarcarComoVendidoAsync(id);

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        Assert.NotEqual("Mobi", veiculo.Modelo);
    }

    [Fact]
    public async Task Editar_IdInexistente_Retorna404()
    {
        await AutenticarComoVendedorAsync();

        var response = await _client.PutAsJsonAsync($"/veiculos/{Guid.NewGuid()}", NovaEdicaoValida());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Editar_AnoInvalido_Retorna422()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida() with { Ano = 1800 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Editar_PrecoInvalido_Retorna422()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida() with { Preco = 0 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Editar_CampoObrigatorioAusente_Retorna400()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida() with { Marca = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Editar_SemToken_Retorna401()
    {
        var response = await _client.PutAsJsonAsync($"/veiculos/{Guid.NewGuid()}", NovaEdicaoValida());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Editar_TokenDeComprador_Retorna403()
    {
        await AutenticarComoVendedorAsync();
        var id = await CadastrarVeiculoAsync();

        var email = $"vendas-teste.{Guid.NewGuid():N}@example.com";
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.CriarUsuarioAsync(adminToken, email, "SenhaForte123");
        var token = await _keycloakHelper.LoginAsync(email, "SenhaForte123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await _client.PutAsJsonAsync($"/veiculos/{id}", NovaEdicaoValida());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static EditarVeiculoRequestDto NovaEdicaoValida() => new(
        Marca: "Fiat", Modelo: "Mobi", Ano: 2025, Cor: "Prata", Preco: 85000.00m);

    private async Task<Guid> CadastrarVeiculoAsync()
    {
        var response = await _client.PostAsJsonAsync("/veiculos", TestData.NovoVeiculoValido());
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        return body!.Id;
    }

    private async Task<VeiculoResult> CadastroAtualAsync(Guid id)
    {
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        return new VeiculoResult { Id = veiculo.Id, Placa = veiculo.Placa, Status = veiculo.Status.ToString() };
    }

    /// <summary>
    /// Não há (ainda) endpoint que leve um veículo a `Vendido` — essa transição só chega no
    /// Épico 3. Ajusta o status direto no banco de teste, mesmo padrão já registrado nos
    /// refinamentos das US2.3/US2.4 para `Reservado`/`Vendido`.
    /// </summary>
    private async Task MarcarComoVendidoAsync(Guid id)
    {
        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == id);
        dbContext.Entry(veiculo).Property(nameof(Veiculo.Status)).CurrentValue = StatusVeiculo.Vendido;
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

/// <summary>Corpo de PUT /veiculos/{id} usado pelos testes — sem `placa`/`status`, de propósito (ver refinamento da US2.2).</summary>
public record EditarVeiculoRequestDto(string Marca, string Modelo, int Ano, string Cor, decimal Preco);
