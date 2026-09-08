# Cluster kind, nó único. Reproduz as portas que o docker-compose já expõe hoje
# (gateway :8080, Keycloak :8081, Mailpit :8025) via `extraPortMappings` — a maioria dos
# exemplos `curl http://localhost:8080/...` já escritos no README continua funcionando sem
# editar nenhum exemplo, só troca o que está por trás da porta. As portas de debug direto
# (5081/5082 no docker-compose) não ganham mapeamento aqui: no cluster, tudo passa pelo
# gateway (mesma filosofia de "porta única de entrada" do ADR-0005).
#
# `extraPortMappings` liga host:porta -> uma porta no próprio node (que é, por baixo, um
# container Docker) — não chega sozinho num Pod. Os Deployments do gateway/keycloak/mailpit
# (infra/k8s/) usam `hostPort` no container pra de fato ocupar essa porta dentro do node,
# mesmo mecanismo que dispensa um Ingress controller aqui (só um node, sem necessidade real de
# balancear entre vários).
resource "kind_cluster" "this" {
  name           = "revendax"
  wait_for_ready = true

  # Fixa a mesma imagem de node que a versão do kind já usa por padrão nesta máquina — sem
  # isso, o provider Terraform (que embute sua própria versão da lib do kind, não
  # necessariamente igual ao binário `kind` instalado à parte) pode tentar puxar uma tag
  # diferente da já cacheada localmente.
  node_image = "kindest/node:v1.27.3"

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    # Faz o containerd de cada node resolver "localhost:5000" (o nome que os manifests em
    # infra/k8s/ usam pra referenciar as imagens) como o registry local, alcançável pelo
    # hostname que os nodes enxergam depois de entrarem na mesma rede Docker dele
    # (infra/deploy.sh conecta isso depois do cluster existir — ver registry.tf).
    containerd_config_patches = [
      <<-TOML
      [plugins."io.containerd.grpc.v1.cri".registry.mirrors."localhost:5000"]
        endpoint = ["http://kind-registry:5000"]
      TOML
    ]

    node {
      role = "control-plane"

      extra_port_mappings {
        container_port = 8080 # gateway
        host_port       = 8080
      }
      extra_port_mappings {
        container_port = 8081 # keycloak
        host_port       = 8081
      }
      extra_port_mappings {
        container_port = 8025 # mailpit (UI)
        host_port       = 8025
      }
    }
  }

  depends_on = [docker_container.registry]
}
