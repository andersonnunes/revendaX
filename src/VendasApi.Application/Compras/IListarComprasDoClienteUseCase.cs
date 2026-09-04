namespace VendasApi.Application.Compras;

public interface IListarComprasDoClienteUseCase
{
    Task<IReadOnlyList<CompraResult>> ExecutarAsync(string clienteId, CancellationToken cancellationToken);
}
