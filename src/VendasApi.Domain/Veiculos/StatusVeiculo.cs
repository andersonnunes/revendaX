namespace VendasApi.Domain.Veiculos;

/// <summary>Ciclo de vida comercial do veículo — não confundir com <c>Ativo</c> (US2.5), que é
/// sobre visibilidade do registro, não sobre este ciclo.</summary>
public enum StatusVeiculo
{
    Disponivel,
    Reservado,
    Vendido,
}
