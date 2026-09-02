# ADR-0005: API Gateway (YARP) como porta única de entrada

## Status
Aceito

## Contexto

Com dois serviços (`identity-api`, ADR-0002), o cliente teria que conhecer dois hosts/portas
diferentes para consumir a plataforma. Não é um requisito do enunciado, mas simplifica o
consumo pelo time de frontend (citado no enunciado como quem vai integrar a solução depois) e
segue o mesmo padrão usado no hackathon de Arquitetura de Software desta pós-graduação, onde
um API Gateway (YARP) já centralizava roteamento, e é um ponto natural para futuramente
adicionar preocupações transversais (rate limiting, CORS, logging centralizado) sem duplicar
em cada serviço.

## Decisão

Adicionar um terceiro serviço, `gateway`, usando **YARP** (`Yarp.ReverseProxy`, .NET 10):
- Roteia `GET/POST/... /identity/{**catch-all}` → `identity-api` (removendo o prefixo
  `/identity`).
- Roteia `GET/POST/... /vendas/{**catch-all}` → `vendas-api` (removendo o prefixo `/vendas`).
- Expõe seu próprio `GET /health` (liveness do gateway em si, não confundir com o `/health`
  de cada serviço, que também fica acessível via proxy em `/identity/health` e
  `/vendas/health`).
- É a **única porta exposta externamente** no ambiente Docker Compose (`8080`);
  `identity-api`/`vendas-api` mantêm porta própria só para debug direto durante o
  desenvolvimento, não como caminho "oficial" de consumo.

**O login (US1.2) fica fora do gateway** — o cliente troca credenciais por token direto no
Keycloak, não através do `gateway`/`identity-api`. Motivo: é uma decisão da US1.2 (ROPC
direto no Keycloak, para simplificar a demonstração em vídeo via curl/Postman, sem
frontend); o gateway roteia para os dois *serviços de domínio* que construímos, não para o
Keycloak, que é peça de infraestrutura de terceiros.

## Consequências

**Positivas:**
- Um único host/porta para o time de frontend integrar, em vez de dois.
- Ponto único para adicionar, no futuro, preocupações transversais (rate limiting, CORS,
  logging/tracing centralizado) sem duplicar em `identity-api` e `vendas-api`.
- Mesmo padrão do hackathon anterior desta pós-graduação — reduz a curva de aprendizado de
  quem revisar o projeto já tendo visto esse padrão.

**Negativas / trade-offs aceitos:**
- Mais um serviço para buildar, testar, conteinerizar e escanear (Trivy) — aceito porque o
  padrão de CI por serviço já estabelecido nos outros dois serviços é replicável sem esforço
  extra: só mais um `ci-<servico>.yml`.
- Mais um salto de rede por requisição (cliente → gateway → serviço) — irrelevante no escopo
  do desafio (sem requisito de latência), mas registrado como trade-off real.
- O gateway **não** faz validação de token nem qualquer regra de negócio — é roteamento puro.
  Se essa linha for cruzada no futuro (ex.: mover a validação JWT do `vendas-api` para o
  gateway), isso merece uma decisão própria, não incluída aqui.
