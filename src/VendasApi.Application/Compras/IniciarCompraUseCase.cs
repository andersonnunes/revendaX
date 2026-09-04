using VendasApi.Application.Ports;
using VendasApi.Domain.Exceptions;
using DomainCompra = VendasApi.Domain.Compras.Compra;

namespace VendasApi.Application.Compras;

/// <summary>
/// Orquestra o início da compra: busca o veículo, delega o guard de estado ao agregado
/// (<c>Veiculo.Reservar</c>), grava o preço vigente como snapshot na nova <c>Compra</c> e
/// persiste os dois agregados atomicamente via <see cref="IUnitOfWork"/> — sem isso, uma falha
/// entre as duas escritas deixaria o sistema inconsistente (compra criada sem veículo
/// reservado, ou vice-versa).
/// </summary>
public class IniciarCompraUseCase(
    IVeiculoRepository veiculoRepository,
    ICompraRepository compraRepository,
    IUnitOfWork unitOfWork) : IIniciarCompraUseCase
{
    public async Task<CompraResult> ExecutarAsync(string clienteId, IniciarCompraCommand command, CancellationToken cancellationToken)
    {
        var veiculo = await veiculoRepository.ObterPorIdAsync(command.VeiculoId, cancellationToken)
            ?? throw new VeiculoNaoEncontradoException();

        veiculo.Reservar();
        var compra = DomainCompra.Iniciar(veiculo.Id, clienteId, veiculo.Preco);

        await unitOfWork.ExecutarAtomicamenteAsync(
            async () =>
            {
                await veiculoRepository.AtualizarAsync(veiculo, cancellationToken);
                await compraRepository.AdicionarAsync(compra, cancellationToken);
            },
            cancellationToken);

        return compra.ToResult();
    }
}
