provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project      = local.project_slug
      EvaluationId = var.evaluation_id
      ManagedBy    = "terraform"
      Environment  = var.environment
    }
  }
}
