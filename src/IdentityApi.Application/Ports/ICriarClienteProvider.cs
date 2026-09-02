using IdentityApi.Application.Clientes;

namespace IdentityApi.Application.Ports;

/// <summary>
/// Porta para criar um cliente no provedor de identidade (implementada pela Infrastructure —
/// Keycloak, nesta entrega, ver ADR-0001). A Application não sabe nem precisa saber qual
/// provedor é.
///
/// Separada de <see cref="IRecuperarSenhaProvider"/> — cadastro e recuperação de senha são
/// motivos de mudança independentes (o schema de usuário muda por um lado, o fluxo de
/// redefinição por outro), e nenhum caso de uso precisa dos dois ao mesmo tempo.
/// </summary>
public interface ICriarClienteProvider
{
    /// <summary>
    /// Cria o cliente no provedor de identidade. Lança
    /// <see cref="IdentityApi.Domain.Exceptions.EmailJaCadastradoException"/>,
    /// <see cref="IdentityApi.Domain.Exceptions.CpfJaCadastradoException"/> ou
    /// <see cref="IdentityApi.Domain.Exceptions.ProvedorIdentidadeIndisponivelException"/>
    /// conforme o caso.
    /// </summary>
    Task<ClienteResult> CriarClienteAsync(CriarClienteCommand command, CancellationToken cancellationToken);
}
