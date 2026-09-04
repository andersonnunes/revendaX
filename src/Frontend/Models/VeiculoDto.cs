namespace Frontend.Models;

/// <summary>
/// Formato próprio do Frontend, não `VendasApi.Application.VeiculoResult` via
/// `ProjectReference` — decisão da US4.3: `VeiculoResult` é só forma de dados (sem lógica que
/// valha a pena não duplicar, diferente do `CpfValidator`/`PlacaValidator`), e referenciar o
/// assembly inteiro do `VendasApi.Application` acoplaria o frontend a tipos internos do
/// backend sem necessidade. Promovido de `Pages/Veiculos.razor` pra cá na US4.5, quando o
/// painel do vendedor passou a precisar do mesmo formato (mesmo racional do `CompraDto`,
/// promovido na US4.4 pelo mesmo motivo: dois consumidores, não mais um só).
/// </summary>
public sealed class VeiculoDto
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
