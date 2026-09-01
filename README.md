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
| `identity-api` | Cadastro de clientes — única credencial com permissão de escrita no Keycloak | `5081` |
| `vendas-api` | Catálogo de veículos e compras | `5082` |
| Keycloak | Identity Provider (realm `clientes`, ainda não configurado — ver status abaixo) | `8081` |

> **Status atual**: só o esqueleto dos dois serviços existe (build, testes, Docker, health
> check). Nenhuma regra de negócio foi implementada ainda — o Keycloak sobe "puro", sem o
> realm `clientes` importado.

## Como rodar localmente

Pré-requisitos: [.NET SDK](https://dotnet.microsoft.com/download) na versão pinada em
[`global.json`](global.json), e [Docker](https://www.docker.com/) com Compose.

**Só os serviços .NET, sem container** (útil durante o desenvolvimento):

```bash
dotnet restore
dotnet build
dotnet run --project src/IdentityApi   # http://localhost:5081 (porta do launchSettings.json)
dotnet run --project src/VendasApi     # http://localhost:5082 (porta do launchSettings.json)
```

Cada serviço expõe `/health` (200 OK) e, em `Development`, a documentação da API via Scalar
em `/scalar`.

**Ambiente completo via Docker Compose** (serviços + Keycloak + Postgres de cada serviço):

```bash
docker compose -f infra/docker-compose.yml up --build
```

## Como testar

```bash
dotnet test
```

Cada serviço tem seu projeto de teste correspondente em `tests/` (`IdentityApi.Tests`,
`VendasApi.Tests`). Por enquanto, cobrem só o esqueleto (`GET /health`) — a cobertura de
regra de negócio entra junto de cada funcionalidade.

## Estrutura

```
revendaX/
├── global.json              # versão do SDK .NET pinada
├── revendaX.slnx
├── src/
│   ├── IdentityApi/
│   └── VendasApi/
├── tests/
│   ├── IdentityApi.Tests/
│   └── VendasApi.Tests/
├── infra/
│   └── docker-compose.yml
├── docs/
│   ├── architecture.md
│   └── adr/
└── .github/workflows/
    ├── ci-identity-api.yml
    └── ci-vendas-api.yml
```

Cada serviço tem CI independente (`.github/workflows/ci-<servico>.yml`), disparado só por
mudanças no seu próprio path — uma mudança em `vendas-api` não builda nem testa
`identity-api`, e vice-versa.
