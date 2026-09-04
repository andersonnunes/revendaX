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
| `vendas-api` | Catálogo de veículos e compras (Postgres próprio, `vendas-db`) | `5082` (debug direto) |
| Keycloak | Identity Provider (realm `clientes`, importado automaticamente do export commitado) | `8081` |
| Mailpit | SMTP fake de dev (US1.4) — captura os e-mails de redefinição de senha | `8025` (UI/API) |
| `frontend` | UI web (Blazor WebAssembly, bônus além do pedido do PDF) — login via Keycloak | `8082` |

> **Status atual**: Épico 1 (Identidade) completo — cadastro (US1.1), login direto no Keycloak
> (US1.2), validação de token no `vendas-api` via JWKS (US1.3, endpoint `/whoami` de
> diagnóstico), recuperação de senha (US1.4) e usuário `vendedor` provisionado no realm
> (US1.5). **Épico 2 (Veículos) completo** — cadastro (US2.1), edição (US2.2), listagem
> pública de veículos à venda (US2.3), listagem restrita de veículos vendidos (US2.4) e
> exclusão/soft delete (US2.5), `vendas-api` já com Clean Architecture e persistência própria
> (EF Core + Postgres). **Épico 3 (Compras) completo** — início de compra (US3.1, `POST
> /compras`, reserva o veículo e cria a compra `Pendente`), concorrência entre compras
> simultâneas (US3.2, controle otimista via `xmin`), efetivação da compra via webhook de
> pagamento simulado (US3.3), consulta de status pelo dono (US3.4, `GET /compras/{id}`) e
> expiração automática de reservas não pagas (US3.5, job em background). `gateway` ainda é só
> o esqueleto (build, testes, Docker, roteamento). **Frontend web (bônus, além do escopo
> pedido no PDF) em andamento** — login via Keycloak (Authorization Code + PKCE) implementado
> (US4.1), Blazor WebAssembly.

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

> **Importante**: o Keycloak só reimporta `infra/keycloak/realm-clientes.json` se o realm
> `clientes` **ainda não existir** no Postgres dele — como `keycloak-db-data` é um volume
> persistente, um `docker compose up` depois de uma mudança no realm (novo client, nova role
> etc.) **não aplica a mudança** silenciosamente. Depois de editar o realm, suba com
> `docker compose -f infra/docker-compose.yml down -v && docker compose -f
> infra/docker-compose.yml up --build` para forçar reimportação limpa.

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

## Testar o login (US1.2)

Login é **direto no Keycloak** — não passa pelo `identity-api` nem pelo `gateway` (porta
`8081`, exposta pelo `docker-compose`, não `8080`):

```bash
curl -X POST http://localhost:8081/realms/clientes/protocol/openid-connect/token \
  -d "grant_type=password" \
  -d "client_id=vendas-frontend" \
  -d "username=maria@example.com" \
  -d "password=SenhaForte123"
# 200 — { "access_token": "...", "expires_in": 300, "refresh_token": "...", ... }
```

O `sub` dentro do `access_token` (JWT) é o mesmo `id` retornado no cadastro. Credenciais
erradas ou e-mail não cadastrado → 401. Renovar sem logar de novo:
`grant_type=refresh_token&refresh_token={refresh_token}&client_id=vendas-frontend` no mesmo
endpoint.

## Testar a validação de token (US1.3)

`vendas-api` valida o token via JWKS — `GET /whoami` (qualquer usuário autenticado) e
`GET /whoami/cliente` (exige a role `cliente`) são endpoints de diagnóstico, sem regra de
negócio real ainda:

```bash
TOKEN=$(curl -s -X POST http://localhost:8081/realms/clientes/protocol/openid-connect/token \
  -d "grant_type=password" -d "client_id=vendas-frontend" \
  -d "username=maria@example.com" -d "password=SenhaForte123" | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

curl http://localhost:8080/vendas/whoami -H "Authorization: Bearer $TOKEN"
# 200 — { "sub": "...", "email": "...", "roles": ["cliente", ...] }

curl -o /dev/null -w "%{http_code}\n" http://localhost:8080/vendas/whoami
# 401 — sem token
```

> Sem `KC_HOSTNAME` fixo no Keycloak (já configurado em `infra/docker-compose.yml`), o `iss`
> do token variaria conforme o host/porta usado pra logar, enquanto `vendas-api` validaria
> contra outro valor — os dois nunca bateriam e todo token seria rejeitado como "issuer
> inválido". `KC_HOSTNAME=http://localhost:8081` (o endereço que o navegador enxerga, desde a
> US4.1) fixa o `iss`; `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true` mantém `jwks_uri`/`token_endpoint`
> resolvendo pelo hostname interno do Docker quando quem pergunta é um container
> (`identity-api`/`vendas-api`) — sem isso, esses serviços não conseguiriam alcançar
> `localhost:8081` de dentro do próprio container pra buscar as chaves JWKS.

