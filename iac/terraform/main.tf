# Root module: networking, data, S3, ECR, Aurora, ALB, ECS, CloudFront, IAM, Cognito, SSM — see *.tf files.

resource "random_id" "suffix" {
  byte_length = 4
}
