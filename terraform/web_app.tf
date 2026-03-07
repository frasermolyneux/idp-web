resource "azurerm_linux_web_app" "app" {
  name = local.web_app_name

  resource_group_name = data.azurerm_resource_group.rg.name
  location            = data.azurerm_resource_group.rg.location

  service_plan_id = data.azurerm_service_plan.sp.id
  https_only      = true

  identity {
    type         = "UserAssigned"
    identity_ids = [local.web_identity_id]
  }

  key_vault_reference_identity_id = local.web_identity_id

  site_config {
    application_stack {
      dotnet_version = "9.0"
    }

    ftps_state          = "Disabled"
    always_on           = true
    minimum_tls_version = "1.2"

    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 5
  }

  app_settings = {
    "AZURE_CLIENT_ID"                            = local.web_identity_client_id
    "APPLICATIONINSIGHTS_CONNECTION_STRING"      = local.app_insights_connection_string
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    "ASPNETCORE_ENVIRONMENT"                     = var.environment == "prd" ? "Production" : "Development"
    "AzureAd__Instance"                          = "https://login.microsoftonline.com/"
    "AzureAd__TenantId"                          = data.azurerm_client_config.current.tenant_id
    "AzureAd__ClientId"                          = local.idp_web_app_client_id
    "AzureAd__ClientSecret"                      = format("@Microsoft.KeyVault(SecretUri=%s)", local.idp_obo_client_secret_uri)
    "CosmosDb__Endpoint"                         = local.cosmosdb_endpoint
    "CosmosDb__DatabaseName"                     = local.cosmosdb_database
    "IdpAgents__BaseUrl"                         = format("https://fn-idp-agents-%s-%s.azurewebsites.net", var.environment, var.location)
    "IdpAgents__Scopes"                          = format("api://%s/idp-agents-%s/.default", data.azurerm_client_config.current.tenant_id, var.environment)
  }

  lifecycle {
    ignore_changes = [app_settings["WEBSITE_RUN_FROM_PACKAGE"]]
  }

  tags = var.tags
}

resource "azurerm_app_service_custom_hostname_binding" "primary" {
  hostname            = local.public_hostname
  app_service_name    = azurerm_linux_web_app.app.name
  resource_group_name = azurerm_linux_web_app.app.resource_group_name

  depends_on = [
    azurerm_dns_txt_record.app_service_verification,
    azurerm_dns_cname_record.web_app
  ]
}

resource "time_sleep" "wait_for_hostname_binding" {
  create_duration = "60s"

  depends_on = [
    azurerm_app_service_custom_hostname_binding.primary
  ]
}

resource "azurerm_app_service_managed_certificate" "primary" {
  custom_hostname_binding_id = azurerm_app_service_custom_hostname_binding.primary.id

  depends_on = [
    time_sleep.wait_for_hostname_binding,
    azurerm_dns_cname_record.web_app
  ]
}

resource "azurerm_app_service_certificate_binding" "primary" {
  hostname_binding_id = azurerm_app_service_custom_hostname_binding.primary.id
  certificate_id      = azurerm_app_service_managed_certificate.primary.id
  ssl_state           = "SniEnabled"
}