## Testar a role `vendedor` (US1.5)

Não existe autocadastro de vendedor — o realm já sobe com um usuário semeado, **credencial de
desenvolvimento/demonstração local, não uma credencial real**:

```
usuário: vendedor@revendax.local
senha:   VendedorDev123
```

```bash
TOKEN=$(curl -s -X POST http://localhost:8081/realms/clientes/protocol/openid-connect/token \
  -d "grant_type=password" -d "client_id=vendas-frontend" \
  -d "username=vendedor@revendax.local" -d "password=VendedorDev123" | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

curl http://localhost:8080/vendas/whoami/vendedor -H "Authorization: Bearer $TOKEN"
# 200 — { "sub": "...", "email": "vendedor@revendax.local", "roles": ["cliente", "vendedor", ...] }
```

Um token de comprador (US1.1/US1.2) nesse mesmo endpoint → 403 (role `vendedor` ausente).

## Testar o cadastro de veículo (US2.1)

`POST /veiculos` exige token com a role `vendedor` — a migração do schema do `vendas-db` roda
automaticamente no startup do `vendas-api` (sem passo manual):

```bash
TOKEN=$(curl -s -X POST http://localhost:8081/realms/clientes/protocol/openid-connect/token \
  -d "grant_type=password" -d "client_id=vendas-frontend" \
  -d "username=vendedor@revendax.local" -d "password=VendedorDev123" | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

curl -X POST http://localhost:8080/vendas/veiculos \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"marca": "Fiat", "modelo": "Argo", "ano": 2024, "cor": "Branco", "preco": 89900.00, "placa": "ABC1D23"}'
# 201 Created — { "id": "...", "status": "Disponivel", ... }
```

Placa duplicada → 409; ano fora do intervalo (1950 até o ano atual + 1), preço ≤ 0 ou placa em
formato inválido (nem padrão antigo `AAA9999`, nem Mercosul `AAA9A99`) → 422; campo obrigatório
ausente → 400; sem token → 401; token sem a role `vendedor` → 403.

## Testar a edição de veículo (US2.2)

`PUT /veiculos/{id}` — mesma autorização do cadastro. `placa` e `status` são imutáveis por
este endpoint (não fazem parte do corpo):

```bash
curl -X PUT http://localhost:8080/vendas/veiculos/{id} \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"marca": "Fiat", "modelo": "Argo", "ano": 2025, "cor": "Prata", "preco": 85000.00}'
# 200 OK — corpo com os dados atualizados; placa/status inalterados
```

Veículo `id` inexistente → 404; veículo com `status: Vendido` → 409 (conflito de estado, não
editável); ano/preço inválidos → 422; campo obrigatório ausente → 400.

## Testar a listagem de veículos à venda (US2.3)

`GET /veiculos` é **público** — não exige token nem role. Retorna só veículos
`status: Disponivel`, ordenados por preço ascendente (desempate por data de cadastro):

```bash
curl http://localhost:8080/vendas/veiculos
# 200 — [ { "id": "...", "preco": 62000.00, "status": "Disponivel", ... }, ... ]
```

Lista vazia (`[]`, não 404) se não houver nenhum veículo `Disponivel`.

## Testar a listagem de veículos vendidos (US2.4)

`GET /veiculos/vendidos` — restrito à role `vendedor` (não é vitrine pública, é
acompanhamento comercial). Mesma ordenação por preço ascendente:

```bash
curl http://localhost:8080/vendas/veiculos/vendidos -H "Authorization: Bearer $TOKEN"
# 200 — só veículos com status: Vendido
```

Sem token → 401; token de comprador (sem role `vendedor`) → 403.

## Testar a exclusão de veículo (US2.5)

`DELETE /veiculos/{id}` — soft delete (a linha continua no banco, só `ativo` vira `false`;
some da listagem pública). Só permitido em veículo `Disponivel`; idempotente (excluir de novo
retorna 204 outra vez, não erro):

```bash
curl -X DELETE http://localhost:8080/vendas/veiculos/{id} -H "Authorization: Bearer $TOKEN"
# 204 No Content
```

Veículo `Reservado`/`Vendido` → 409; `id` inexistente → 404; sem token/role `vendedor` →
401/403.

## Testar a compra de veículo (US3.1)

`POST /compras` — restrito à role `cliente`. Reserva o veículo (`Disponivel → Reservado`) e
cria a compra `Pendente` atomicamente (a mesma transação garante que as duas escritas
acontecem juntas, ou nenhuma delas):

```bash
curl -X POST http://localhost:8080/vendas/compras \
  -H "Authorization: Bearer $TOKEN_CLIENTE" -H "Content-Type: application/json" \
  -d '{"veiculoId": "<id de um veículo Disponivel>"}'
# 201 Created — { "id": "...", "veiculoId": "...", "clienteId": "...", "preco": 89900.00, "status": "Pendente", "criadoEm": "..." }
```

