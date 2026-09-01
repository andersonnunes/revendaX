using IdentityApi.Application.Clientes;
using IdentityApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApi.Controllers;

/// <summary>
/// Cadastro de clientes (US1.1). Camada fina: bind + mapeamento de exceções de domínio para
/// status HTTP. A regra de negócio está em IdentityApi.Application/Domain, não aqui.
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
        try
        {
            var resultado = await criarClienteUseCase.ExecutarAsync(command, cancellationToken);
            return Created($"/clientes/{resultado.Id}", resultado);
        }
        catch (CpfInvalidoException)
        {
            return UnprocessableEntity(new { message = "CPF inválido." });
        }
        catch (EmailJaCadastradoException)
        {
            return Conflict(new { message = "E-mail já cadastrado." });
        }
        catch (CpfJaCadastradoException)
        {
            return Conflict(new { message = "CPF já cadastrado." });
        }
        catch (ProvedorIdentidadeIndisponivelException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Serviço de identidade indisponível." });
        }
    }
}
