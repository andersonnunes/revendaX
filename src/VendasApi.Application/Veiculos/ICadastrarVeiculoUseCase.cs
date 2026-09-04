namespace VendasApi.Application.Veiculos;

public interface ICadastrarVeiculoUseCase
{
    Task<VeiculoResult> ExecutarAsync(CadastrarVeiculoCommand command, CancellationToken cancellationToken);
}
