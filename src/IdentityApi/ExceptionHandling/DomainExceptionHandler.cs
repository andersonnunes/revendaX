using IdentityApi.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace IdentityApi.ExceptionHandling;

/// <summary>
/// Mapeia exceções de negócio (IdentityApi.Domain) para status HTTP, num único lugar — os
/// controllers ficam livres de try/catch. Adicionar uma exceção de negócio nova é adicionar
/// uma entrada aqui, não editar cada controller que pode lançá-la (fecha o gap de OCP que
/// existia quando esse mapeamento estava espalhado em try/catch por controller).
/// </summary>
public class DomainExceptionHandler : IExceptionHandler
{
    private static readonly Dictionary<Type, (int StatusCode, string Message)> Mapeamentos = new()
    {
        [typeof(CpfInvalidoException)] = (StatusCodes.Status422UnprocessableEntity, "CPF inválido."),
        [typeof(EmailJaCadastradoException)] = (StatusCodes.Status409Conflict, "E-mail já cadastrado."),
        [typeof(CpfJaCadastradoException)] = (StatusCodes.Status409Conflict, "CPF já cadastrado."),
        [typeof(ProvedorIdentidadeIndisponivelException)] =
            (StatusCodes.Status503ServiceUnavailable, "Serviço de identidade indisponível."),
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!Mapeamentos.TryGetValue(exception.GetType(), out var mapeamento))
        {
            return false; // não é uma exceção de negócio conhecida — deixa o handler padrão (500) tratar
        }

        httpContext.Response.StatusCode = mapeamento.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new { message = mapeamento.Message }, cancellationToken);
        return true;
    }
}
