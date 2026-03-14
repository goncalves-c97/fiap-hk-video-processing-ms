output "ecs_cluster_name" {
  description = "Cluster ECS compartilhado."
  value       = data.terraform_remote_state.infra.outputs.ecs_cluster_name
}

output "ecs_service_name" {
  description = "Nome do servico ECS de processamento."
  value       = data.terraform_remote_state.infra.outputs.ecs_service_names.video_processing
}

output "db_endpoint" {
  description = "Endpoint do banco SQL Server compartilhado com o video-upload-ms."
  value       = data.terraform_remote_state.infra.outputs.database_endpoints.video_upload
}

output "db_secret_arn" {
  description = "ARN do secret com DB_CONNECTION_STRING e DB_NAME do VideoUploadDb."
  value       = data.terraform_remote_state.infra.outputs.database_secret_arns.video_upload
}

output "shared_secret_arn" {
  description = "ARN do secret compartilhado com RabbitMQ e credenciais S3."
  value       = data.terraform_remote_state.infra.outputs.shared_secret_arn
}

output "bucket_name" {
  description = "Bucket S3 usado para buscar o video original e publicar o zip."
  value       = data.terraform_remote_state.infra.outputs.bucket_name
}

output "rabbitmq_host" {
  description = "Hostname interno do RabbitMQ."
  value       = data.terraform_remote_state.infra.outputs.rabbitmq_host
}

output "container_image" {
  description = "Imagem Docker Hub configurada para o video-processing-ms."
  value       = data.terraform_remote_state.infra.outputs.dockerhub_images.video_processing
}
