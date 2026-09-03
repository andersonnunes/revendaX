using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Keycloak + Postgres reais e efêmeros, uma única instância de cada por execução da suíte —
/// compartilhados entre todas as classes de teste de integração do `vendas-api` via
/// <see cref="VendasApiIntegrationCollection"/>. Mesmo racional já aplicado em
/// `IdentityApi.Tests` (US1.5/validação de SOLID): <c>IClassFixture</c> cria uma instância por
/// classe, então sem esse compartilhamento cada classe nova (esta e as futuras do Épico 2)
/// subiria seu próprio par de containers.
///
/// Os dois containers não precisam de rede Docker compartilhada entre si (diferente do
/// Keycloak+Mailpit do `identity-api`) — só o processo de teste (fora de container) precisa
/// alcançar cada um pela porta mapeada no host.
/// </summary>
public class VendasApiTestEnvironment : IAsyncLifetime
{
    private readonly IContainer _keycloak = new ContainerBuilder("quay.io/keycloak/keycloak:26.0")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
        .WithBindMount(
            Path.Combine(AppContext.BaseDirectory, "realm-clientes.json"),
            "/opt/keycloak/data/import/realm-clientes.json",
            AccessMode.ReadOnly)
        .WithCommand("start-dev", "--import-realm")
        .WithPortBinding(8080, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(r => r.ForPath("/realms/clientes").ForPort(8080)))
        .Build();

    // Mesma imagem/tag do vendas-db em infra/docker-compose.yml — consistência entre teste e
    // ambiente real.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("vendas")
        .WithUsername("vendas")
        .WithPassword("vendas")
        .Build();

    public string KeycloakBaseUrl => $"http://{_keycloak.Hostname}:{_keycloak.GetMappedPublicPort(8080)}";

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public Task InitializeAsync() => Task.WhenAll(_keycloak.StartAsync(), _postgres.StartAsync());

    public Task DisposeAsync() => Task.WhenAll(_keycloak.DisposeAsync().AsTask(), _postgres.DisposeAsync().AsTask());
}
