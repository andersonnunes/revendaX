namespace VendasApi.Application.Compras;

/// <summary>Resultado do caso de uso "iniciar compra".</summary>
public class CompraResult
{
    public Guid Id { get; set; }
    public Guid VeiculoId { get; set; }
    public string ClienteId { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
}
