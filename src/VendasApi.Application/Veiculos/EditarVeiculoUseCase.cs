using VendasApi.Application.Ports;
using VendasApi.Domain.Exceptions;

namespace VendasApi.Application.Veiculos;

/// <summary>
/// Orquestra a edição: busca o veículo, delega a regra de negócio (guard de estado +
/// validação de ano/preço) ao agregado (<see cref="Domain.Veiculos.Veiculo.AtualizarDados"/>)
/// e persiste — mesma ordem já usada no cadastro (US2.1).
/// </summary>
public class EditarVeiculoUseCase(IVeiculoRepository veiculoRepository) : IEditarVeiculoUseCase
{
    public async Task<VeiculoResult> ExecutarAsync(Guid id, EditarVeiculoCommand command, CancellationToken cancellationToken)
    {
        var veiculo = await veiculoRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new VeiculoNaoEncontradoException();

        veiculo.AtualizarDados(command.Marca, command.Modelo, command.Ano, command.Cor, command.Preco);

        await veiculoRepository.AtualizarAsync(veiculo, cancellationToken);

        return veiculo.ToResult();
    }
}
