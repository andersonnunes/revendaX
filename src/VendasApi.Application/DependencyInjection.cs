using Microsoft.Extensions.DependencyInjection;
using VendasApi.Application.Veiculos;

namespace VendasApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICadastrarVeiculoUseCase, CadastrarVeiculoUseCase>();
        services.AddScoped<IEditarVeiculoUseCase, EditarVeiculoUseCase>();
        services.AddScoped<IListarVeiculosDisponiveisUseCase, ListarVeiculosDisponiveisUseCase>();
        return services;
    }
}
