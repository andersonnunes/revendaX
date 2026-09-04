using VendasApi.Application.Ports;
using VendasApi.Domain.Exceptions;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Veiculos;

/// <summary>
/// Orquestra o cadastro: delega a validação de ano/preço/placa ao agregado
/// (<see cref="Veiculo.Cadastrar"/>) e checa unicidade de placa antes de persistir — mesma
/// ordem já usada no `identity-api` (regra de negócio pura primeiro, checagem que depende de
/// infraestrutura depois).
/// </summary>
public class CadastrarVeiculoUseCase(IVeiculoRepository veiculoRepository) : ICadastrarVeiculoUseCase
{
    public async Task<VeiculoResult> ExecutarAsync(CadastrarVeiculoCommand command, CancellationToken cancellationToken)
    {
        var veiculo = Veiculo.Cadastrar(command.Marca, command.Modelo, command.Ano, command.Cor, command.Preco, command.Placa);

        if (await veiculoRepository.ObterPorPlacaAsync(veiculo.Placa, cancellationToken) is not null)
        {
            throw new VeiculoJaCadastradoException();
        }

        await veiculoRepository.AdicionarAsync(veiculo, cancellationToken);

        return veiculo.ToResult();
    }
}
