variable "db_password" {
  description = "A senha para o usuário mestre do banco de dados."
  type        = string
  sensitive   = true
  default     = "1q2w3e4r5t"
}

variable "preferred_region" {
  description = "A região AWS preferida para a criação dos recursos."
  type        = string
  default     = "us-east-1"
}

variable "aws_profile" {
  description = "Optional named AWS CLI profile to use for credentials. Leave null to use default environment credentials."
  type        = string
  default     = null
}