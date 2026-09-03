using VendasApi.Domain.Veiculos;

namespace VendasApi.Application.Veiculos;

/// <summary>Mapeamento `Veiculo` (Domain) → `VeiculoResult` (Application), compartilhado por todos os casos de uso que retornam um veículo.</summary>
internal static class VeiculoResultExtensions
{
    public static VeiculoResult ToResult(this Veiculo veiculo) => new()
    {
        Id = veiculo.Id,
        Marca = veiculo.Marca,
        Modelo = veiculo.Modelo,
        Ano = veiculo.Ano,
        Cor = veiculo.Cor,
        Preco = veiculo.Preco,
        Placa = veiculo.Placa,
        Status = veiculo.Status.ToString(),
        CriadoEm = veiculo.CriadoEm,
    };
}
