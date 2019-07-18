provider "azuread" {
}

# Create an application
resource "azuread_application" "ecommerce_azuread_app" {
  name = "${var.azuread_app_name}"
  available_to_other_tenants = false
  oauth2_allow_implicit_flow = true
}

# Create a service principal
resource "azuread_service_principal" "ecommerce_service_principal" {
  application_id = "${azuread_application.ecommerce_azuread_app.application_id}"
}

resource "azuread_application_password" "ecommerce_service_principal_password" {
  application_id        = "${azuread_application.ecommerce_azuread_app.id}"
  value                 = "${var.azuread_app_password}"
  end_date              = "2025-01-01T01:02:03Z"
}

resource "azurerm_key_vault_access_policy" "ecommerce_azuread_app" {
  key_vault_id = "${azurerm_key_vault.ecommerce_kv.id}"

  tenant_id = "${data.azurerm_client_config.current.tenant_id}"
  object_id = "${azuread_service_principal.ecommerce_service_principal.id}"

  key_permissions = [
      "create",
      "get",
      "list",
      "update"
  ]

  secret_permissions = [
      "set",
      "get",
      "list",
      "delete",
  ]
}

data "azurerm_subscription" "primary" {}

# This assigns RBAc role of Owner to our Azure App EcommerceApplication on resource App Configuration
resource "azurerm_role_assignment" "ecommerce_azuread_app" {
  scope                = "${data.azurerm_subscription.primary.id}/resourceGroups/${azurerm_resource_group.rg.name}/providers/Microsoft.AppConfiguration/configurationStores/${var.appconfig_name}"
  role_definition_name = "Owner"
  principal_id         = "${azuread_service_principal.ecommerce_service_principal.id}"
  depends_on           = ["azurerm_template_deployment.ecommerce_app_config"]
}