using IdentityApi.Application.Ports;

namespace IdentityApi.Application.Clientes;

/// <summary>
/// Sem regra de negócio própria além de delegar — a uniformidade da resposta (202 igual,
/// exista ou não o e-mail) é decisão do controller, não deste caso de uso: aqui não há nada
/// pra "ramificar", só repassar.
/// </summary>
public class RecuperarSenhaUseCase(IIdentityProvider identityProvider) : IRecuperarSenhaUseCase
{
    public Task ExecutarAsync(RecuperarSenhaCommand command, CancellationToken cancellationToken) =>
        identityProvider.RecuperarSenhaAsync(command.Email, cancellationToken);
}
