using IdentityApi.Application.Clientes;

namespace IdentityApi.Application.Ports;

/// <summary>
/// Porta para o provedor de identidade (implementada pela Infrastructure — Keycloak, nesta
/// entrega, ver ADR-0001). A Application não sabe nem precisa saber qual provedor é.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Cria o cliente no provedor de identidade. Lança
    /// <see cref="IdentityApi.Domain.Exceptions.EmailJaCadastradoException"/>,
    /// <see cref="IdentityApi.Domain.Exceptions.CpfJaCadastradoException"/> ou
    /// <see cref="IdentityApi.Domain.Exceptions.ProvedorIdentidadeIndisponivelException"/>
    /// conforme o caso.
    /// </summary>
    Task<ClienteResult> CriarClienteAsync(CriarClienteCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Dispara o e-mail de redefinição de senha (US1.4) — não faz nada, silenciosamente, se o
    /// e-mail não existir (uniformidade da resposta é responsabilidade de quem chama, não
    /// desta porta). Lança <see cref="IdentityApi.Domain.Exceptions.ProvedorIdentidadeIndisponivelException"/>
    /// se o provedor estiver indisponível.
    /// </summary>
    Task RecuperarSenhaAsync(string email, CancellationToken cancellationToken);
}
