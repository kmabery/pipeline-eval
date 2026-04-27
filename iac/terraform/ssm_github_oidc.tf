# Allow GitHub Actions (OIDC role) to upsert SSM secrets from .env via scripts/sync-env-to-ssm.ps1

resource "aws_iam_role_policy" "github_actions_ssm" {
  name = "${local.name_prefix}-github-ssm-secrets"
  role = aws_iam_role.github_actions.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ssm:PutParameter",
          "ssm:DeleteParameter",
          "ssm:DescribeParameters",
          "ssm:GetParameter",
          "ssm:GetParameters",
          "ssm:GetParametersByPath",
          "ssm:AddTagsToResource",
          "ssm:ListTagsForResource",
        ]
        Resource = "arn:aws:ssm:${data.aws_region.current.name}:${local.account_id}:parameter${local.ssm_secrets_prefix}*"
      },
      {
        Effect   = "Allow"
        Action   = ["kms:Encrypt", "kms:Decrypt", "kms:DescribeKey"]
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
