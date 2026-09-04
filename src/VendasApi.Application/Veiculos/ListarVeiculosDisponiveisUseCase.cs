using VendasApi.Application.Ports;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Veiculos;

/// <summary>
/// Sem regra de negócio própria além de delegar — filtro e ordenação são resolvidos na query
/// (Infrastructure), não aqui (ver refinamento da US2.3: não é uma invariante do agregado,
/// é uma projeção de leitura). Mesmo tão fino quanto o `RecuperarSenhaUseCase` do Épico 1.
/// </summary>
public class ListarVeiculosDisponiveisUseCase(IVeiculoRepository veiculoRepository) : IListarVeiculosDisponiveisUseCase
{
    public async Task<IReadOnlyList<VeiculoResult>> ExecutarAsync(CancellationToken cancellationToken)
    {
        var veiculos = await veiculoRepository.ListarPorStatusOrdenadosPorPrecoAsync(StatusVeiculo.Disponivel, cancellationToken);
        return veiculos.Select(v => v.ToResult()).ToList();
    }
}
