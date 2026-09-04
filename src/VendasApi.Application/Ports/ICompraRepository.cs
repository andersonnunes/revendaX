using VendasApi.Domain.Compras;

namespace VendasApi.Application.Ports;

/// <summary>Porta de persistência do agregado <see cref="Compra"/>.</summary>
public interface ICompraRepository
{
    Task<Compra?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(Compra compra, CancellationToken cancellationToken);

    Task AtualizarAsync(Compra compra, CancellationToken cancellationToken);

    /// <summary>Compras `Pendente` criadas antes de <paramref name="limite"/> — candidatas a expirar (US3.5).</summary>
    Task<IReadOnlyList<Compra>> ListarPendentesExpiradasAsync(DateTimeOffset limite, CancellationToken cancellationToken);

    /// <summary>Todas as compras do cliente (qualquer status), mais recente primeiro — US3.4 (extensão).</summary>
    Task<IReadOnlyList<Compra>> ListarPorClienteAsync(string clienteId, CancellationToken cancellationToken);
}
