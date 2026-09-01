using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace VendasApi.Tests.Integration;

/// <summary>WebApplicationFactory apontando o `vendas-api` para o Keycloak efêmero do teste.</summary>
public class VendasApiFactory(string keycloakBaseUrl) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:BaseUrl"] = keycloakBaseUrl,
            });
        });
    }
}
