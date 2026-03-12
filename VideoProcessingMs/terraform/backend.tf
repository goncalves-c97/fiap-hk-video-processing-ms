terraform {
  backend "s3" {
    bucket = "fiap-terraform-backend-infra-tf"
    key    = "hk/video-processing-ms/terraform.tfstate"
    region = "us-east-1"
  }
}
