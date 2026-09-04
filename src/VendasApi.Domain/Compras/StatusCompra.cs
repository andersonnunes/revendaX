namespace VendasApi.Domain.Compras;

/// <summary>
/// Ciclo de vida da compra. `Reservado` (citado no backlog junto de "pendente") é status do
/// <c>Veiculo</c>, não da <c>Compra</c> — uma compra `Pendente` sempre corresponde a um
/// veículo `Reservado` (US3.1), sem precisar duplicar o valor aqui.
/// </summary>
public enum StatusCompra
{
    Pendente,
    Concluida,
    Cancelada,
}
