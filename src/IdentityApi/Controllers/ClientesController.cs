using IdentityApi.Application.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApi.Controllers;

/// <summary>
/// Cadastro de clientes (US1.1). Camada fina: só bind + delega ao caso de uso. Exceções de
/// domínio (CPF inválido, e-mail/CPF duplicado, provedor indisponível) são traduzidas para
/// status HTTP pelo <see cref="ExceptionHandling.DomainExceptionHandler"/> global, não aqui —
/// ver Program.cs (`AddExceptionHandler` + `UseExceptionHandler`).
/// </summary>
[ApiController]
[Route("clientes")]
public class ClientesController(ICriarClienteUseCase criarClienteUseCase) : ControllerBase
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
}
