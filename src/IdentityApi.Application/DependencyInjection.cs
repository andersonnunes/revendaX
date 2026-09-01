using IdentityApi.Application.Clientes;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICriarClienteUseCase, CriarClienteUseCase>();
        return services;
    }
}
