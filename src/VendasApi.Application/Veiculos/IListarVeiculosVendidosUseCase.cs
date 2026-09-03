namespace VendasApi.Application.Veiculos;

public interface IListarVeiculosVendidosUseCase
{
    Task<IReadOnlyList<VeiculoResult>> ExecutarAsync(CancellationToken cancellationToken);
}
