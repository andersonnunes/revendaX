namespace VendasApi.Application.Compras;

public interface IIniciarCompraUseCase
{
    Task<CompraResult> ExecutarAsync(string clienteId, IniciarCompraCommand command, CancellationToken cancellationToken);
}
