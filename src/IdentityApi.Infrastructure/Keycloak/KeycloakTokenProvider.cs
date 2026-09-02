using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IdentityApi.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace IdentityApi.Infrastructure.Keycloak;

/// <inheritdoc cref="IKeycloakTokenProvider"/>
public class KeycloakTokenProvider(HttpClient httpClient, IOptions<KeycloakOptions> options)
    : IKeycloakTokenProvider
{
    private readonly KeycloakOptions _options = options.Value;

    public async Task<string> GetServiceAccountTokenAsync(CancellationToken cancellationToken)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
        });

        var response = await httpClient.PostAsync(
            $"realms/{_options.Realm}/protocol/openid-connect/token", form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ProvedorIdentidadeIndisponivelException(
                $"Falha ao autenticar client de serviço no Keycloak (HTTP {(int)response.StatusCode}).");
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        return token?.AccessToken
            ?? throw new ProvedorIdentidadeIndisponivelException("Resposta de token do Keycloak sem access_token.");
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
