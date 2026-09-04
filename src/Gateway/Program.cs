using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS (US4.2) — até aqui todo tráfego vinha de curl/testes/scripts server-side, nenhum
// sujeito a CORS (é uma restrição do navegador, não do servidor). O frontend bônus (Épico 4)
// é o primeiro chamador de verdade rodando num navegador — sem isso, o preflight OPTIONS de
// qualquer POST/PUT/DELETE feito pelo Blazor falha antes de chegar no identity-api/vendas-api.
// Restrito às origens conhecidas do frontend (mesmas já registradas como webOrigins no client
// Keycloak vendas-frontend), não AllowAnyOrigin.
const string PoliticaCorsFrontend = "frontend";
builder.Services.AddCors(options => options.AddPolicy(PoliticaCorsFrontend, policy => policy
    .WithOrigins("http://localhost:8082", "http://localhost:5290")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors(PoliticaCorsFrontend);

// Liveness do próprio gateway — não confundir com o /health de cada serviço, roteado por
// baixo de /identity e /vendas (ver appsettings.json, seção ReverseProxy).
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gateway" }));

app.MapReverseProxy();

app.Run();

public partial class Program;
