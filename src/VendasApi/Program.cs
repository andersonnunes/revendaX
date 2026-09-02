using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using VendasApi.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Validação de token via JWKS (US1.3) — nunca chama a Admin REST API nem o banco do
// Keycloak; a chave pública é buscada e cacheada automaticamente a partir do discovery
// document do Authority, mantendo o vendas-api isolado da base de clientes.
//
// Configuração via AddOptions<>().Configure<IConfiguration>(...) em vez de ler
// builder.Configuration direto no delegate do AddJwtBearer — resolvido via DI, então enxerga
// o IConfiguration final (inclui overrides de teste do WebApplicationFactory), em vez do
// snapshot no momento em que este trecho roda.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var keycloakBaseUrl = configuration["Keycloak:BaseUrl"];
        options.Authority = $"{keycloakBaseUrl}/realms/clientes";
        options.RequireHttpsMetadata = false; // Keycloak local sem TLS
        options.MapInboundClaims = false; // mantém os nomes de claim originais do token (sub, email, ...)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = ["account", "vendas-frontend"],
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddTransient<IClaimsTransformation, RealmRolesClaimsTransformation>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Usado para confirmar que o container subiu corretamente (docker-compose healthcheck manual).
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "vendas-api" }));

app.Run();

// Torna a classe Program (top-level statements) visível para o WebApplicationFactory
// usado em tests/VendasApi.Tests.
public partial class Program;
