# Terraform provisiona só o cluster Kubernetes local (kind) e o registry Docker que ele usa;
# os manifests do que roda dentro do cluster são YAML puro em infra/k8s/, aplicados via
# kubectl (infra/deploy.sh), não recursos `kubernetes_manifest` deste provider — configurar o
# provider `kubernetes` a partir de um kubeconfig que só existe depois do `kind_cluster` ser
# criado é um problema conhecido do Terraform (providers são configurados antes do grafo de
# recursos ser resolvido); rodar `kubectl apply` depois do cluster existir, via `local-exec`
# em `infra/deploy.sh`, contorna isso sem gambiarra de `depends_on` entre providers.

terraform {
  required_version = ">= 1.5"

  required_providers {
    kind = {
      source  = "tehcyx/kind"
      version = "~> 0.11"
    }
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 4.6"
    }
  }
}

provider "docker" {}
