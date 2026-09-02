using IdentityApi.Application.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApi.Controllers;

/// <summary>
/// Cadastro (US1.1) e recuperação de senha (US1.4) de clientes. Camada fina: só bind + delega
/// ao caso de uso. Exceções de domínio (CPF inválido, e-mail/CPF duplicado, provedor
/// indisponível) são traduzidas para status HTTP pelo
/// <see cref="ExceptionHandling.DomainExceptionHandler"/> global, não aqui — ver Program.cs
/// (`AddExceptionHandler` + `UseExceptionHandler`).
/// </summary>
[ApiController]
[Route("clientes")]
public class ClientesController(ICriarClienteUseCase criarClienteUseCase, IRecuperarSenhaUseCase recuperarSenhaUseCase)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ClienteResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Criar(CriarClienteCommand command, CancellationToken cancellationToken)
    {
        var resultado = await criarClienteUseCase.ExecutarAsync(command, cancellationToken);
        return Created($"/clientes/{resultado.Id}", resultado);
    }

    /// <summary>
    /// Sempre 202, exista ou não o e-mail — mitiga enumeração de contas (US1.4, critério 3).
    /// Não é o caso de uso quem decide isso: mesmo se ele lançar (indisponibilidade real do
    /// Keycloak), esse cenário vira 503 pelo <see cref="ExceptionHandling.DomainExceptionHandler"/>,
    /// que também não distingue "e-mail existe" — é uma falha de infraestrutura, não uma
    /// resposta condicional ao dado.
    /// </summary>
    [HttpPost("recuperar-senha")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RecuperarSenha(RecuperarSenhaCommand command, CancellationToken cancellationToken)
    {
        await recuperarSenhaUseCase.ExecutarAsync(command, cancellationToken);
        return Accepted(new { message = "Se o e-mail existir, enviaremos instruções de redefinição." });
    }
}
