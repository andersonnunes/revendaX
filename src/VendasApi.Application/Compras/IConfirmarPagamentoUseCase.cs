namespace VendasApi.Application.Compras;

public interface IConfirmarPagamentoUseCase
{
    Task<CompraResult> ExecutarAsync(Guid compraId, CancellationToken cancellationToken);
}
