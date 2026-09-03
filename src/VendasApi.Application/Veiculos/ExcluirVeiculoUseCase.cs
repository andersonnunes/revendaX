using VendasApi.Application.Ports;
using VendasApi.Domain.Exceptions;

namespace VendasApi.Application.Veiculos;

/// <summary>
/// Orquestra a exclusão (soft delete, US2.5): busca o veículo, delega o guard de estado ao
/// agregado (<see cref="Domain.Veiculos.Veiculo.Excluir"/>) e persiste — nenhum método novo
/// de repositório, reaproveita <c>ObterPorIdAsync</c>/<c>AtualizarAsync</c> da US2.2.
/// </summary>
public class ExcluirVeiculoUseCase(IVeiculoRepository veiculoRepository) : IExcluirVeiculoUseCase
{
    public async Task ExecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var veiculo = await veiculoRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new VeiculoNaoEncontradoException();

        veiculo.Excluir();

        await veiculoRepository.AtualizarAsync(veiculo, cancellationToken);
    }
}
