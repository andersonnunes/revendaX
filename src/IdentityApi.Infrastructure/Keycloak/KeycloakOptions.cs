namespace IdentityApi.Infrastructure.Keycloak;

/// <summary>
/// Configuração de acesso à Admin REST API do Keycloak. Valores injetados via env vars no
/// docker-compose (ver infra/docker-compose.yml) — nunca com valor real em appsettings.json.
/// </summary>
public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = "clientes";
    public string ClientId { get; set; } = "identity-api";
    public string ClientSecret { get; set; } = string.Empty;
}
