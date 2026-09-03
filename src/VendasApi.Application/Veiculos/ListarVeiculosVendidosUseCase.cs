using VendasApi.Application.Ports;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Veiculos;

/// <summary>
/// Espelha o <see cref="ListarVeiculosDisponiveisUseCase"/> (US2.3), só troca o status — a
/// query já foi generalizada lá exatamente para este reaproveitamento (ver refinamento da
/// US2.3). Autorização (`vendedor`) é responsabilidade do controller, não deste caso de uso.
/// </summary>
public class ListarVeiculosVendidosUseCase(IVeiculoRepository veiculoRepository) : IListarVeiculosVendidosUseCase
{
    public async Task<IReadOnlyList<VeiculoResult>> ExecutarAsync(CancellationToken cancellationToken)
    {
        var veiculos = await veiculoRepository.ListarPorStatusOrdenadosPorPrecoAsync(StatusVeiculo.Vendido, cancellationToken);
        return veiculos.Select(v => v.ToResult()).ToList();
    }
}
