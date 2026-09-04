namespace Frontend.Models;

/// <summary>
/// Formato próprio do Frontend pra desserializar a resposta de `POST /compras`/
/// `GET /compras/{id}` (US3.1/US3.4) — mesma decisão da US4.3 (<c>VeiculoDto</c>): é só forma
/// de dados, sem lógica que valha a pena compartilhar via <c>ProjectReference</c> com
/// `VendasApi.Application` (que traria tipos internos do backend sem necessidade nenhuma pro
/// frontend). Compartilhado entre <c>CompraStatus.razor</c> e <c>MinhasCompras.razor</c> — as
/// duas páginas precisam do mesmo formato, então este não fica inline em nenhuma das duas.
/// </summary>
public sealed class CompraDto
{
    public Guid Id { get; set; }
    public Guid VeiculoId { get; set; }
    public string ClienteId { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
}
