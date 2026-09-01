using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// WebApplicationFactory apontando o `identity-api` para o Keycloak efêmero do
/// <see cref="KeycloakContainerFixture"/>, com o mesmo client secret fixo de dev configurado
/// no realm exportado (ver docs/adr, fora deste repositório).
/// </summary>
public class IdentityApiFactory(string keycloakBaseUrl) : WebApplicationFactory<Program>
{
    /// <summary>Tudo que a aplicação logou durante o teste (ver <see cref="CapturingLoggerProvider"/>).</summary>
    public List<string> CapturedLogMessages { get; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:BaseUrl"] = keycloakBaseUrl,
                ["Keycloak:Realm"] = "clientes",
                ["Keycloak:ClientId"] = "identity-api",
                ["Keycloak:ClientSecret"] = "dev-identity-api-secret",
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddProvider(new CapturingLoggerProvider(CapturedLogMessages));
        });
    }
}
