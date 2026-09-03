using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Ports;

/// <summary>
/// Porta de persistência do agregado <see cref="Veiculo"/> (implementada pela Infrastructure
/// — EF Core/Postgres, ver `docs/architecture.md`). Uma porta por agregado, não por caso de
/// uso — é o uso pretendido do padrão Repository, diferente do caso do `identity-api`
/// (`IIdentityProvider` foi dividido porque agrupava dois conceitos de negócio diferentes,
/// não porque repositório-por-agregado esteja errado; ver US2.2/refinamentos).
/// </summary>
public interface IVeiculoRepository
{
    Task<Veiculo?> ObterPorPlacaAsync(string placa, CancellationToken cancellationToken);

    Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken);
}
