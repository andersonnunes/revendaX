using System.Net;
using System.Net.Http.Json;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// Cenário isolado (sem o Keycloak real do <see cref="KeycloakContainerFixture"/>): aponta
/// para um endereço que ninguém escuta, para forçar falha de conexão e confirmar que vira
/// 503, não um 500 genérico nem uma resposta travada.
/// </summary>
public class ClientesControllerKeycloakIndisponivelTests : IAsyncLifetime
{
    private IdentityApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new IdentityApiFactory("http://localhost:1"); // porta que ninguém escuta
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Criar_KeycloakIndisponivel_Retorna503()
    {
        var response = await _client.PostAsJsonAsync("/clientes", new
        {
            Nome = "Maria Silva",
            Email = "maria@example.com",
            Cpf = "529.982.247-25",
            Senha = "SenhaForte123",
            Telefone = "11999990000",
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
