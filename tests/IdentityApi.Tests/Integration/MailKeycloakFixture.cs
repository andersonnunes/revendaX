using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// Keycloak + Mailpit na mesma rede Docker, com o Mailpit respondendo pelo alias `mailpit` —
/// o mesmo hostname configurado em `smtpServer.host` no `realm-clientes.json` (US1.4), então
/// não precisa de configuração de teste separada da de produção/dev.
/// </summary>
public class MailKeycloakFixture : IAsyncLifetime
{
    private readonly INetwork _network = new NetworkBuilder().Build();

    private IContainer? _mailpit;
    private IContainer? _keycloak;

    public string KeycloakBaseUrl => $"http://{_keycloak!.Hostname}:{_keycloak.GetMappedPublicPort(8080)}";

    public string MailpitBaseUrl => $"http://{_mailpit!.Hostname}:{_mailpit.GetMappedPublicPort(8025)}";

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();

        _mailpit = new ContainerBuilder("axllent/mailpit")
            .WithNetwork(_network)
            .WithNetworkAliases("mailpit")
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPath("/api/v1/info").ForPort(8025)))
            .Build();
        await _mailpit.StartAsync();

        _keycloak = new ContainerBuilder("quay.io/keycloak/keycloak:26.0")
            .WithNetwork(_network)
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
        await _keycloak.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_keycloak is not null)
        {
            await _keycloak.DisposeAsync();
        }

        if (_mailpit is not null)
        {
            await _mailpit.DisposeAsync();
        }

        await _network.DeleteAsync();
    }
}
