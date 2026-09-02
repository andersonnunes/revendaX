using System.ComponentModel.DataAnnotations;

namespace IdentityApi.Application.Clientes;

/// <summary>
/// Comando de entrada do caso de uso "criar cliente" — é também o corpo esperado em
/// POST /clientes; o controller faz o bind direto nisso, sem um DTO HTTP intermediário.
/// </summary>
public class CriarClienteCommand
{
    [Required(ErrorMessage = "nome é obrigatório")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "email é obrigatório")]
    [EmailAddress(ErrorMessage = "email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "cpf é obrigatório")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "senha é obrigatória")]
    [MinLength(8, ErrorMessage = "senha deve ter ao menos 8 caracteres")]
    public string Senha { get; set; } = string.Empty;

    public string? Telefone { get; set; }
}
