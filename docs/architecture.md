# Arquitetura — Plataforma de Revenda de Veículos

Plataforma para uma revendedora de veículos automotores vender online, construída para o
Trabalho Substitutivo de Tech Challenge (Fase 3, curso SOAT — PósTech/FIAP). Este documento
descreve a arquitetura **planejada**, antes da implementação em si.

> `BACKLOG.md` e `docs/refinamentos/` (histórias, critérios de aceite, o que já foi
> executado em código) ficam **fora deste repositório** — são material de planejamento da
> atividade acadêmica, não parte da entrega. Vivem no diretório da atividade
> (`fase3/BACKLOG.md` e `fase3/docs/refinamentos/`), um nível acima deste repositório —
> por isso não há link direto aqui: uma vez que este repositório for publicado sozinho
> (ex.: no GitHub), esses arquivos não o acompanham.

## Índice

- [C4 — Nível 1: Contexto](#c4--nível-1-contexto)
- [C4 — Nível 2: Containers](#c4--nível-2-containers)
- [Fluxo ponta-a-ponta (demonstração)](#fluxo-ponta-a-ponta-demonstração)
- [Database per Service](#database-per-service)
- [Decisões de arquitetura (ADRs)](#decisões-de-arquitetura-adrs)

---

## C4 — Nível 1: Contexto

```mermaid
C4Context
    title Contexto — Plataforma de Revenda de Veículos

    Person(cliente, "Cliente", "Cadastra-se, autentica, navega o catálogo e compra veículos")
    Person(vendedor, "Vendedor/Administrador", "Cadastra e edita os veículos à venda")
    System(plataforma, "Plataforma de Revenda de Veículos", "Cadastro de clientes, catálogo de veículos e fluxo de compra")
    System_Ext(pagamento, "Gateway de pagamento", "Mock/webhook — confirma o pagamento que efetiva a compra")

    Rel(cliente, plataforma, "Cadastro, login, listagem, compra", "HTTPS")
    Rel(vendedor, plataforma, "Cadastro/edição de veículos", "HTTPS")
    Rel(pagamento, plataforma, "Webhook de confirmação de pagamento", "HTTPS")
```

## C4 — Nível 2: Containers

```mermaid
C4Container
    title Containers — Plataforma de Revenda de Veículos

    Person(cliente, "Cliente")
    Person(vendedor, "Vendedor/Administrador")

    System_Boundary(plataforma, "Plataforma de Revenda de Veículos") {
        Container(identityApi, "identity-api", ".NET 10", "Cadastro de clientes — única credencial com permissão de escrita no Keycloak")
        Container(keycloak, "Keycloak", "Keycloak (self-hosted)", "Identity Provider — realm `clientes`; emite e permite validar JWT")
        ContainerDb(keycloakDb, "Postgres (Keycloak)", "PostgreSQL", "Dados de clientes/credenciais — só o Keycloak acessa")

        Container(vendasApi, "vendas-api", ".NET 10", "Catálogo de veículos, compra e efetivação de pagamento (Épicos 2 e 3)")
        ContainerDb(vendasDb, "Postgres (vendas)", "PostgreSQL", "Veículos + compras — mesmo serviço, mesmo schema")
    }

    System_Ext(pagamento, "Gateway de pagamento (mock)")

    Rel(cliente, identityApi, "POST /clientes (cadastro)", "HTTPS")
    Rel(cliente, keycloak, "Login — troca credenciais por JWT", "HTTPS")
    Rel(cliente, vendasApi, "Lista veículos, compra", "HTTPS + Bearer JWT")
    Rel(vendedor, vendasApi, "Cadastra/edita veículos", "HTTPS + Bearer JWT")

    Rel(identityApi, keycloak, "Admin REST API (client de serviço)", "HTTPS")
    Rel(keycloak, keycloakDb, "JDBC")
    Rel(vendasApi, keycloak, "Valida token via JWKS — nunca acessa keycloakDb", "HTTPS")
    Rel(vendasApi, vendasDb, "EF Core")
    Rel(pagamento, vendasApi, "Webhook de confirmação de pagamento", "HTTPS")
```

**Notas sobre o diagrama:**
- `vendas-api` valida o JWT localmente via **JWKS** do Keycloak (chave pública) — nunca chama
  o `identity-api` nem acessa `keycloakDb` para isso. É o que garante o isolamento do serviço
  de identidade exigido pelo enunciado mesmo em tempo de execução, não só no deploy
  (ver [ADR-0002](adr/0002-dois-servicos-identity-vendas.md) e US1.3 em
  `fase3/docs/refinamentos/US1.1-cadastro-cliente.md`, fora deste repositório).
- `identity-api` é a **única** peça com credencial (client de serviço confidencial) para
  escrever no Keycloak via Admin REST API — o frontend nunca fala direto com a Admin API.
- Veículos e compras (Épicos 2 e 3) ficam no mesmo serviço/banco por decisão explícita — o
  PDF não exige separá-los entre si, só separar a identidade do resto (ver
  [ADR-0002](adr/0002-dois-servicos-identity-vendas.md)).

---

## Fluxo ponta-a-ponta (demonstração)

Corresponde ao teste início-a-fim exigido no vídeo de demonstração (US4.6): cadastro de
cliente, cadastro de veículo, compra e efetivação da compra.

```mermaid
sequenceDiagram
    participant C as Cliente
    participant V as Vendedor
    participant IA as identity-api
    participant KC as Keycloak
    participant VA as vendas-api
    participant PG as Gateway de pagamento (mock)

    C->>IA: POST /clientes (cadastro)
    IA->>KC: Admin API — cria usuário no realm `clientes`
    IA-->>C: 201 Created

    C->>KC: login (troca credenciais por token)
    KC-->>C: JWT

    V->>VA: POST /veiculos (Bearer JWT do vendedor)
    VA-->>V: 201 Created (status = disponível)

    C->>VA: GET /veiculos (à venda, ordenado por preço)
    VA-->>C: lista de veículos disponíveis

    C->>VA: POST /compras {veiculoId} (Bearer JWT)
    VA->>KC: valida o token via JWKS
    VA->>VA: veículo → reservado · compra → pendente
    VA-->>C: 201 Created

    PG-->>VA: webhook — pagamento confirmado
    VA->>VA: compra → concluído · veículo → vendido

    C->>VA: GET /compras/{id}
    VA-->>C: status = concluído
```

---

## Database per Service

```
clientes (via Keycloak)  → identity-api   (credenciais e dados pessoais do cliente)
vendas                    → vendas-api     (veículos + compras — write model único, mesmo serviço)
```

Dois bancos lógicos isolados — nenhum serviço acessa o banco do outro. `vendas-api` mantém
veículos e compras no **mesmo** schema/transação porque pertencem ao mesmo serviço (decisão
de [ADR-0002](adr/0002-dois-servicos-identity-vendas.md)): isso também simplifica a regra de
concorrência da US3.2 (reservar um veículo e criar o pedido cabem numa única transação local,
sem precisar de transação distribuída entre serviços).

---

## Decisões de arquitetura (ADRs)

Formato [MADR](https://adr.github.io/madr/) em [`docs/adr/`](adr/):

- [ADR-0001 — Keycloak como provedor de identidade](adr/0001-keycloak-como-provedor-de-identidade.md)
- [ADR-0002 — Dois serviços (identity-api + vendas-api), não três](adr/0002-dois-servicos-identity-vendas.md)
- [ADR-0003 — .NET 10 como stack](adr/0003-dotnet-10.md)
- [ADR-0004 — Scalar em vez de Swagger/Swashbuckle](adr/0004-scalar-em-vez-de-swagger.md)
