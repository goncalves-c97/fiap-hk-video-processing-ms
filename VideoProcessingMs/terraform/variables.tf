variable "preferred_region" {
  description = "A regiao AWS preferida para a criacao dos recursos."
  type        = string
  default     = "us-east-1"
}

variable "aws_profile" {
  description = "Perfil opcional da AWS CLI. Se null, usa credenciais do ambiente."
  type        = string
  default     = null
}

variable "infra_state_bucket" {
  description = "Bucket S3 onde fica o terraform.tfstate da stack compartilhada."
  type        = string
  default     = "fiap-terraform-backend-infra-tf"
}

variable "infra_state_key" {
  description = "Chave do terraform.tfstate da stack compartilhada."
  type        = string
  default     = "hk/infra/terraform.tfstate"
}
