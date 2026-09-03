using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Veiculos;

namespace VendasApi.Controllers;

/// <summary>
/// Cadastro de veículos (US2.1). Camada fina: só bind + delega ao caso de uso. Exceções de
/// domínio (ano/preço/placa inválidos, placa duplicada) são traduzidas para status HTTP pelo
/// <see cref="ExceptionHandling.DomainExceptionHandler"/> global, não aqui — ver Program.cs.
/// </summary>
[ApiController]
[Route("veiculos")]
public class VeiculosController(ICadastrarVeiculoUseCase cadastrarVeiculoUseCase) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "vendedor")]
    [ProducesResponseType(typeof(VeiculoResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cadastrar(CadastrarVeiculoCommand command, CancellationToken cancellationToken)
    {
        var resultado = await cadastrarVeiculoUseCase.ExecutarAsync(command, cancellationToken);
        return Created($"/veiculos/{resultado.Id}", resultado);
    }
}
