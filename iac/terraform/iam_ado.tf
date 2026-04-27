# Azure DevOps -> AWS OIDC trust + IAM role.
# ADO workload identity federation issuer: https://vstoken.dev.azure.com/<org-id>
# See: https://learn.microsoft.com/en-us/azure/devops/pipelines/release/configure-workload-identity
# Mirrors iam_github.tf so phase 2 (ADO) can deploy to the same AWS resources without long-lived keys.

variable "ado_organization_url" {
  type        = string
  description = "Azure DevOps organization URL (e.g. https://dev.azure.com/ECI-LBMH)"
  default     = "https://dev.azure.com/ECI-LBMH"
}

variable "ado_organization_id" {
  type        = string
  description = <<EOT
Azure DevOps organization GUID, used in the OIDC issuer URL https://vstoken.dev.azure.com/<id>.
Get it with: az devops invoke --area Profile --resource Profiles --route-parameters id=me --api-version 5.0
or via the ADO REST: https://dev.azure.com/<org>/_apis/connectionData
EOT
  default     = ""
}

variable "ado_project" {
  type        = string
  description = "Azure DevOps project name"
  default     = "LBMH-POC"
}

variable "ado_service_connection_id" {
  type        = string
  description = "ADO service connection (workload identity) GUID for AWS deploys"
  default     = ""
}

locals {
  ado_oidc_issuer  = var.ado_organization_id == "" ? "" : "https://vstoken.dev.azure.com/${var.ado_organization_id}"
  ado_oidc_enabled = var.ado_organization_id != "" && var.ado_service_connection_id != ""
}

data "tls_certificate" "ado" {
  count = local.ado_oidc_enabled ? 1 : 0
  url   = local.ado_oidc_issuer
}

resource "aws_iam_openid_connect_provider" "ado" {
  count = local.ado_oidc_enabled ? 1 : 0
  url   = local.ado_oidc_issuer

  client_id_list = [
    "api://AzureADTokenExchange",
  ]

  thumbprint_list = [data.tls_certificate.ado[0].certificates[0].sha1_fingerprint]
}

resource "aws_iam_role" "ado_pipelines" {
  count = local.ado_oidc_enabled ? 1 : 0
  name  = "${local.name_prefix}-ado-pipelines"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = "sts:AssumeRoleWithWebIdentity"
        Principal = {
          Federated = aws_iam_openid_connect_provider.ado[0].arn
        }
        Condition = {
          StringEquals = {
            "vstoken.dev.azure.com:aud" = "api://AzureADTokenExchange"
            "vstoken.dev.azure.com:sub" = "sc://${var.ado_organization_url}/${var.ado_project}/${var.ado_service_connection_id}"
          }
        }
      }
    ]
  })
}

resource "aws_iam_role_policy" "ado_pipelines_deploy" {
  count = local.ado_oidc_enabled ? 1 : 0
  name  = "${local.name_prefix}-ado-deploy"
  role  = aws_iam_role.ado_pipelines[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ecr:GetAuthorizationToken",
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:BatchGetImage",
          "ecr:PutImage",
          "ecr:InitiateLayerUpload",
          "ecr:UploadLayerPart",
          "ecr:CompleteLayerUpload",
        ]
        Resource = [
          aws_ecr_repository.api.arn,
          "${aws_ecr_repository.api.arn}/*",
          aws_ecr_repository.gateway.arn,
          "${aws_ecr_repository.gateway.arn}/*",
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket",
        ]
        Resource = [
          aws_s3_bucket.web.arn,
          "${aws_s3_bucket.web.arn}/*",
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "cloudfront:CreateInvalidation",
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "ecs:DescribeServices",
          "ecs:UpdateService",
          "ecs:DescribeTaskDefinition",
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "iam:PassRole",
        ]
        Resource = [
          aws_iam_role.ecs_execution.arn,
          aws_iam_role.ecs_api_task.arn,
          aws_iam_role.ecs_gateway_task.arn,
        ]
      }
    ]
  })
}

output "ado_pipelines_role_arn" {
  value       = local.ado_oidc_enabled ? aws_iam_role.ado_pipelines[0].arn : ""
  description = "ARN to wire into the ADO AWS service connection (workload identity)."
}
