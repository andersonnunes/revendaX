# Registry Docker local — as 3 imagens do projeto são publicadas aqui
# (localhost:5000/<serviço>:dev) em vez de usar `kind load docker-image`, pra manter o fluxo
# de deploy parecido com o de um registry de verdade (build local -> docker push -> os
# manifests referenciam a imagem pela tag), não porque `kind load` não funcionasse.
#
# Fica no mesmo docker network do cluster (conectado depois de criado — ver
# infra/deploy.sh, não aqui: o network "kind" só existe depois que o `kind_cluster` sobe, e um
# `docker_network` deste provider referenciando esse nome criaria uma segunda rede, não a que
# o `kind` gerencia). O containerd de cada node do cluster é configurado (ver cluster.tf) pra
# resolver "localhost:5000" internamente como "kind-registry:5000" — o hostname que os nodes
# enxergam depois de conectados a essa mesma rede.
resource "docker_image" "registry" {
  name         = "registry:2"
  keep_locally = true
}

resource "docker_container" "registry" {
  name    = "kind-registry"
  image   = docker_image.registry.image_id
  restart = "always"

  ports {
    internal = 5000
    external = 5000
  }
}
