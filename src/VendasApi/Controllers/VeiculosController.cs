using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Veiculos;

namespace VendasApi.Controllers;

/// <summary>
/// Cadastro (US2.1), edição (US2.2) e listagem de veículos à venda (US2.3). Camada fina: só
/// bind + delega ao caso de uso. Exceções de domínio (ano/preço/placa inválidos, placa
/// duplicada, veículo vendido, veículo não encontrado) são traduzidas para status HTTP pelo
/// <see cref="ExceptionHandling.DomainExceptionHandler"/> global, não aqui — ver Program.cs.
/// </summary>
[ApiController]
[Route("veiculos")]
public class VeiculosController(
    ICadastrarVeiculoUseCase cadastrarVeiculoUseCase,
    IEditarVeiculoUseCase editarVeiculoUseCase,
    IListarVeiculosDisponiveisUseCase listarVeiculosDisponiveisUseCase)
    : ControllerBase
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

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "vendedor")]
    [ProducesResponseType(typeof(VeiculoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Editar(Guid id, EditarVeiculoCommand command, CancellationToken cancellationToken)
    {
        var resultado = await editarVeiculoUseCase.ExecutarAsync(id, command, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>Público, sem `[Authorize]` — visitante/cliente navegam o catálogo sem login (US2.3).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VeiculoResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarDisponiveis(CancellationToken cancellationToken)
    {
        var resultado = await listarVeiculosDisponiveisUseCase.ExecutarAsync(cancellationToken);
        return Ok(resultado);
    }
}
