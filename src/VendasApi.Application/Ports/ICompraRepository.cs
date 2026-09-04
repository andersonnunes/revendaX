using VendasApi.Domain.Compras;

namespace VendasApi.Application.Ports;

/// <summary>
/// Porta de persistência do agregado <see cref="Compra"/> — só <see cref="AdicionarAsync"/>
/// por enquanto, tudo que a US3.1 precisa. `ObterPorIdAsync` entra numa história futura, quando
/// a compra precisar ser consultada por id.
/// </summary>
public interface ICompraRepository
{
    Task AdicionarAsync(Compra compra, CancellationToken cancellationToken);
}
