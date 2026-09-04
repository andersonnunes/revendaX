using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace VendasApi.Tests.Integration;

/// <summary>WebApplicationFactory apontando o `vendas-api` para o Keycloak e o Postgres efêmeros do teste.</summary>
public class VendasApiFactory(string keycloakBaseUrl, string postgresConnectionString) : WebApplicationFactory<Program>
{
    /// <summary>Segredo fixo do webhook de pagamento (US3.3) neste ambiente de teste — ver <see cref="ConfigureWebHost"/>.</summary>
    public const string WebhookSecret = "test-webhook-secret";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:BaseUrl"] = keycloakBaseUrl,
                ["ConnectionStrings:VendasDb"] = postgresConnectionString,
                ["Pagamentos:WebhookSecret"] = WebhookSecret,
            });
        });
    }
}
