namespace IdentityApi.Application.Clientes;

/// <summary>
/// Resultado do caso de uso "criar cliente". `Id` é o user id do provedor de identidade
/// (mesmo valor do claim `sub` do token emitido no login, US1.2) — nunca inclui senha/cpf.
/// </summary>
public class ClienteResult
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
}
