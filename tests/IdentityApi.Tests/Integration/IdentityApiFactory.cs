using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// WebApplicationFactory apontando o `identity-api` para o Keycloak efêmero do
/// <see cref="KeycloakContainerFixture"/>, com o mesmo client secret fixo de dev configurado
/// no realm exportado (ver docs/adr, fora deste repositório).
/// </summary>
public class IdentityApiFactory(string keycloakBaseUrl) : WebApplicationFactory<Program>
{
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
    }
}
