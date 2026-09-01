# RevendaX

Plataforma para uma revendedora de veículos automotores vender online — Trabalho
Substitutivo de Tech Challenge (Fase 3, curso SOAT, PósTech/FIAP).

Documentação de arquitetura (diagramas C4, fluxo ponta-a-ponta) e as decisões de projeto
(ADRs) estão em [`docs/architecture.md`](docs/architecture.md) e [`docs/adr/`](docs/adr/).
O backlog e o detalhamento de cada história ficam fora deste repositório — são material de
planejamento da atividade acadêmica, não parte da entrega.

## Serviços

| Serviço | Descrição | Porta local (docker-compose) |
|---|---|---|
| `gateway` | Porta única de entrada (YARP) — roteia `/identity/**` e `/vendas/**` | `8080` |
| `identity-api` | Cadastro de clientes — única credencial com permissão de escrita no Keycloak | `5081` (debug direto) |
| `vendas-api` | Catálogo de veículos e compras | `5082` (debug direto) |
| Keycloak | Identity Provider (realm `clientes`, ainda não configurado — ver status abaixo) | `8081` |

> **Status atual**: `identity-api` já implementa o cadastro de cliente (US1.1) — o Keycloak
> sobe com o realm `clientes` importado (`infra/keycloak/realm-clientes.json`). `vendas-api`
> e `gateway` ainda são só o esqueleto (build, testes, Docker, health check, roteamento).

## Como rodar localmente

Pré-requisitos: [.NET SDK](https://dotnet.microsoft.com/download) na versão pinada em
[`global.json`](global.json), e [Docker](https://www.docker.com/) com Compose.

**Só os serviços .NET, sem container** (útil durante o desenvolvimento):

```bash
dotnet restore
dotnet build
dotnet run --project src/IdentityApi   # http://localhost:5081 (porta do launchSettings.json)
dotnet run --project src/VendasApi     # http://localhost:5082 (porta do launchSettings.json)
dotnet run --project src/Gateway       # http://localhost:5083 (porta do launchSettings.json)
```

Rodando assim (sem Docker), o `Gateway` não encontra `identity-api`/`vendas-api` nos
hostnames de container — sobrescreva os destinos do YARP via env var, ex.:
`ReverseProxy__Clusters__identity-cluster__Destinations__destination1__Address=http://localhost:5081/`
(ver comentário em `src/Gateway/appsettings.json`). Do mesmo jeito, `IdentityApi` sozinho
precisa de um Keycloak acessível e do client secret — se estiver usando o Keycloak do
`docker-compose` (porta `8081`) enquanto roda o `identity-api` fora de container:
`Keycloak__BaseUrl=http://localhost:8081 Keycloak__ClientSecret=dev-identity-api-secret dotnet run --project src/IdentityApi`.

Cada serviço expõe `/health` (200 OK) e, em `Development`, a documentação da API via Scalar
em `/scalar`.

**Ambiente completo via Docker Compose** (todos os serviços + Keycloak + Postgres de cada
um, já com o roteamento do gateway funcionando):

```bash
docker compose -f infra/docker-compose.yml up --build
# gateway:        http://localhost:8080/health
# via gateway:     http://localhost:8080/identity/health
#                  http://localhost:8080/vendas/health
```

## Testar o cadastro de cliente (US1.1)

Com a stack no ar (`docker compose up`, acima):

```bash
curl -X POST http://localhost:8080/identity/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Maria Silva",
    "email": "maria@example.com",
    "cpf": "529.982.247-25",
    "senha": "SenhaForte123",
    "telefone": "11999990000"
  }'
# 201 Created — { "id": "...", "nome": "Maria Silva", "email": "maria@example.com", "criadoEm": "..." }
```

E-mail ou CPF repetido → 409; e-mail malformado ou senha curta → 400; CPF com dígito
verificador inválido → 422.

## Como testar

```bash
dotnet test
```

Cada serviço tem seu(s) projeto(s) de teste em `tests/`. `identity-api` tem dois:
`IdentityApi.Domain.Tests` (unitário, `CpfValidator`, sem infraestrutura) e `IdentityApi.Tests`
(integração — sobe um **Keycloak real e efêmero via Testcontainers**, com o mesmo
`realm-clientes.json` importado, e exercita `POST /clientes` de verdade contra ele; não
mocka a Admin API). `vendas-api`/`gateway` ainda só cobrem o esqueleto (`GET /health`).

## Estrutura

```
revendaX/
├── global.json              # versão do SDK .NET pinada
├── revendaX.slnx
├── src/
│   ├── Gateway/                    # YARP — porta única de entrada
│   ├── IdentityApi/                 # host web — Controllers, Program.cs
│   ├── IdentityApi.Application/     # casos de uso, comandos/resultados, portas
│   ├── IdentityApi.Domain/          # regra de CPF, exceções de negócio (zero dependências)
│   ├── IdentityApi.Infrastructure/  # implementação contra a Admin REST API do Keycloak
│   └── VendasApi/
├── tests/
│   ├── Gateway.Tests/
│   ├── IdentityApi.Domain.Tests/    # unitário
│   ├── IdentityApi.Tests/           # integração (Keycloak real via Testcontainers)
│   └── VendasApi.Tests/
├── infra/
│   ├── docker-compose.yml
│   └── keycloak/realm-clientes.json  # realm `clientes` exportado (US1.1)
├── docs/
│   ├── architecture.md
│   └── adr/
└── .github/workflows/
    ├── ci-gateway.yml
    ├── ci-identity-api.yml
    └── ci-vendas-api.yml
```

Cada serviço tem CI independente (`.github/workflows/ci-<servico>.yml`), disparado só por
mudanças no seu próprio path — uma mudança em `vendas-api` não builda nem testa
`identity-api` nem `gateway`, e por aí vai.
