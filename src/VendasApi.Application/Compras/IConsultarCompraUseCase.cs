namespace VendasApi.Application.Compras;

public interface IConsultarCompraUseCase
{
    Task<CompraResult> ExecutarAsync(Guid compraId, string clienteId, CancellationToken cancellationToken);
}
