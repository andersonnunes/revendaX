using System.Net;
using System.Net.Http.Json;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// Cenários de teste da US1.4 contra Keycloak + Mailpit reais e efêmeros — o e-mail de
/// redefinição precisa mesmo sair do Keycloak e chegar no Mailpit, não é assumido.
/// </summary>
public class RecuperarSenhaTests : IClassFixture<MailKeycloakFixture>, IAsyncLifetime
{
    private readonly MailKeycloakFixture _fixture;
    private IdentityApiFactory _factory = null!;
    private HttpClient _client = null!;
    private MailpitClient _mailpit = null!;

    public RecuperarSenhaTests(MailKeycloakFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _factory = new IdentityApiFactory(_fixture.KeycloakBaseUrl);
        _client = _factory.CreateClient();
        _mailpit = new MailpitClient(new HttpClient { BaseAddress = new Uri(_fixture.MailpitBaseUrl) });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RecuperarSenha_EmailCadastrado_Retorna202EEnviaEmail()
    {
        var request = TestData.NovoClienteValido();
        var cadastro = await _client.PostAsJsonAsync("/clientes", request);
        cadastro.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/clientes/recuperar-senha", new { email = request.Email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var recebidas = await AguardarEmailAsync(request.Email);
        Assert.Equal(1, recebidas);
    }

    [Fact]
    public async Task RecuperarSenha_EmailNaoCadastrado_Retorna202SemEnviarEmail()
    {
        var email = $"naoexiste.{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/clientes/recuperar-senha", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Mesmo tempo de espera do caso positivo, pra não confundir "não esperou o
        // suficiente" com "realmente não enviou".
        await Task.Delay(TimeSpan.FromSeconds(2));
        var recebidas = await _mailpit.ContarMensagensParaAsync(email);
        Assert.Equal(0, recebidas);
    }

    [Fact]
    public async Task RecuperarSenha_EmailMalformado_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/clientes/recuperar-senha", new { email = "não-é-um-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> AguardarEmailAsync(string email)
    {
        for (var tentativa = 0; tentativa < 10; tentativa++)
        {
            var count = await _mailpit.ContarMensagensParaAsync(email);
            if (count > 0)
            {
                return count;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return 0;
    }
}