`clienteId` vem do `sub` do token, nunca do corpo da requisição. `preco` é um retrato do preço
do veículo no momento da compra — mesmo que o vendedor edite o preço depois (veículo
`Reservado` continua editável, US2.2), a compra já criada não muda. Veículo `Reservado`/
`Vendido` → 409; `veiculoId` inexistente → 404; ausente/malformado → 400; sem token/role
`cliente` → 401/403.

Ainda não há endpoint de efetivação/confirmação de pagamento — o veículo fica `Reservado`
indefinidamente após esta etapa (segue no Épico 3).

## Concorrência na compra (US3.2)

Duas requisições de compra simultâneas para o mesmo veículo nunca criam duas compras: a
segunda recebe 409. Controle de concorrência otimista via `xmin` (coluna de sistema do
Postgres) em `Veiculo` — qualquer escrita concorrente sobre o mesmo veículo (duas compras, ou
uma edição correndo contra uma compra) é detectada e vira 409, não só o caminho comum de
"veículo não está mais disponível":

```bash
# Duas chamadas reais e concorrentes para o mesmo veiculoId
curl -X POST http://localhost:8080/vendas/compras \
  -H "Authorization: Bearer $TOKEN_CLIENTE" -H "Content-Type: application/json" \
  -d '{"veiculoId": "<id>"}' &
curl -X POST http://localhost:8080/vendas/compras \
  -H "Authorization: Bearer $TOKEN_CLIENTE" -H "Content-Type: application/json" \
  -d '{"veiculoId": "<id>"}' &
wait
# uma responde 201, a outra 409 — { "message": "O veículo foi alterado por outra operação simultânea." }
```

Sem retentativa automática da requisição perdedora — cabe ao cliente tentar outro veículo.

## Testar a efetivação da compra (confirmação de pagamento, US3.3)

`POST /compras/{id}/confirmar-pagamento` simula o callback de um gateway de pagamento — não
tem `[Authorize]` de usuário (não há cliente/vendedor logado nesse fluxo), é protegido por um
segredo compartilhado no header `X-Webhook-Secret` (`dev-webhook-secret` no
`docker-compose.yml`, valor de desenvolvimento). Confirma o pagamento, muda a compra para
`Concluida` e o veículo para `Vendido`:

```bash
curl -X POST http://localhost:8080/vendas/compras/{id}/confirmar-pagamento \
  -H "X-Webhook-Secret: dev-webhook-secret"
# 200 — { "id": "...", "status": "Concluida", ... }
```

Idempotente: confirmar de novo uma compra já `Concluida` (reentrega do webhook) retorna 200 de
novo, sem erro e sem mudar nada. Compra `Cancelada` → 409; `id` inexistente → 404; header
ausente/incorreto → 401. Depois da confirmação, o veículo aparece em `GET /veiculos/vendidos`
(US2.4) e some de `GET /veiculos` (US2.3).

## Testar a consulta de status da compra (US3.4)

`GET /compras/{id}` — restrito à role `cliente` **e** ao dono da compra:

```bash
curl http://localhost:8080/vendas/compras/{id} -H "Authorization: Bearer $TOKEN_CLIENTE"
# 200 — { "id": "...", "status": "Pendente" | "Concluida" | "Cancelada", ... }
```

`id` inexistente **ou** de uma compra de outro cliente → 404 (nunca 403 — não confirma pra um
cliente que um id alheio existe). Sem token → 401; token sem role `cliente` → 403.

`GET /compras` (sem id) lista **todas** as compras do cliente autenticado, qualquer status,
mais recente primeiro — `clienteId` vem só do `sub` do token, nunca de parâmetro de rota/query:

```bash
curl http://localhost:8080/vendas/compras -H "Authorization: Bearer $TOKEN_CLIENTE"
# 200 — [ { "id": "...", "status": "...", ... }, ... ] (lista vazia se o cliente não tiver nenhuma)
```

## Expiração automática de reservas (US3.5)

Uma compra `Pendente` que não é paga em 30 minutos (`Compras:TimeoutReservaMinutos`) é
cancelada automaticamente e o veículo volta a `Disponivel` — sem gatilho HTTP, é um
`BackgroundService` interno do `vendas-api` que varre o banco a cada minuto
(`Compras:IntervaloVerificacaoMinutos`). Sobe junto com `docker compose up --build`, sem
infraestrutura de agendamento externa. Ajustável via `appsettings.json`/variável de ambiente,
sem mudança de código:

```bash
# Exemplo: reduzir o timeout pra 2 minutos, verificando a cada 1 minuto (útil pra demonstração)
Compras__TimeoutReservaMinutos=2
Compras__IntervaloVerificacaoMinutos=1
```

