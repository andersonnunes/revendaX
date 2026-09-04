using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Compras;
using VendasApi.Auth;

namespace VendasApi.Controllers;

/// <summary>
/// Início da compra (US3.1), confirmação de pagamento (US3.3) e consulta de status (US3.4).
/// Camada fina: só bind + delega ao caso de uso. Exceções de domínio (veículo não encontrado,
/// veículo indisponível, compra não encontrada/cancelada) são traduzidas para status HTTP pelo
/// <see cref="ExceptionHandling.DomainExceptionHandler"/> global, não aqui — ver Program.cs.
/// </summary>
[ApiController]
[Route("compras")]
public class ComprasController(
    IIniciarCompraUseCase iniciarCompraUseCase,
    IConfirmarPagamentoUseCase confirmarPagamentoUseCase,
    IConsultarCompraUseCase consultarCompraUseCase,
    IListarComprasDoClienteUseCase listarComprasDoClienteUseCase)
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

    /// <summary>
    /// Lista todas as compras do cliente autenticado (US3.4, extensão) — o `clienteId` vem só
    /// da claim `sub` do token, nunca de parâmetro de rota/query; um cliente não tem como pedir
    /// a lista de outro. Sem paginação (mesma decisão já aceita nas listagens de veículo,
    /// US2.3/US2.4) — nunca 404, lista vazia se o cliente não tiver nenhuma compra.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "cliente")]
    [ProducesResponseType(typeof(IReadOnlyList<CompraResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var clienteId = User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Token autenticado sem claim 'sub'.");

        var resultado = await listarComprasDoClienteUseCase.ExecutarAsync(clienteId, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Consulta de status pelo dono da compra (US3.4) — compra inexistente ou de outro
    /// cliente retornam 404 igualmente, nunca 403 (ver <see cref="ConsultarCompraUseCase"/>).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "cliente")]
    [ProducesResponseType(typeof(CompraResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken cancellationToken)
    {
        var clienteId = User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Token autenticado sem claim 'sub'.");

        var resultado = await consultarCompraUseCase.ExecutarAsync(id, clienteId, cancellationToken);
        return Ok(resultado);
    }
}
