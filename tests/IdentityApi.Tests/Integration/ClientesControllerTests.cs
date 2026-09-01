using System.Net;
using System.Net.Http.Json;
using IdentityApi.Application.Clientes;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// Cenários de teste da US1.1 (ver fase3/docs/refinamentos/US1.1-cadastro-cliente.md, fora
/// deste repositório) contra um Keycloak real efêmero — não mockado.
/// </summary>
public class ClientesControllerTests : IClassFixture<KeycloakContainerFixture>, IAsyncLifetime
{
    private readonly KeycloakContainerFixture _keycloak;
    private IdentityApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ClientesControllerTests(KeycloakContainerFixture keycloak)
    {
        _keycloak = keycloak;
    }

    public Task InitializeAsync()
    {
        _factory = new IdentityApiFactory(_keycloak.BaseUrl);
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
        var request = NovoRequestValido();

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
        var request = NovoRequestValido();
        var primeira = await _client.PostAsJsonAsync("/clientes", request);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        var segunda = await _client.PostAsJsonAsync("/clientes", request with { Cpf = GerarCpfValido() });

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Criar_CpfDuplicado_Retorna409()
    {
        var cpf = GerarCpfValido();
        var primeira = await _client.PostAsJsonAsync("/clientes", NovoRequestValido() with { Cpf = cpf });
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

        var segunda = await _client.PostAsJsonAsync("/clientes", NovoRequestValido() with { Cpf = cpf });

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Criar_EmailMalformado_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/clientes", NovoRequestValido() with { Email = "não-é-um-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criar_SenhaFraca_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/clientes", NovoRequestValido() with { Senha = "curta" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criar_CpfComDigitoVerificadorInvalido_Retorna422()
    {
        // Dígitos distintos (não cai na regra "todos iguais") mas dígito verificador errado —
        // exercita de fato o cálculo do dígito, não só a rejeição mais óbvia.
        var response = await _client.PostAsJsonAsync("/clientes", NovoRequestValido() with { Cpf = "123.456.789-99" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Criar_SenhaNuncaApareceNaResposta()
    {
        var request = NovoRequestValido();

        var response = await _client.PostAsJsonAsync("/clientes", request);

        var corpo = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(request.Senha, corpo);
    }

    [Fact]
    public async Task Criar_SenhaNuncaApareceEmNenhumLog()
    {
        var request = NovoRequestValido();

        var response = await _client.PostAsJsonAsync("/clientes", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Sanidade: se isso for 0, a captura de log não está funcionando e o teste abaixo
        // passaria por motivo errado (nada logado, não "nada com a senha logado").
        Assert.NotEmpty(_factory.CapturedLogMessages);

        var logsComSenha = _factory.CapturedLogMessages.Where(m => m.Contains(request.Senha)).ToList();
        Assert.Empty(logsComSenha);
    }

    private static ClienteRequestDto NovoRequestValido() => new(
        Nome: "Maria Silva",
        Email: $"maria.{Guid.NewGuid():N}@example.com",
        Cpf: GerarCpfValido(),
        Senha: "SenhaForte123",
        Telefone: "11999990000");

    /// <summary>
    /// Gera um CPF com dígitos verificadores válidos para uso nos testes — não reaproveita
    /// IdentityApi.Domain.Validation.CpfValidator (que só valida, não gera); é geração de
    /// massa de teste, não a lógica sendo testada.
    /// </summary>
    private static string GerarCpfValido()
    {
        var random = Random.Shared;
        int[] digits;
        do
        {
            digits = Enumerable.Range(0, 9).Select(_ => random.Next(0, 10)).ToArray();
        } while (digits.Distinct().Count() == 1);

        var d1 = CalcularDigito(digits, 9);
        var d2 = CalcularDigito([.. digits, d1], 10);
        return string.Concat(digits) + d1 + d2;
    }

    private static int CalcularDigito(int[] numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * (length + 1 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private record ClienteRequestDto(string Nome, string Email, string Cpf, string Senha, string? Telefone);
}