## Testar a recuperação de senha (US1.4)

`identity-api` só dispara o e-mail — a troca de senha acontece na página hospedada do
Keycloak. E-mails de desenvolvimento são capturados pelo **Mailpit**, UI em
`http://localhost:8025`:

```bash
curl -X POST http://localhost:8080/identity/clientes/recuperar-senha \
  -H "Content-Type: application/json" -d '{"email": "maria@example.com"}'
# 202 — sempre, exista ou não o e-mail (evita enumeração de contas)
```

Abra `http://localhost:8025` para ver o e-mail capturado. O link de redefinição já vem
apontando para `http://localhost:8081/realms/clientes/login-actions/...` — abre direto no
navegador do host, sem troca manual de hostname (isso exigia um workaround manual antes da
US4.1, quando `KC_HOSTNAME` ainda apontava pro hostname interno do Docker).

## Frontend web (bônus)

Não pedido pelo PDF do desafio — feito como extra. UI em `http://localhost:8082`, Blazor
WebAssembly servido como estático via nginx. Fala direto com o Keycloak pra login
(Authorization Code + PKCE, não ROPC — diferente do resto deste README, que usa ROPC via
`curl` porque é a forma direta de testar a API) e com o `gateway` pras chamadas de API.

O client `vendas-frontend` no Keycloak mantém os dois fluxos ao mesmo tempo: ROPC (usado pelos
exemplos deste README e pela suíte de testes automatizados) continua funcionando sem nenhuma
mudança; Authorization Code + PKCE é o que a UI usa de fato.

## Como testar

```bash
dotnet test
```

Cada serviço tem seu(s) projeto(s) de teste em `tests/`, seguindo o mesmo padrão: um projeto
unitário por Domain (sem infraestrutura) e um projeto de integração contra dependências reais
via **Testcontainers** — nunca mock de banco, identity provider ou fila.

`identity-api`: `IdentityApi.Domain.Tests` (unitário, `CpfValidator`) e `IdentityApi.Tests`
(integração — Keycloak real e efêmero, com o mesmo `realm-clientes.json` importado; cobre
cadastro, login e recuperação de senha, esta última também contra um **Mailpit real** na
mesma rede Docker do teste, confirmando que o e-mail chega via API do Mailpit).

`vendas-api`: `VendasApi.Domain.Tests` (unitário, `PlacaValidator` + `Veiculo` + `Compra`) e
`VendasApi.Tests` (integração — Keycloak **e** Postgres reais e efêmeros, compartilhados por
todas as classes de teste do projeto via `[Collection]`, para não subir um par de containers
por classe; cobre validação de token — `/whoami`, `/whoami/cliente`, `/whoami/vendedor` —,
cadastro (`POST /veiculos`), edição (`PUT /veiculos/{id}`), as duas listagens
(`GET /veiculos`, pública; `GET /veiculos/vendidos`, restrita a `vendedor`), exclusão/soft
delete (`DELETE /veiculos/{id}`), início de compra (`POST /compras`), concorrência entre
compras simultâneas para o mesmo veículo (requisições HTTP concorrentes reais via
`Task.WhenAll`), efetivação da compra via webhook simulado
(`POST /compras/{id}/confirmar-pagamento`), consulta de status pelo dono
(`GET /compras/{id}`) e expiração automática de reservas
(`ICancelarComprasExpiradasUseCase` chamado diretamente, sem esperar o `BackgroundService`/
timer real), incluindo consultas diretas ao Postgres do teste para confirmar que os dados
persistidos batem com o que foi enviado, não só a resposta HTTP).

`gateway` ainda só cobre o esqueleto (`GET /health`).

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
│   ├── VendasApi/                   # host web — Controllers, Program.cs
│   ├── VendasApi.Application/       # casos de uso, comandos/resultados, portas
│   ├── VendasApi.Domain/            # entidades Veiculo/Compra, validação de placa, exceções (zero dependências)
│   └── VendasApi.Infrastructure/    # EF Core + Npgsql, migrações, unidade de trabalho
├── tests/
│   ├── Gateway.Tests/
│   ├── IdentityApi.Domain.Tests/    # unitário
│   ├── IdentityApi.Tests/           # integração (Keycloak real via Testcontainers)
│   ├── VendasApi.Domain.Tests/      # unitário
│   └── VendasApi.Tests/             # integração (Keycloak + Postgres reais via Testcontainers)
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

Cada CI também **falha se a cobertura de linha ficar abaixo de 80%** (`coverlet.msbuild`,
`/p:Threshold=80`) — código gerado (ex.: glue do Scalar/OpenAPI, sempre em `obj/`) é excluído
do cálculo (`/p:ExcludeByFile="**/obj/**/*.cs"`), senão o número ficaria artificialmente baixo
sem refletir lógica de fato não testada.
