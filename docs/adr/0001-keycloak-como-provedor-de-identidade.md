# ADR-0001: Keycloak como provedor de identidade

## Status
Aceito

## Contexto

O enunciado exige que o cadastro/autorização de compradores seja **totalmente apartado** do
resto da solução, e cita explicitamente três caminhos possíveis: um serviço dedicado (Auth0),
integração com serviço existente (Cognito), um serviço local (Keycloak), ou uma implementação
totalmente personalizada.

Critérios para decidir: custo (o projeto não tem orçamento de nuvem dedicado), facilidade de
demonstrar em vídeo/CI sem depender de conta externa, e maturidade de integração com .NET via
OpenID Connect/OAuth2.

## Decisão

Usar **Keycloak self-hosted via Docker**, com um realm dedicado (`clientes`) isolado de
qualquer outro realm que venha a existir.

- Auth0/Cognito exigem conta em serviço de terceiros, custo potencial e dependência de rede
  externa para rodar CI e gravar o vídeo de demonstração — Keycloak roda inteiramente local.
- Implementação 100% personalizada (hash de senha, emissão de JWT, etc.) reinventa uma peça
  crítica de segurança sem necessidade — o enunciado já aceita uma solução pronta.
- Keycloak tem suporte nativo a OpenID Connect/OAuth2 e integra bem com ASP.NET Core
  (`Microsoft.AspNetCore.Authentication.JwtBearer` + validação via JWKS).

## Consequências

**Positivas:**
- Nenhuma dependência de conta/custo externo — Keycloak sobe via `docker-compose` tanto local
  quanto em CI (container efêmero com o realm importado).
- Emissão e validação de token (JWKS) seguem um padrão aberto, sem código de segurança
  proprietário para manter.
- Configuração do realm é versionável como JSON export (`infra/keycloak/realm-clientes.json`),
  reprodutível em qualquer ambiente.

**Negativas / trade-offs aceitos:**
- CPF não é campo nativo do Keycloak — vira *user attribute* customizado, e sua unicidade
  precisa ser checada pela aplicação (`identity-api`), não pelo Keycloak.
- Um serviço a mais para operar (Keycloak + seu próprio Postgres) além dos serviços de
  domínio — aceito porque é exatamente o isolamento que o enunciado pede.
