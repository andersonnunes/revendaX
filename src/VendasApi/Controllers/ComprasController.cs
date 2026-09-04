using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Compras;
using VendasApi.Auth;

namespace VendasApi.Controllers;

/// <summary>
/// Início da compra (US3.1) e confirmação de pagamento (US3.3). Camada fina: só bind + delega
/// ao caso de uso. Exceções de domínio (veículo não encontrado, veículo indisponível, compra
/// não encontrada/cancelada) são traduzidas para status HTTP pelo
/// <see cref="ExceptionHandling.DomainExceptionHandler"/> global, não aqui — ver Program.cs.
/// </summary>
[ApiController]
[Route("compras")]
public class ComprasController(
    IIniciarCompraUseCase iniciarCompraUseCase,
    IConfirmarPagamentoUseCase confirmarPagamentoUseCase)
    : ControllerBase
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

    /// <summary>
    /// Webhook simulado do gateway de pagamento (US3.3) — sem `[Authorize]` de usuário, quem
    /// chama é um sistema externo. Idempotente: reentrega numa compra já `Concluida` também
    /// retorna 200, sem erro (ver <see cref="ConfirmarPagamentoUseCase"/>).
    /// </summary>
    [HttpPost("{id:guid}/confirmar-pagamento")]
    [ExigeSegredoWebhook]
    [ProducesResponseType(typeof(CompraResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmarPagamento(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await confirmarPagamentoUseCase.ExecutarAsync(id, cancellationToken);
        return Ok(resultado);
    }
}
