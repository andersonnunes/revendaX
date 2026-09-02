# ADR-0004: Scalar em vez de Swagger/Swashbuckle

## Status
Aceito

## Contexto

O template padrão `dotnet new webapi` (mesmo em .NET 8/10) ainda referencia
`Swashbuckle.AspNetCore` (Swagger/SwaggerUI) para documentar e explorar a API. Desde o .NET 9,
o ASP.NET Core tem geração de OpenAPI **nativa** (`Microsoft.AspNetCore.OpenApi`), e o
Swashbuckle tem apresentado incompatibilidades com esse gerador mais novo (o pacote foi
desenhado para o gerador de OpenAPI anterior, mantido por terceiros, com atualizações mais
lentas que o ritmo de releases do .NET).

Precisamos de uma UI de exploração de API (para o time de frontend testar os endpoints antes
da integração, e para a demonstração em vídeo) que funcione bem com .NET 10 (ADR-0003).

## Decisão

Usar a geração de OpenAPI **nativa** do ASP.NET Core (`Microsoft.AspNetCore.OpenApi`,
`AddOpenApi()` + `MapOpenApi()`) como fonte da especificação, com **Scalar**
(`Scalar.AspNetCore`, `MapScalarApiReference()`) como UI de exploração — no lugar de
Swashbuckle/SwaggerUI.

## Consequências

**Positivas:**
- Sem dependência de um gerador de OpenAPI de terceiros desalinhado do ciclo de release do
  .NET — a especificação vem do próprio framework.
- Scalar é mais leve e tem UI mais moderna que o SwaggerUI clássico, sem custo adicional de
  configuração relevante.

**Negativas / trade-offs aceitos:**
- Menos onipresente que Swagger — quem já conhece SwaggerUI de outros projetos precisa se
  adaptar à UI do Scalar (baixo custo, mas existe).
- Um primeiro esqueleto (descartado — ver [ADR-0003](0003-dotnet-10.md)) foi gerado pelo
  template padrão, que vem com `Swashbuckle.AspNetCore` por default — ao recriar o esqueleto
  direto em `net10.0`, já nasceu com Scalar, sem migração intermediária.
