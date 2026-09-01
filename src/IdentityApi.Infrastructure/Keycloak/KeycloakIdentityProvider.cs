using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IdentityApi.Application.Clientes;
using IdentityApi.Application.Ports;
using IdentityApi.Domain.Exceptions;
using IdentityApi.Domain.Validation;
using Microsoft.Extensions.Options;

namespace IdentityApi.Infrastructure.Keycloak;

/// <summary>
/// Implementação de <see cref="IIdentityProvider"/> contra a Admin REST API do Keycloak.
/// Único ponto do sistema com credencial de escrita no Keycloak (client de serviço
/// `identity-api`, ver ADR-0001 e docs/refinamentos/US1.1-cadastro-cliente.md, fora deste
/// repositório).
/// </summary>
public class KeycloakIdentityProvider(
    HttpClient httpClient, IOptions<KeycloakOptions> options, IKeycloakTokenProvider tokenProvider)
    : IIdentityProvider
{
    private readonly KeycloakOptions _options = options.Value;

    public async Task<ClienteResult> CriarClienteAsync(CriarClienteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = await tokenProvider.GetServiceAccountTokenAsync(cancellationToken);
            var cpfDigits = CpfValidator.OnlyDigits(command.Cpf);

            await GarantirCpfDisponivelAsync(accessToken, cpfDigits, cancellationToken);

            var userId = await CriarUsuarioAsync(accessToken, command, cpfDigits, cancellationToken);

            // Não faz uma chamada extra pra "buscar o usuário recém-criado" — o Keycloak já
            // confirmou a criação (só chegamos aqui se ela deu 201); buscar de novo só pra
            // montar a resposta criaria uma janela onde o usuário existe no Keycloak mas o
            // chamador recebe 503 achando que falhou (estado divergente do ponto de vista do
            // cliente, mesmo sem estado divergente em banco nosso). Os campos da resposta já
            // são os que nós mesmos mandamos — não há nada que o Keycloak "devolveria de
            // diferente" que precisássemos ler de volta.
            return new ClienteResult
            {
                Id = userId,
                Nome = command.Nome,
                Email = command.Email,
                CriadoEm = DateTimeOffset.UtcNow,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ProvedorIdentidadeIndisponivelException(
                "Não foi possível falar com o provedor de identidade.", ex);
        }
    }

    private async Task GarantirCpfDisponivelAsync(string accessToken, string cpfDigits, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"admin/realms/{_options.Realm}/users?q=cpf:{cpfDigits}&exact=true");
        request.Headers.Authorization = new("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProvedorIdentidadeIndisponivelException(
                $"Falha ao consultar CPF no Keycloak (HTTP {(int)response.StatusCode}).");
        }

        var usuarios = await response.Content.ReadFromJsonAsync<List<object>>(cancellationToken);
        if (usuarios is { Count: > 0 })
        {
            throw new CpfJaCadastradoException();
        }
    }

    private async Task<string> CriarUsuarioAsync(
        string accessToken, CriarClienteCommand command, string cpfDigits, CancellationToken cancellationToken)
    {
        var payload = new KeycloakUserRepresentation
        {
            Username = command.Email,
            Email = command.Email,
            FirstName = command.Nome,
            Enabled = true,
            EmailVerified = false,
            Attributes = new Dictionary<string, string[]>
            {
                ["cpf"] = [cpfDigits],
                ["telefone"] = string.IsNullOrWhiteSpace(command.Telefone) ? [] : [command.Telefone],
            },
            Credentials =
            [
                new KeycloakCredentialRepresentation
                {
                    Type = "password",
                    Value = command.Senha,
                    Temporary = false,
                },
            ],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"admin/realms/{_options.Realm}/users")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new EmailJaCadastradoException();
        }

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new ProvedorIdentidadeIndisponivelException(
                $"Falha ao criar usuário no Keycloak (HTTP {(int)response.StatusCode}).");
        }

        var location = response.Headers.Location
            ?? throw new ProvedorIdentidadeIndisponivelException("Keycloak não retornou o Location do usuário criado.");
        return location.Segments[^1];
    }

    private class KeycloakUserRepresentation
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }

        [JsonPropertyName("email")]
        public required string Email { get; set; }

        [JsonPropertyName("firstName")]
        public required string FirstName { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("emailVerified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("attributes")]
        public required Dictionary<string, string[]> Attributes { get; set; }

        [JsonPropertyName("credentials")]
        public required List<KeycloakCredentialRepresentation> Credentials { get; set; }
    }

    private class KeycloakCredentialRepresentation
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("value")]
        public required string Value { get; set; }

        [JsonPropertyName("temporary")]
        public bool Temporary { get; set; }
    }
}
