using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Ports;

/// <summary>
/// Porta de persistência do agregado <see cref="Veiculo"/> (implementada pela Infrastructure
/// — EF Core/Postgres, ver `docs/architecture.md`). Uma porta por agregado, não por caso de
/// uso — é o uso pretendido do padrão Repository, diferente do caso do `identity-api`
/// (`IIdentityProvider` foi dividido porque agrupava dois conceitos de negócio diferentes,
/// não porque repositório-por-agregado esteja errado).
/// </summary>
public interface IVeiculoRepository
{
    Task<Veiculo?> ObterPorPlacaAsync(string placa, CancellationToken cancellationToken);

    Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken);

    Task AtualizarAsync(Veiculo veiculo, CancellationToken cancellationToken);

    /// <summary>
    /// Filtra por status e ordena por preço ascendente (desempate por `CriadoEm` ascendente)
    /// — parametrizado por status, não um método fixo tipo `ListarDisponiveisAsync`, porque a
    /// US2.4 precisa exatamente da mesma operação para `Vendido`.
    /// </summary>
    Task<IReadOnlyList<Veiculo>> ListarPorStatusOrdenadosPorPrecoAsync(StatusVeiculo status, CancellationToken cancellationToken);
}
