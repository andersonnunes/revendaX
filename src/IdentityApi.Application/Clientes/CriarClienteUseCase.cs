using IdentityApi.Application.Ports;
using IdentityApi.Domain.Exceptions;
using IdentityApi.Domain.Validation;

namespace IdentityApi.Application.Clientes;

/// <summary>
/// Orquestra o cadastro: valida a regra de negócio do CPF (dígito verificador) antes de
/// delegar ao provedor de identidade — a checagem de duplicidade (e-mail/CPF já cadastrados)
/// é responsabilidade da implementação de <see cref="IIdentityProvider"/>, não deste caso de
/// uso.
/// </summary>
public class CriarClienteUseCase(ICriarClienteProvider clienteProvider) : ICriarClienteUseCase
{
    public Task<ClienteResult> ExecutarAsync(CriarClienteCommand command, CancellationToken cancellationToken)
    {
        if (!CpfValidator.IsValid(command.Cpf))
        {
            throw new CpfInvalidoException();
        }

        return clienteProvider.CriarClienteAsync(command, cancellationToken);
    }
}
