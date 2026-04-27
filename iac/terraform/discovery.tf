resource "aws_service_discovery_private_dns_namespace" "internal" {
  name        = "${local.name_prefix}.local"
  description = "PipelineEval ECS service discovery"
  vpc         = data.aws_vpc.main.id
}

resource "aws_service_discovery_service" "api" {
  name = "api"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.internal.id

    dns_records {
      ttl  = 15
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_custom_config {
    failure_threshold = 1
  }
}

locals {
  api_service_discovery_hostname = "api.${aws_service_discovery_private_dns_namespace.internal.name}"
  api_internal_base_url          = "http://${local.api_service_discovery_hostname}:8080"
}
