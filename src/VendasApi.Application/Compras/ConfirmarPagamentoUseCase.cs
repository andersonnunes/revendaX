using VendasApi.Application.Ports;
using VendasApi.Domain.Compras;
using VendasApi.Domain.Exceptions;

namespace VendasApi.Application.Compras;

/// <summary>
/// Orquestra a confirmação de pagamento: busca a compra, corta idempotência se já
/// <see cref="StatusCompra.Concluida"/> (reentrega de webhook — sucesso sem repetir a escrita),
/// senão delega os guards de estado aos agregados (<c>Compra.ConfirmarPagamento</c>,
/// <c>Veiculo.MarcarComoVendido</c>) e persiste os dois atomicamente via
/// <see cref="IUnitOfWork"/> — mesmo argumento da US3.1: uma falha entre as duas escritas
/// deixaria o sistema com uma compra concluída e veículo ainda reservado, ou vice-versa.
/// </summary>
public class ConfirmarPagamentoUseCase(
    ICompraRepository compraRepository,
    IVeiculoRepository veiculoRepository,
    IUnitOfWork unitOfWork) : IConfirmarPagamentoUseCase
{
    public async Task<CompraResult> ExecutarAsync(Guid compraId, CancellationToken cancellationToken)
    {
        var compra = await compraRepository.ObterPorIdAsync(compraId, cancellationToken)
            ?? throw new CompraNaoEncontradaException();

        if (compra.Status == StatusCompra.Concluida)
        {
            return compra.ToResult();
        }

        var veiculo = await veiculoRepository.ObterPorIdAsync(compra.VeiculoId, cancellationToken)
            ?? throw new VeiculoNaoEncontradoException();

        compra.ConfirmarPagamento();
        veiculo.MarcarComoVendido();

        await unitOfWork.ExecutarAtomicamenteAsync(
            async () =>
            {
                await compraRepository.AtualizarAsync(compra, cancellationToken);
                await veiculoRepository.AtualizarAsync(veiculo, cancellationToken);
            },
            cancellationToken);

        return compra.ToResult();
    }
}
