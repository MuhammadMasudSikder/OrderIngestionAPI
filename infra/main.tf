##############################################
# main.tf – Orderingestion (Free Tier)
##############################################

terraform {
  required_version = ">= 1.7.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

#No     Subscription name     Subscription ID                       Tenant
#-----  --------------------  ------------------------------------  ----------
#[1] *  Azure subscription 1  f10a7eca-27b7-43fd-94f0-770ddc5a120e  TechVision

provider "azurerm" {
  features {}
  subscription_id = "f10a7eca-27b7-43fd-94f0-770ddc5a120e"
}

#---------------------------------------------
# Variables
#---------------------------------------------
variable "project_name" { default = "orderingestion" }
variable "location"     { default = "East Asia" }
variable "sku_sql"      { default = "GP_S_Gen5_1" }

#---------------------------------------------
# Resource Group
#---------------------------------------------
resource "azurerm_resource_group" "rg" {
  name     = "${var.project_name}-rg"
  location = var.location
}

#---------------------------------------------
# Azure Container Registry (Free/Dev SKU)
#---------------------------------------------
resource "azurerm_container_registry" "acr" {
  name                = "${var.project_name}acr"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  sku                 = "Basic"         # Cheapest available (no free SKU)
  admin_enabled       = false
}

#---------------------------------------------
# Key Vault (Free-tier equivalent)
#---------------------------------------------
data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv" {
  name                        = "${var.project_name}-kv"
  location                    = var.location
  resource_group_name          = azurerm_resource_group.rg.name
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  sku_name                    = "standard"    # Only two tiers: standard/premium
  soft_delete_retention_days  = 7
}

#---------------------------------------------
# Azure SQL (Free-tier: Serverless Dev Database)
#---------------------------------------------
resource "azurerm_mssql_server" "sql" {
  name                         = "${var.project_name}-sql"
  resource_group_name           = azurerm_resource_group.rg.name
  location                      = var.location
  administrator_login           = "sqladminuser"
  administrator_login_password  = "P@ssw0rd1234!"
  version                       = "12.0"
}

resource "azurerm_mssql_database" "sqldb" {
  name      = "${var.project_name}db"
  server_id = azurerm_mssql_server.sql.id
  sku_name  = var.sku_sql

  min_capacity                 = 0.5
  max_size_gb                  = 32
  auto_pause_delay_in_minutes  = 60  # Auto-pause after 60 minutes idle
}

#---------------------------------------------
# Azure Kubernetes Service (AKS) – Free Node Pool
#---------------------------------------------
resource "azurerm_kubernetes_cluster" "aks" {
  name                = "${var.project_name}-aks"
  location            = var.location
  resource_group_name = azurerm_resource_group.rg.name
  dns_prefix          = "${var.project_name}-dns"

  # Cheapest node pool: 1 node of smallest VM type
  default_node_pool {
    name       = "systempool"
	temporary_name_for_rotation   = "tempnp"
    node_count = 1
    vm_size    = "Standard_B2s"  # at least 2 vCPUs and 4 GB RAM.
    os_disk_size_gb = 30
  }

  identity {
    type = "SystemAssigned"
  }

  depends_on = [azurerm_container_registry.acr]
}

# Allow AKS to pull images from ACR
resource "azurerm_role_assignment" "aks_acr_pull" {
  principal_id         = azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
  role_definition_name = "AcrPull"
  scope                = azurerm_container_registry.acr.id
}
