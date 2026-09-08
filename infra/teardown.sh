#!/usr/bin/env bash
# Desmancha tudo que infra/deploy.sh criou — cluster kind e registry, sem sobra de
# container/rede/volume na máquina. Idempotente: rodar sem nada no ar não é erro.
set -euo pipefail

cd "$(dirname "$0")/.."

terraform -chdir=infra/terraform destroy -auto-approve

# A rede Docker "kind" é criada pelo próprio kind como efeito colateral do cluster (não é um
# recurso rastreado pelo Terraform) e o registry é conectado a ela depois de criado (ver
# infra/deploy.sh — só possível depois que a rede já existe, então fora do grafo do
# Terraform). Resultado, confirmado rodando de verdade: sem esta linha, a rede "kind" ficava
# órfã depois do destroy (nada mais conectado a ela, mas ninguém a removia). `|| true`:
# se já não existir (nunca chegou a subir, ou outro cluster kind ainda a estiver usando), não
# é erro.
docker network rm kind 2>/dev/null || true
