using System.Net;
using System.Net.Http.Json;
using IdentityApi.Application.Clientes;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// Cenários de teste do cadastro de cliente (`POST /clientes`) contra um Keycloak real
/// efêmero — não mockado.
/// </summary>
[Collection(nameof(IdentityApiIntegrationCollection))]
public class ClientesControllerTests : IAsyncLifetime
{
    private readonly MailKeycloakFixture _keycloak;
    private IdentityApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ClientesControllerTests(MailKeycloakFixture keycloak)
    {
        _keycloak = keycloak;
    }

    public Task InitializeAsync()
    {
        _factory = new IdentityApiFactory(_keycloak.KeycloakBaseUrl);
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
    public async Task Criar_DadosValidos_Retorna201ComIdGerado()
    {
        var request = TestData.NovoClienteValido();

        var response = await _client.PostAsJsonAsync("/clientes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ClienteResult>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Id));
        Assert.Equal(request.Email, body.Email);
    }

    [Fact]
    public async Task Criar_EmailDuplicado_Retorna409()
    {
        var request = TestData.NovoClienteValido();
        var primeira = await _client.PostAsJsonAsync("/clientes", request);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        var segunda = await _client.PostAsJsonAsync("/clientes", request with { Cpf = TestData.GerarCpfValido() });

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Criar_CpfDuplicado_Retorna409()
    {
        var cpf = TestData.GerarCpfValido();
        var primeira = await _client.PostAsJsonAsync("/clientes", TestData.NovoClienteValido() with { Cpf = cpf });
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        var segunda = await _client.PostAsJsonAsync("/clientes", TestData.NovoClienteValido() with { Cpf = cpf });

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Criar_EmailMalformado_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/clientes", TestData.NovoClienteValido() with { Email = "não-é-um-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criar_SenhaFraca_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/clientes", TestData.NovoClienteValido() with { Senha = "curta" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criar_CpfComDigitoVerificadorInvalido_Retorna422()
    {
        // Dígitos distintos (não cai na regra "todos iguais") mas dígito verificador errado —
        // exercita de fato o cálculo do dígito, não só a rejeição mais óbvia.
        var response = await _client.PostAsJsonAsync("/clientes", TestData.NovoClienteValido() with { Cpf = "123.456.789-99" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Criar_SenhaNuncaApareceNaResposta()
    {
        var request = TestData.NovoClienteValido();

        var response = await _client.PostAsJsonAsync("/clientes", request);

        var corpo = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(request.Senha, corpo);
    }

    [Fact]
    public async Task Criar_SenhaNuncaApareceEmNenhumLog()
    {
        var request = TestData.NovoClienteValido();

        var response = await _client.PostAsJsonAsync("/clientes", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Sanidade: se isso for 0, a captura de log não está funcionando e o teste abaixo
        // passaria por motivo errado (nada logado, não "nada com a senha logado").
        Assert.NotEmpty(_factory.CapturedLogMessages);

        var logsComSenha = _factory.CapturedLogMessages.Where(m => m.Contains(request.Senha)).ToList();
        Assert.Empty(logsComSenha);
    }
}
