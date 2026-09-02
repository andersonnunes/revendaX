using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace VendasApi.Tests.Integration;

/// <summary>
/// Sobe um Keycloak real e efêmero, com o realm `infra/keycloak/realm-clientes.json`
/// importado — mesmo padrão usado em `tests/IdentityApi.Tests` (duplicado aqui de propósito:
/// são assemblies de teste separados, um por serviço; extrair um projeto de infraestrutura
/// de teste compartilhado não vale o acoplamento entre os dois para o tamanho atual do
/// desafio).
/// </summary>
public class KeycloakContainerFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("quay.io/keycloak/keycloak:26.0")
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

    public string BaseUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(8080)}";

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
