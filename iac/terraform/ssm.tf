# SSM Parameter Store (SecureString) for runtime secrets. API loads via Aws:SsmParameterPrefix.
# Values populated by Terraform (e.g. DB) and/or scripts/sync-env-to-ssm.ps1 from .env.

# Aurora connection string — owned by Terraform (rotates on apply if DB password changes).
resource "aws_ssm_parameter" "connection_string_pipelineeval" {
  name        = "${local.ssm_secrets_prefix}/ConnectionStrings__pipelineeval"
  description = "Npgsql connection string for PipelineEval.Api"
  type        = "SecureString"
  value       = local.db_connection_string
}

# Optional placeholders; script overwrites. ignore_changes prevents terraform apply from wiping manual/synced values.
resource "aws_ssm_parameter" "observability_api_key" {
  name        = "${local.ssm_secrets_prefix}/Observability__ApiKey"
  description = "Coralogix / OTLP API key (set via sync-env-to-ssm.ps1)"
  type        = "SecureString"
  value       = "PLACEHOLDER_SYNC_FROM_ENV"
  overwrite   = true

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_iam_role_policy" "ecs_api_ssm_read" {
  name = "${local.name_prefix}-ecs-api-ssm-read"
  role = aws_iam_role.ecs_api_task.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ssm:GetParameters",
          "ssm:GetParameter",
          "ssm:GetParametersByPath",
        ]
        Resource = "arn:aws:ssm:${data.aws_region.current.name}:${local.account_id}:parameter${local.ssm_secrets_prefix}*"
      },
      {
        Effect = "Allow"
        Action = [
          "kms:Decrypt",
        ]
        Resource = "*"
        Condition = {
          StringEquals = {
            "kms:ViaService" = "ssm.${data.aws_region.current.name}.amazonaws.com"
          }
        }
      }
    ]
  })
}

resource "aws_ssm_parameter" "yarp_reverse_proxy" {
  name        = "${local.ssm_secrets_prefix}/yarp-reverse-proxy-json"
  description = "YARP Routes/Clusters JSON (Gateway:SsmReverseProxyParameter loads raw or ReverseProxy-wrapped)"
  type        = "String"
  value = jsonencode({
    Routes = {
      api = {
        ClusterId = "api"
        Match = {
          Path = "{**catch-all}"
        }
      }
    }
    Clusters = {
      api = {
        Destinations = {
          d1 = {
            Address = "${local.api_internal_base_url}/"
          }
        }
      }
    }
  })
}

resource "aws_iam_role_policy" "ecs_gateway_ssm_read" {
  name = "${local.name_prefix}-ecs-gw-ssm-read"
  role = aws_iam_role.ecs_gateway_task.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ssm:GetParameter",
          "ssm:GetParameters",
          "ssm:GetParametersByPath",
        ]
        Resource = "arn:aws:ssm:${data.aws_region.current.name}:${local.account_id}:parameter${local.ssm_secrets_prefix}*"
      },
      {
        Effect = "Allow"
        Action = [
          "kms:Decrypt",
        ]
        Resource = "*"
        Condition = {
          StringEquals = {
            "kms:ViaService" = "ssm.${data.aws_region.current.name}.amazonaws.com"
          }
        }
      }
    ]
  })
}
