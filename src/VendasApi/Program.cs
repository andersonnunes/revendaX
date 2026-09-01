using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Usado pelo pipeline de CD para confirmar que o deploy subiu (ver docs/refinamentos/US0).
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "vendas-api" }));

app.Run();

// Torna a classe Program (top-level statements) visível para o WebApplicationFactory
// usado em tests/VendasApi.Tests.
public partial class Program;
