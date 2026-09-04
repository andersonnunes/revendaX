using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Application.Ports;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // (serviceProvider, options) em vez de capturar IConfiguration direto — resolve via DI
        // no momento em que o DbContext é efetivamente construído, então enxerga overrides de
        // configuração tardios (ex.: WebApplicationFactory nos testes), não um snapshot de
        // quando este método rodou. Mesmo problema já resolvido no `vendas-api` para o
        // JwtBearer (ver Program.cs) — aplicado aqui de propósito, antes de o bug acontecer.
        services.AddDbContext<VendasDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            options.UseNpgsql(configuration.GetConnectionString("VendasDb"));
        });

        services.AddScoped<IVeiculoRepository, EfVeiculoRepository>();

        return services;
    }
}
