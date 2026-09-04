namespace VendasApi.Application.Veiculos;

public interface IExcluirVeiculoUseCase
{
    Task ExecutarAsync(Guid id, CancellationToken cancellationToken);
}
