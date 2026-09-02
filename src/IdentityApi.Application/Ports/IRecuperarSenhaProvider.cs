namespace IdentityApi.Application.Ports;

/// <summary>
/// Porta para disparar a recuperação de senha no provedor de identidade (implementada pela
/// Infrastructure — Keycloak, nesta entrega, ver ADR-0001).
///
/// Separada de <see cref="ICriarClienteProvider"/> — ver o porquê no doc-comment dela.
/// </summary>
public interface IRecuperarSenhaProvider
{
    /// <summary>
    /// Dispara o e-mail de redefinição de senha (US1.4) — não faz nada, silenciosamente, se o
    /// e-mail não existir (uniformidade da resposta é responsabilidade de quem chama, não
    /// desta porta). Lança <see cref="IdentityApi.Domain.Exceptions.ProvedorIdentidadeIndisponivelException"/>
    /// se o provedor estiver indisponível.
    /// </summary>
    Task RecuperarSenhaAsync(string email, CancellationToken cancellationToken);
}
