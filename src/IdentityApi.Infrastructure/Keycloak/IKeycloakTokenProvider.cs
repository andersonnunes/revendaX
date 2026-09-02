namespace IdentityApi.Infrastructure.Keycloak;

/// <summary>
/// Autenticação do client de serviço `identity-api` no Keycloak — responsabilidade separada
/// de <see cref="KeycloakIdentityProvider"/> (que faz CRUD de usuário) porque são motivos de
/// mudança diferentes: o endpoint/protocolo de token muda por um lado, o schema de usuário
/// muda por outro. Também reaproveitável por qualquer futura operação administrativa no
/// Keycloak além de criar cliente (ex.: US1.5).
/// </summary>
public interface IKeycloakTokenProvider
{
    Task<string> GetServiceAccountTokenAsync(CancellationToken cancellationToken);
}
