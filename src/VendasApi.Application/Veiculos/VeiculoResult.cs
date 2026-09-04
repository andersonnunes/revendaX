namespace VendasApi.Application.Veiculos;

/// <summary>Resultado do caso de uso "cadastrar veículo".</summary>
public class VeiculoResult
{
    public Guid Id { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Ano { get; set; }
    public string Cor { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
}
