output "kubeconfig_path" {
  description = "Kubeconfig gerado pelo provider kind — usado por infra/deploy.sh pro kubectl apply."
  value       = kind_cluster.this.kubeconfig_path
}

output "cluster_name" {
  value = kind_cluster.this.name
}
