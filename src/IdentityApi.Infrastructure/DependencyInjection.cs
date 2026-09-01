using IdentityApi.Application.Ports;
using IdentityApi.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IdentityApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

        services.AddHttpClient<IKeycloakTokenProvider, KeycloakTokenProvider>((serviceProvider, client) =>
        {
            var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
            client.BaseAddress = new Uri(keycloakOptions.BaseUrl);
        });

        services.AddHttpClient<IIdentityProvider, KeycloakIdentityProvider>((serviceProvider, client) =>
        {
            var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
            client.BaseAddress = new Uri(keycloakOptions.BaseUrl);
        });

        return services;
    }
}
