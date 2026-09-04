using System.ComponentModel.DataAnnotations;

namespace VendasApi.Application.Compras;

/// <summary>
/// Comando de entrada do caso de uso "iniciar compra" — corpo de POST /compras. Sem
/// `ClienteId`: vem do claim `sub` do token autenticado (`ComprasController`), nunca do corpo
/// da requisição. `[Required]` sozinho não pega `Guid.Empty` (não é
/// `null`), por isso `IValidatableObject` — mesmo efeito de 400 automático do `[ApiController]`,
/// cobrindo também o caso de campo ausente/vazio, não só malformado.
/// </summary>
public class IniciarCompraCommand : IValidatableObject
{
    public Guid VeiculoId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VeiculoId == Guid.Empty)
        {
            yield return new ValidationResult("veiculoId é obrigatório", [nameof(VeiculoId)]);
        }
    }
}
