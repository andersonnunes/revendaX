using VendasApi.Domain.Compras;

namespace VendasApi.Application.Compras;

/// <summary>Mapeamento `Compra` (Domain) → `CompraResult` (Application) — mesmo padrão de `VeiculoResultExtensions`.</summary>
internal static class CompraResultExtensions
{
    public static CompraResult ToResult(this Compra compra) => new()
    {
        Id = compra.Id,
        VeiculoId = compra.VeiculoId,
        ClienteId = compra.ClienteId,
        Preco = compra.Preco,
        Status = compra.Status.ToString(),
        CriadoEm = compra.CriadoEm,
    };
}
