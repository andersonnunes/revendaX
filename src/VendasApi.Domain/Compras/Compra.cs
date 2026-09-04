namespace VendasApi.Domain.Compras;

/// <summary>
/// Agregado de compra — independente de <c>Veiculo</c> (só referencia <see cref="VeiculoId"/>,
/// sem navegação de volta), com ciclo de vida e dono (<see cref="ClienteId"/>) próprios. Gera o
/// próprio `Id`/`CriadoEm`, mesmo racional já usado em `Veiculo`.
/// </summary>
public class Compra
{
    public Guid Id { get; private set; }
    public Guid VeiculoId { get; private set; }

    /// <summary>`sub` do token do Keycloak — nenhuma tabela de cliente em `vendas-api` (mesma decisão do Épico 1).</summary>
    public string ClienteId { get; private set; } = string.Empty;

    /// <summary>
    /// Snapshot de <c>Veiculo.Preco</c> no momento da compra, imutável dali em diante — um
    /// veículo `Reservado` continua editável (US2.2), então o preço do veículo pode mudar
    /// enquanto esta compra segue `Pendente`; a compra não deve refletir essa mudança.
    /// </summary>
    public decimal Preco { get; private set; }

    public StatusCompra Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private Compra()
    {
        // Construtor exigido pelo EF Core para materializar entidades via reflexão.
    }

    /// <summary>
    /// Inicia uma compra `Pendente` (US3.1). Sem validação de formato aqui — `veiculoId`
    /// (existência) e `clienteId` (vem de um token já validado, nunca do corpo da requisição)
    /// já são responsabilidade de camadas anteriores (`IniciarCompraUseCase`), não deste
    /// agregado.
    /// </summary>
    public static Compra Iniciar(Guid veiculoId, string clienteId, decimal preco) => new()
    {
        Id = Guid.NewGuid(),
        VeiculoId = veiculoId,
        ClienteId = clienteId,
        Preco = preco,
        Status = StatusCompra.Pendente,
        CriadoEm = DateTimeOffset.UtcNow,
    };
}
