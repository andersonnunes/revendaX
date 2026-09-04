using Microsoft.Extensions.DependencyInjection;
using VendasApi.Application.Compras;
using VendasApi.Application.Veiculos;

namespace VendasApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICadastrarVeiculoUseCase, CadastrarVeiculoUseCase>();
        services.AddScoped<IEditarVeiculoUseCase, EditarVeiculoUseCase>();
        services.AddScoped<IListarVeiculosDisponiveisUseCase, ListarVeiculosDisponiveisUseCase>();
        services.AddScoped<IListarVeiculosVendidosUseCase, ListarVeiculosVendidosUseCase>();
        services.AddScoped<IExcluirVeiculoUseCase, ExcluirVeiculoUseCase>();
        services.AddScoped<IIniciarCompraUseCase, IniciarCompraUseCase>();
        return services;
    }
}
