using VendasApi.Domain.Compras;

namespace VendasApi.Application.Ports;

/// <summary>Porta de persistência do agregado <see cref="Compra"/>.</summary>
public interface ICompraRepository
{
    Task<Compra?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(Compra compra, CancellationToken cancellationToken);

    Task AtualizarAsync(Compra compra, CancellationToken cancellationToken);
}
