##############################################
# outputs.tf
##############################################
output "resource_group" { value = azurerm_resource_group.rg.name }
output "acr_login_server" { value = azurerm_container_registry.acr.login_server }
output "aks_name" { value = azurerm_kubernetes_cluster.aks.name }
output "sql_server" { value = azurerm_mssql_server.sql.fully_qualified_domain_name }