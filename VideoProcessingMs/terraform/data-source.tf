data "aws_db_subnet_group" "rds_subnet_group" {
  name = "rds-sqlserver-subnet-group"
}

# Usamos o data source 'aws_subnets' para pegar uma lista de IDs.
data "aws_subnets" "private_subnets" {
  filter {
    name   = "vpc-id"
    values = [data.terraform_remote_state.network.outputs.vpc_id]
  }
}

data "terraform_remote_state" "network" {
  backend = "s3"

  config = {
    bucket = "fiap-terraform-backend-infra-tf"
    key    = "hk/infra/terraform.tfstate"
    region = "us-east-1"
  }
}