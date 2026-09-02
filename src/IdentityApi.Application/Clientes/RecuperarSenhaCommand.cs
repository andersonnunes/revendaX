using System.ComponentModel.DataAnnotations;

namespace IdentityApi.Application.Clientes;

/// <summary>Comando de entrada de POST /clientes/recuperar-senha.</summary>
public class RecuperarSenhaCommand
{
    [Required(ErrorMessage = "email é obrigatório")]
    [EmailAddress(ErrorMessage = "email inválido")]
    public string Email { get; set; } = string.Empty;
}
