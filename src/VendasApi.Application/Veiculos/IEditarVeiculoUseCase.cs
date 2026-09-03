namespace VendasApi.Application.Veiculos;

public interface IEditarVeiculoUseCase
{
    Task<VeiculoResult> ExecutarAsync(Guid id, EditarVeiculoCommand command, CancellationToken cancellationToken);
}
