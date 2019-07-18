output "app_name" {
  value = "${azuread_application.ecommerce_azuread_app.name}"
}

output "app_id" {
  value = "${azuread_application.ecommerce_azuread_app.id}"
}

output "app_client_id" {
  value = "${azuread_application.ecommerce_azuread_app.application_id}"
}

output "service_principal_id" {
  value = "${azuread_service_principal.ecommerce_service_principal.id}"
}

output "instrumentation_key" {
  value = "${azurerm_application_insights.ecommerce_ai.instrumentation_key}"
}

output "managed_identity" {
  value = "RunAs=App;AppId=${azuread_application.ecommerce_azuread_app.application_id};TenantId=${data.azurerm_client_config.current.tenant_id};AppKey=${var.azuread_app_password}"
}