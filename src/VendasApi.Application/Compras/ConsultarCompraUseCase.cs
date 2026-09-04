using VendasApi.Application.Ports;
using VendasApi.Domain.Exceptions;

namespace VendasApi.Application.Compras;

/// <summary>
/// Busca a compra por id e confirma que pertence ao cliente autenticado — compra inexistente
/// ou de outro cliente lançam a mesma <see cref="CompraNaoEncontradaException"/> (404), nunca
/// 403: não confirma pra um cliente que um id alheio existe (ver "Decisões" da US3.4).
/// </summary>
public class ConsultarCompraUseCase(ICompraRepository compraRepository) : IConsultarCompraUseCase
{
    public async Task<CompraResult> ExecutarAsync(Guid compraId, string clienteId, CancellationToken cancellationToken)
    {
        var compra = await compraRepository.ObterPorIdAsync(compraId, cancellationToken)
            ?? throw new CompraNaoEncontradaException();

        if (compra.ClienteId != clienteId)
        {
            throw new CompraNaoEncontradaException();
        }

        return compra.ToResult();
    }
}
