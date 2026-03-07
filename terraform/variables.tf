variable "environment" {
  type = string
}

variable "workload_name" {
  description = "Name of the workload as defined in platform-workloads state"
  type        = string
  default     = "idp-web"
}

variable "location" {
  type    = string
  default = "swedencentral"
}

variable "subscription_id" {
  type = string
}

variable "idp_core_state" {
  type = object({
    resource_group_name  = string
    storage_account_name = string
    container_name       = string
    key                  = string
    subscription_id      = string
    tenant_id            = string
  })
}

variable "platform_hosting_state" {
  type = object({
    resource_group_name  = string
    storage_account_name = string
    container_name       = string
    key                  = string
    subscription_id      = string
    tenant_id            = string
  })
}

variable "platform_workloads_state" {
  type = object({
    resource_group_name  = string
    storage_account_name = string
    container_name       = string
    key                  = string
    subscription_id      = string
    tenant_id            = string
  })
}

variable "dns" {
  type = object({
    subscription_id     = string
    resource_group_name = string
    domain              = string
    subdomain           = string
  })
}

variable "tags" {
  default = {}
}
