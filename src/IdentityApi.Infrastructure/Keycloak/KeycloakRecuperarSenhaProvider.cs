using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IdentityApi.Application.Ports;
using IdentityApi.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace IdentityApi.Infrastructure.Keycloak;

/// <summary>
/// Implementação de <see cref="IRecuperarSenhaProvider"/> contra a Admin REST API do Keycloak.
///
/// Separada de <see cref="KeycloakClienteProvider"/> — cadastro e recuperação de senha são
/// motivos de mudança independentes (ver doc-comment de <see cref="ICriarClienteProvider"/>).
/// </summary>
public class KeycloakRecuperarSenhaProvider(
    HttpClient httpClient, IOptions<KeycloakOptions> options, IKeycloakTokenProvider tokenProvider)
    : IRecuperarSenhaProvider
{
    private readonly KeycloakOptions _options = options.Value;

    public async Task RecuperarSenhaAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = await tokenProvider.GetServiceAccountTokenAsync(cancellationToken);
            var userId = await BuscarIdPorEmailAsync(accessToken, email, cancellationToken);
            if (userId is null)
            {
                return; // e-mail não cadastrado — silencioso de propósito, resposta uniforme é do controller
            }

            await DispararEmailDeRedefinicaoAsync(accessToken, userId, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ProvedorIdentidadeIndisponivelException(
                "Não foi possível falar com o provedor de identidade.", ex);
        }
    }

    private async Task<string?> BuscarIdPorEmailAsync(string accessToken, string email, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"admin/realms/{_options.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true");
        request.Headers.Authorization = new("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProvedorIdentidadeIndisponivelException(
                $"Falha ao consultar e-mail no Keycloak (HTTP {(int)response.StatusCode}).");
        }

        var usuarios = await response.Content.ReadFromJsonAsync<List<KeycloakUserRead>>(cancellationToken);
        return usuarios is { Count: > 0 } ? usuarios[0].Id : null;
    }

    private async Task DispararEmailDeRedefinicaoAsync(string accessToken, string userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put, $"admin/realms/{_options.Realm}/users/{userId}/execute-actions-email")
        {
            Content = JsonContent.Create(new[] { "UPDATE_PASSWORD" }),
        };
        request.Headers.Authorization = new("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProvedorIdentidadeIndisponivelException(
                $"Falha ao disparar e-mail de redefinição no Keycloak (HTTP {(int)response.StatusCode}).");
        }
    }

    private class KeycloakUserRead
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
    }
}
