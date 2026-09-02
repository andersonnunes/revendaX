namespace IdentityApi.Application.Clientes;

public interface IRecuperarSenhaUseCase
{
    Task ExecutarAsync(RecuperarSenhaCommand command, CancellationToken cancellationToken);
}
