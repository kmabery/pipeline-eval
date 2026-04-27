output "cat_uploads_bucket_name" {
  value       = aws_s3_bucket.cat_uploads.bucket
  description = "Private S3 bucket for cat image objects (configure API S3:BucketName)"
}

output "web_bucket_name" {
  value       = aws_s3_bucket.web.bucket
  description = "S3 bucket for static web assets (sync dist/)"
}

output "cloudfront_distribution_id" {
  value       = aws_cloudfront_distribution.web.id
  description = "CloudFront distribution ID for cache invalidation"
}

output "cloudfront_domain_name" {
  value       = aws_cloudfront_distribution.web.domain_name
  description = "HTTPS URL host for the SPA (https://DOMAIN)"
}

output "ecr_repository_url" {
  value       = aws_ecr_repository.api.repository_url
  description = "ECR repository URL for the API image"
}

output "ecr_gateway_repository_url" {
  value       = aws_ecr_repository.gateway.repository_url
  description = "ECR repository URL for the YARP gateway image"
}

output "ecr_repository_name" {
  value       = aws_ecr_repository.api.name
  description = "ECR API repository name (GitHub variable ECR_REPOSITORY_NAME)"
}

output "ecr_gateway_repository_name" {
  value       = aws_ecr_repository.gateway.name
  description = "ECR gateway repository name (GitHub variable ECR_GATEWAY_REPOSITORY_NAME)"
}

output "alb_dns_name" {
  value       = aws_lb.main.dns_name
  description = "Public ALB DNS (YARP gateway)"
}

output "public_app_url" {
  value       = "https://${aws_cloudfront_distribution.web.domain_name}"
  description = "Browser URL for SPA; use same host with /api/* for API via gateway"
}

output "api_url_for_vite" {
  value       = "https://${aws_cloudfront_distribution.web.domain_name}"
  description = "Set VITE_API_URL to this value (paths use /api/... and /health)"
}

output "ecs_cluster_name" {
  value       = aws_ecs_cluster.main.name
  description = "ECS cluster name (GitHub variable ECS_CLUSTER_NAME)"
}

output "ecs_service_api_name" {
  value       = aws_ecs_service.api.name
  description = "ECS API service name (GitHub variable ECS_SERVICE_API)"
}

output "ecs_service_gateway_name" {
  value       = aws_ecs_service.gateway.name
  description = "ECS gateway service name (GitHub variable ECS_SERVICE_GATEWAY)"
}

output "database_connection_string_secret" {
  value       = local.db_connection_string
  sensitive   = true
  description = "Npgsql connection string (also stored in SSM; break-glass / local use)"
}

output "ssm_secrets_prefix" {
  value       = local.ssm_secrets_prefix
  description = "Prefix for SecureString parameters; set API Aws__SsmParameterPrefix and sync-env-to-ssm.ps1 -Prefix"
}

output "github_actions_role_arn" {
  value       = aws_iam_role.github_actions.arn
  description = "IAM role ARN for GitHub OIDC (set as AWS_ROLE_ARN in workflow)"
}

output "aurora_endpoint" {
  value       = aws_rds_cluster.main.endpoint
  description = "Aurora PostgreSQL writer endpoint"
}

output "cognito_user_pool_id" {
  value       = aws_cognito_user_pool.main.id
  description = "Cognito User Pool ID (VITE_COGNITO_USER_POOL_ID / Cognito__UserPoolId)"
}

output "cognito_user_pool_client_id" {
  value       = aws_cognito_user_pool_client.spa.id
  description = "Cognito app client ID (VITE_COGNITO_CLIENT_ID / Cognito__ClientId)"
}

output "cognito_region" {
  value       = var.aws_region
  description = "Region for Cognito SDK / VITE_COGNITO_REGION"
}

output "yarp_ssm_parameter_name" {
  value       = aws_ssm_parameter.yarp_reverse_proxy.name
  description = "SSM parameter holding YARP Routes/Clusters JSON for the gateway"
}
