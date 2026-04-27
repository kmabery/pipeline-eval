variable "evaluation_id" {
  type        = string
  description = "Unique slug per evaluation (drives CloudFront comment, Aurora cluster id, ECS cluster/services, ALB, S3, ECR, IAM, SSM prefixes). Set in the per-evaluation terraform.tfvars by the eval scaffolder."
}

variable "aws_region" {
  type        = string
  description = "AWS region"
  default     = "us-east-1"
}

variable "vpc_id" {
  type        = string
  description = "Existing VPC ID to use (console format vpc-...; must have subnets in var.aws_region for Aurora, ECS, and ALB)"
}

variable "project_name" {
  type        = string
  description = "Short human-readable project name (defaults to evaluation_id when omitted)"
  default     = ""
}

variable "environment" {
  type        = string
  description = "Environment label (e.g. dev, prod, eval)"
  default     = "eval"
}

variable "cat_bucket_cors_origins" {
  type        = string
  description = "Comma-separated browser origins allowed for presigned S3 uploads (e.g. https://app.example.com,http://localhost:5173)"
  default     = "http://localhost:5173,http://localhost:5101"
}

variable "github_org" {
  type        = string
  description = "GitHub org or user for OIDC trust"
}

variable "github_repo" {
  type        = string
  description = "GitHub repository name for OIDC trust"
}

variable "github_branches" {
  type        = list(string)
  description = "Full ref paths allowed for OIDC sub claim (e.g. refs/heads/main)"
  default     = ["refs/heads/main"]
}

variable "github_environments" {
  type        = list(string)
  description = "GitHub Environment names allowed for OIDC sub claim (matches reusable-cd-deploy environment:)"
  default     = ["production"]
}
