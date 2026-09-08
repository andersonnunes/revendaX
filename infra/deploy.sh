#!/usr/bin/env bash
# Deploy automatizado: cluster Kubernetes local (kind) provisionado via Terraform, com o
# sistema inteiro rodando nele — um comando, do zero até os endpoints respondendo, mesmo
# critério já usado pelo `docker compose up --build` (esse continua existindo e funcionando,
# ver README, esta é só uma segunda forma de subir tudo).
#
# Rodar a partir da raiz do repositório: ./infra/deploy.sh
set -euo pipefail

cd "$(dirname "$0")/.."
REGISTRY=localhost:5000

echo "==> 1/5 Provisionando cluster (kind) e registry (Terraform)"
terraform -chdir=infra/terraform init -input=false
terraform -chdir=infra/terraform apply -auto-approve
KUBECONFIG_PATH=$(terraform -chdir=infra/terraform output -raw kubeconfig_path)
export KUBECONFIG="$KUBECONFIG_PATH"

echo "==> 2/5 Conectando o registry à rede do cluster"
# O node do kind só resolve "kind-registry" (usado no containerd_config_patches, ver
# infra/terraform/cluster.tf) depois que os dois containers estão na mesma rede Docker — a
# rede "kind" só existe depois que o cluster já subiu, por isso esse passo é aqui, não dentro
# do Terraform. `|| true`: já conectado numa execução anterior não é erro.
docker network connect kind kind-registry 2>/dev/null || true

# Documenta o registry local no cluster (KEP-1755) — não é estritamente necessário pras
# imagens serem puxadas (o containerd_config_patches já resolve isso sozinho), mas é a
# convenção que outras ferramentas Kubernetes usam pra descobrir um registry local.
kubectl apply -f - <<EOF
apiVersion: v1
kind: ConfigMap
metadata:
  name: local-registry-hosting
  namespace: kube-public
data:
  localRegistryHosting.v1: |
    host: "localhost:5000"
    help: "https://kind.sigs.k8s.io/docs/user/local-registry/"
EOF

echo "==> 3/5 Build e push das imagens"
for servico in Gateway:gateway IdentityApi:identity-api VendasApi:vendas-api; do
  pasta="${servico%%:*}"
  nome="${servico##*:}"
  docker build -t "$REGISTRY/$nome:dev" -f "src/$pasta/Dockerfile" .
  docker push "$REGISTRY/$nome:dev"
done

echo "==> 4/5 Aplicando manifests"
kubectl apply -f infra/k8s/00-namespace.yaml

# O realm continua com uma única fonte (infra/keycloak/realm-clientes.json) — o ConfigMap é
# gerado a partir dele aqui, não mantido como uma cópia à mão dentro de infra/k8s/.
kubectl create configmap keycloak-realm \
  --namespace revendax \
  --from-file=infra/keycloak/realm-clientes.json \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -f infra/k8s/

echo "==> 5/5 Esperando os Deployments ficarem prontos"
kubectl -n revendax rollout status deployment --timeout=180s

cat <<EOF

Sistema no ar (cluster kind "revendax"):
  gateway:  http://localhost:8080/health
            http://localhost:8080/identity/health
            http://localhost:8080/vendas/health
  keycloak: http://localhost:8081
  mailpit:  http://localhost:8025

KUBECONFIG desta sessão: $KUBECONFIG_PATH
Pra desmanchar tudo: ./infra/teardown.sh
EOF
