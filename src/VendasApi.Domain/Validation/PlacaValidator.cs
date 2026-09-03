using System.Text.RegularExpressions;

namespace VendasApi.Domain.Validation;

/// <summary>
/// Validação de formato de placa de veículo — regra de negócio pura, sem dependência de
/// infraestrutura (mesmo padrão do <c>CpfValidator</c> do `identity-api`). Aceita os dois
/// padrões em uso no Brasil: o antigo (3 letras + 4 dígitos) e o Mercosul (3 letras + 1
/// dígito + 1 letra + 2 dígitos). Unicidade da placa é responsabilidade da Infrastructure
/// (consulta ao banco), não deste validador.
/// </summary>
public static class PlacaValidator
{
    private static readonly Regex PadraoAntigo = new("^[A-Z]{3}[0-9]{4}$", RegexOptions.Compiled);
    private static readonly Regex PadraoMercosul = new("^[A-Z]{3}[0-9][A-Z][0-9]{2}$", RegexOptions.Compiled);

    /// <summary>Maiúsculas, sem hífen/espaços — mesma normalização usada antes de validar ou persistir.</summary>
    public static string Normalizar(string? placa) =>
        string.IsNullOrWhiteSpace(placa) ? string.Empty : placa.Trim().ToUpperInvariant().Replace("-", "");

    /// <summary>Espera uma placa já normalizada (ver <see cref="Normalizar"/>).</summary>
    public static bool IsValid(string placaNormalizada) =>
        PadraoAntigo.IsMatch(placaNormalizada) || PadraoMercosul.IsMatch(placaNormalizada);
}
