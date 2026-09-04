using Frontend;
using Frontend.Auth;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Roda no navegador do usuário, não dentro da rede do Docker — precisa do hostname que o
// próprio navegador enxerga (localhost:<porta publicada>), diferente do `Keycloak__BaseUrl`
// interno (`http://keycloak:8080`) usado pelos serviços .NET do lado do servidor.
var gatewayBaseAddress = builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:8080/";

// Authorization Code + PKCE (US4.1) — não ROPC: Blazor WebAssembly tem suporte nativo a este
// fluxo via esta mesma biblioteca; o usuário é redirecionado pra tela de login hospedada do
// próprio Keycloak, nunca digita a senha em código nosso.
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Keycloak", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
})
    .AddAccountClaimsPrincipalFactory<RealmRolesClaimsPrincipalFactory>();

// HttpClient com o token anexado automaticamente nas chamadas pro gateway — as páginas
// seguintes (US4.2+) só injetam este client, sem reimplementar anexação de Bearer.
builder.Services.AddHttpClient("Gateway", client => client.BaseAddress = new Uri(gatewayBaseAddress))
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
        handler.ConfigureHandler(authorizedUrls: [gatewayBaseAddress]);
        return handler;
    });
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gateway"));

await builder.Build().RunAsync();
