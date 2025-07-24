terraform {
  backend "azurerm" {
    resource_group_name  = "rg-picpay-simplified"
    storage_account_name = "acrpicpaysimplified"
    container_name       = "tfstate"
    key                  = "picpay-simplified.terraform.tfstate"
  }
}