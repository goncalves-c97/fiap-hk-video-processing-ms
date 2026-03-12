
output "private_subnet_ids" {
  value = data.aws_subnets.private_subnets.ids
}

output "rds_security_group_id" {
  description = "The ID of the security group for the RDS instance."
  value       = data.terraform_remote_state.network.outputs.rds_security_group_id
}