namespace IdentityApi.Application.Clientes;

public interface ICriarClienteUseCase
{
    Task<ClienteResult> ExecutarAsync(CriarClienteCommand command, CancellationToken cancellationToken);
}
