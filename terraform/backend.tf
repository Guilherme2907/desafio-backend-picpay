terraform {
  // Configure o backend remoto para AWS S3
  backend "s3" {
    bucket = "picpay-simplified-bucket1" // Crie um bucket S3 manualmente para isso
    key    = "picpay-challenge/terraform.tfstate"
    region = "us-east-1"
  }
}
