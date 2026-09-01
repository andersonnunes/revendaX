# ADR-0002: Dois serviços (identity-api + vendas-api), não três

## Status
Aceito

## Contexto

O enunciado diz: "o processo de registro e autorização de compradores deve ser feito de forma
separada [...] esse serviço deve estar **totalmente apartado** do resto da solução." A
exigência de isolamento total é explícita **apenas** para o serviço de identidade — o texto
fala em "resto da solução" no singular, sem pedir que veículos e compras (Épicos 2 e 3) virem
serviços distintos entre si.

Uma primeira iteração desta arquitetura chegou a propor três serviços
(`identity-api` + `veiculos-api` + `compras-api`), por analogia a um desenho de
microsserviços mais granular — mas isso adiciona infraestrutura (mais um Dockerfile, mais um
pipeline, mais um deploy, chamada de rede entre `veiculos-api` e `compras-api` para checar
disponibilidade do veículo) sem que o enunciado peça.

## Decisão

**Dois serviços**, em monorepo:
- `identity-api` — isolado, único com exigência explícita de separação total (ADR-0001).
- `vendas-api` — veículos (Épico 2) e compras (Épico 3) juntos, mesmo banco/transação.

Cada serviço com Dockerfile e pipeline de CI/CD independente (build/teste/deploy isolados por
serviço, disparado só por mudança no path daquele serviço).

## Consequências

**Positivas:**
- Menos infraestrutura para manter dentro do prazo do desafio, sem abrir mão de nada exigido
  pelo enunciado.
- Reservar um veículo e criar o pedido de compra (US3.1/US3.2) cabem numa única transação de
  banco local — sem precisar de transação distribuída nem de um mecanismo de compensação
  entre `veiculos-api` e `compras-api`.
- `identity-api` continua podendo ser deployado, escalado e substituído independentemente do
  `vendas-api` — o isolamento que de fato importa (dados de cliente vs. dados transacionais)
  é preservado.

**Negativas / trade-offs aceitos:**
- `vendas-api` cresce mais que os outros dois seriam individualmente — se o domínio de
  vendas crescer muito no futuro, separar veículos de compras volta a fazer sentido; não é o
  caso no escopo atual do desafio.
- Menos "cara" de microsserviços granulares para efeito de demonstração — aceito porque o
  critério de avaliação é o enunciado, não um padrão de arquitetura por si só.
