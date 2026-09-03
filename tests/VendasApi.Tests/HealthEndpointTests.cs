using System.Net;
using VendasApi.Tests.Integration;

namespace VendasApi.Tests;

/// <summary>
/// Precisa do Keycloak/Postgres reais (via <see cref="VendasApiIntegrationCollection"/>) desde
/// a US2.1 — o próprio startup do `vendas-api` já migra o schema do banco antes de responder a
/// qualquer requisição (`Program.cs`), então nem `/health` sobe sem uma connection string real.
/// </summary>
[Collection(nameof(VendasApiIntegrationCollection))]
public class HealthEndpointTests : IAsyncLifetime
{
    private readonly VendasApiTestEnvironment _env;
    private VendasApiFactory _factory = null!;

    public HealthEndpointTests(VendasApiTestEnvironment env)
    {
        _env = env;
    }

    public Task InitializeAsync()
    {
        _factory = new VendasApiFactory(_env.KeycloakBaseUrl, _env.PostgresConnectionString);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Health_Returns200Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
