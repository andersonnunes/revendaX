using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VendasApi.Controllers;

/// <summary>
/// Endpoint de diagnóstico só para validar a autenticação/autorização (US1.3) antes de
/// existir regra de negócio real (Épico 2/3) — descartável quando os endpoints reais já
/// forem os protegidos de fato.
/// </summary>
[ApiController]
[Route("whoami")]
[Authorize]
public class WhoAmIController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            sub = User.FindFirst("sub")?.Value,
            email = User.FindFirst("email")?.Value,
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
        });
    }

    /// <summary>Variante só para exercitar autorização por role — 403 se o token não tiver `cliente`.</summary>
    [HttpGet("cliente")]
    [Authorize(Roles = "cliente")]
    public IActionResult GetSoParaCliente() => Get();
}
