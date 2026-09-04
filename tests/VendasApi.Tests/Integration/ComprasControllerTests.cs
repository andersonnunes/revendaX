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
/// Cenários de teste do início de compra (`POST /compras`, US3.1) contra Keycloak e Postgres
/// reais — não mockados.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class ComprasControllerTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;
    private HttpClient _client = null!;
    private KeycloakTestHelper _keycloakHelper = null!;

    public ComprasControllerTests(VendasApiTestEnvironment env)
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
    public async Task Iniciar_VeiculoDisponivel_Retorna201EVeiculoFicaReservado()
    {
        var veiculoId = await CadastrarVeiculoComoVendedorAsync();
        var (_, tokenCliente) = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);

        var response = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var compra = await response.Content.ReadFromJsonAsync<CompraResult>();
        Assert.Equal("Pendente", compra!.Status);
        Assert.Equal(veiculoId, compra.VeiculoId);

        await using var dbContext = CriarDbContext();
        var veiculo = await dbContext.Veiculos.SingleAsync(v => v.Id == veiculoId);
        Assert.Equal(StatusVeiculo.Reservado, veiculo.Status);
    }

    [Theory]
    [InlineData(StatusVeiculo.Reservado)]
    [InlineData(StatusVeiculo.Vendido)]
    public async Task Iniciar_VeiculoNaoDisponivel_Retorna409(StatusVeiculo status)
    {
        var veiculoId = await CadastrarVeiculoComoVendedorAsync();
        await AjustarStatusVeiculoAsync(veiculoId, status);
        var (_, tokenCliente) = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);

        var response = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_VeiculoInexistente_Retorna404()
    {
        var (_, tokenCliente) = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);

        var response = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_VeiculoIdAusente_Retorna400()
    {
        var (_, tokenCliente) = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);

        var response = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Iniciar_SemToken_Retorna401()
    {
        var response = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Não usa o usuário `vendedor` semeado para este cenário: ele também carrega a role
    /// `cliente` (todo usuário do realm recebe `default-roles-clientes`, que inclui
    /// `cliente`), então um token de vendedor sozinho não prova "sem a role cliente". Em vez
    /// disso, remove `cliente` do composite do realm temporariamente (mesma técnica já usada
    /// em <see cref="WhoAmITests"/>), testando a autorização de fato: token autenticado sem a
    /// role exigida.
    /// </summary>
    [Fact]
    public async Task Iniciar_TokenSemRoleCliente_Retorna403()
    {
        var veiculoId = await CadastrarVeiculoComoVendedorAsync();
        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        await _keycloakHelper.RemoverClienteDoDefaultRoleAsync(adminToken);
        try
        {
            var (_, token) = await CriarClienteEFazerLoginAsync();
            _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var response = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await _keycloakHelper.RestaurarClienteNoDefaultRoleAsync(adminToken);
        }
    }

    [Fact]
    public async Task Iniciar_GravaClienteIdDoSubDoToken()
    {
        var veiculoId = await CadastrarVeiculoComoVendedorAsync();
        var (clienteId, tokenCliente) = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);

        var response = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId));

        var compra = await response.Content.ReadFromJsonAsync<CompraResult>();
        Assert.Equal(clienteId, compra!.ClienteId);
    }

    [Fact]
    public async Task Iniciar_GravaPrecoComoSnapshot_EdicaoPosteriorDoVeiculoNaoAltera()
    {
        var precoOriginal = TestData.GerarPrecoUnico();
        var veiculoId = await CadastrarVeiculoComoVendedorAsync(precoOriginal);
        var (_, tokenCliente) = await CriarClienteEFazerLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenCliente);

        var compraResponse = await _client.PostAsJsonAsync("/compras", new CompraRequestDto(veiculoId));
        var compra = await compraResponse.Content.ReadFromJsonAsync<CompraResult>();

        // Veículo Reservado continua editável (US2.2) — o preço pode mudar depois da compra criada.
        await AutenticarComoVendedorAsync();
        await _client.PutAsJsonAsync(
            $"/veiculos/{veiculoId}",
            new EditarVeiculoRequestDto("Fiat", "Argo", 2024, "Branco", TestData.GerarPrecoUnico()));

        await using var dbContext = CriarDbContext();
        var compraNoBanco = await dbContext.Compras.SingleAsync(c => c.Id == compra!.Id);
        Assert.Equal(precoOriginal, compraNoBanco.Preco);
    }

    private async Task<Guid> CadastrarVeiculoComoVendedorAsync(decimal? preco = null)
    {
        await AutenticarComoVendedorAsync();
        var response = await _client.PostAsJsonAsync(
            "/veiculos", TestData.NovoVeiculoValido() with { Preco = preco ?? TestData.GerarPrecoUnico() });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<VeiculoResult>();
        return body!.Id;
    }

    private async Task<(string ClienteId, string Token)> CriarClienteEFazerLoginAsync()
    {
        var email = $"compra-teste.{Guid.NewGuid():N}@example.com";
        const string senha = "SenhaForte123";

        var adminToken = await _keycloakHelper.ObterTokenAdminAsync();
        var clienteId = await _keycloakHelper.CriarUsuarioAsync(adminToken, email, senha);
        var token = await _keycloakHelper.LoginAsync(email, senha);
        return (clienteId, token);
    }

    private async Task AutenticarComoVendedorAsync()
    {
        var token = await _keycloakHelper.LoginAsync("vendedor@revendax.local", "VendedorDev123");
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    /// <summary>Não há (ainda) endpoint que leve um veículo a `Reservado`/`Vendido` fora do fluxo de compra em teste.</summary>
    private async Task AjustarStatusVeiculoAsync(Guid id, StatusVeiculo status)
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

/// <summary>Corpo de POST /compras usado pelos testes.</summary>
public record CompraRequestDto(Guid VeiculoId);
