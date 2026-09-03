using Microsoft.AspNetCore.Diagnostics;
using VendasApi.Domain.Exceptions;

namespace VendasApi.ExceptionHandling;

/// <summary>
/// Mapeia exceções de negócio (VendasApi.Domain) para status HTTP, num único lugar — mesmo
/// padrão do `identity-api` (ver `IdentityApi/ExceptionHandling/DomainExceptionHandler.cs`,
/// que resolveu o mesmo problema de OCP: um `catch` por exceção em cada controller obrigaria
/// editar todo controller a cada exceção nova).
/// </summary>
public class DomainExceptionHandler : IExceptionHandler
{
    private static readonly Dictionary<Type, (int StatusCode, string Message)> Mapeamentos = new()
    {
        [typeof(AnoInvalidoException)] = (StatusCodes.Status422UnprocessableEntity, "Ano do veículo fora do intervalo permitido."),
        [typeof(PrecoInvalidoException)] = (StatusCodes.Status422UnprocessableEntity, "Preço deve ser maior que zero."),
        [typeof(PlacaInvalidaException)] = (StatusCodes.Status422UnprocessableEntity, "Placa em formato inválido."),
        [typeof(VeiculoJaCadastradoException)] = (StatusCodes.Status409Conflict, "Já existe um veículo cadastrado com essa placa."),
        [typeof(VeiculoVendidoException)] = (StatusCodes.Status409Conflict, "Veículo vendido não pode ser editado."),
        [typeof(VeiculoNaoEncontradoException)] = (StatusCodes.Status404NotFound, "Veículo não encontrado."),
        [typeof(VeiculoNaoPodeSerExcluidoException)] = (StatusCodes.Status409Conflict, "Só é possível excluir veículo disponível."),
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
