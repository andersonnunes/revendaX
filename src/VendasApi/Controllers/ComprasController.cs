using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Compras;

namespace VendasApi.Controllers;

/// <summary>
/// Início da compra (US3.1). Camada fina: só bind + delega ao caso de uso. Exceções de domínio
/// (veículo não encontrado, veículo indisponível) são traduzidas para status HTTP pelo
/// <see cref="ExceptionHandling.DomainExceptionHandler"/> global, não aqui — ver Program.cs.
/// </summary>
[ApiController]
[Route("compras")]
public class ComprasController(IIniciarCompraUseCase iniciarCompraUseCase) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "cliente")]
    [ProducesResponseType(typeof(CompraResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Iniciar(IniciarCompraCommand command, CancellationToken cancellationToken)
    {
        // `[Authorize(Roles = "cliente")]` já garante um token válido com claim `sub` — o
        // fallback abaixo é defensivo, não um caminho alcançável em uso normal.
        var clienteId = User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Token autenticado sem claim 'sub'.");

        var resultado = await iniciarCompraUseCase.ExecutarAsync(clienteId, command, cancellationToken);
        return Created($"/compras/{resultado.Id}", resultado);
    }
}
