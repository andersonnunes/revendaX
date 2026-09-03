namespace VendasApi.Application.Veiculos;

public interface IListarVeiculosDisponiveisUseCase
{
    Task<IReadOnlyList<VeiculoResult>> ExecutarAsync(CancellationToken cancellationToken);
}
