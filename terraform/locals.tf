locals {
  workload_resource_group = data.terraform_remote_state.platform_workloads.outputs.workload_resource_groups[var.workload_name][var.environment].resource_groups[lower(var.location)]
  resource_group_name     = local.workload_resource_group.name

  app_service_plan = data.terraform_remote_state.platform_hosting.outputs.app_service_plans["default"]

  web_identity_id           = data.terraform_remote_state.idp_core.outputs.idp_web_mi_id
  web_identity_client_id    = data.terraform_remote_state.idp_core.outputs.idp_web_mi_client_id
  web_identity_principal_id = data.terraform_remote_state.idp_core.outputs.idp_web_mi_principal_id

  app_insights_connection_string   = data.terraform_remote_state.idp_core.outputs.app_insights_connection_string
  app_insights_instrumentation_key = data.terraform_remote_state.idp_core.outputs.app_insights_instrumentation_key

  key_vault_uri     = data.terraform_remote_state.idp_core.outputs.key_vault_uri
  cosmosdb_endpoint = data.terraform_remote_state.idp_core.outputs.cosmosdb_endpoint
  cosmosdb_database = data.terraform_remote_state.idp_core.outputs.cosmosdb_database_name

  idp_web_app_client_id     = data.terraform_remote_state.idp_core.outputs.idp_web_app_client_id
  idp_agents_app_client_id  = data.terraform_remote_state.idp_core.outputs.idp_agents_app_client_id
  idp_obo_client_secret_uri = data.terraform_remote_state.idp_core.outputs.idp_obo_client_secret_uri

  web_app_name = format("app-idp-web-%s-%s", var.environment, var.location)
}
