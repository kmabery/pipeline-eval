resource "aws_ecs_cluster" "main" {
  name = "${local.name_prefix}-cluster"
}

resource "aws_cloudwatch_log_group" "gateway" {
  name              = "/ecs/${local.name_prefix}-gateway"
  retention_in_days = 7
}

resource "aws_cloudwatch_log_group" "api" {
  name              = "/ecs/${local.name_prefix}-api"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "gateway" {
  family                   = "${local.name_prefix}-gateway"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_gateway_task.arn

  container_definitions = jsonencode([{
    name  = "gateway"
    image = "${aws_ecr_repository.gateway.repository_url}:latest"
    portMappings = [{
      containerPort = 8080
      protocol      = "tcp"
    }]
    essential = true
    environment = [
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      { name = "ASPNETCORE_URLS", value = "http://+:8080" },
      { name = "Gateway__SsmReverseProxyParameter", value = aws_ssm_parameter.yarp_reverse_proxy.name },
      { name = "Aws__SsmParameterPrefix", value = local.ssm_secrets_prefix },
      { name = "Observability__UseLocal", value = "false" },
      { name = "Observability__OtlpEndpoint", value = "https://ingress.us2.coralogix.com:443" },
      # application/subsystem aligned with .env so a single Coralogix DataPrime filter
      # ($l.applicationname=='spruce-next' && $l.subsystemname=='PipelineEval') covers both
      # local and deployed; service.name distinguishes Api vs Gateway in tracing waterfalls.
      { name = "Observability__ApplicationName", value = "spruce-next" },
      { name = "Observability__SubSystem", value = "PipelineEval" },
      { name = "Observability__ServiceName", value = "PipelineEval.Gateway" },
    ]
    # ECS injects this as the env var Observability__ApiKey; the .NET env-var configuration
    # provider then translates __ to : automatically, so it lands as Observability:ApiKey
    # in IConfiguration. The library also has an in-code literal-key fallback for the
    # AWS Systems Manager configuration provider path.
    secrets = [
      { name = "Observability__ApiKey", valueFrom = aws_ssm_parameter.observability_api_key.arn },
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.gateway.name
        "awslogs-region"        = data.aws_region.current.name
        "awslogs-stream-prefix" = "gateway"
      }
    }
  }])
}

resource "aws_ecs_task_definition" "api" {
  family                   = "${local.name_prefix}-api"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "1024"
  memory                   = "2048"
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_api_task.arn

  container_definitions = jsonencode([{
    name  = "api"
    image = "${aws_ecr_repository.api.repository_url}:latest"
    portMappings = [{
      containerPort = 8080
      protocol      = "tcp"
    }]
    essential = true
    environment = [
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      { name = "ASPNETCORE_URLS", value = "http://+:8080" },
      { name = "Aws__SsmParameterPrefix", value = local.ssm_secrets_prefix },
      { name = "Observability__UseLocal", value = "false" },
      { name = "Observability__OtlpEndpoint", value = "https://ingress.us2.coralogix.com:443" },
      # application/subsystem aligned with .env so a single Coralogix DataPrime filter
      # covers Api + Gateway; service.name (PipelineEval.Api here) distinguishes them.
      { name = "Observability__ApplicationName", value = "spruce-next" },
      { name = "Observability__SubSystem", value = "PipelineEval" },
      { name = "Observability__ServiceName", value = "PipelineEval.Api" },
      { name = "S3__BucketName", value = aws_s3_bucket.cat_uploads.bucket },
      { name = "S3__Region", value = data.aws_region.current.name },
      { name = "S3__KeyPrefix", value = "cats" },
      { name = "Authentication__RequireAuthentication", value = "true" },
      { name = "Cognito__Region", value = data.aws_region.current.name },
      { name = "Cognito__UserPoolId", value = aws_cognito_user_pool.main.id },
      { name = "Cognito__ClientId", value = aws_cognito_user_pool_client.spa.id },
      { name = "Cors__Origins__0", value = "https://${aws_cloudfront_distribution.web.domain_name}" },
    ]
    secrets = [
      { name = "Observability__ApiKey", valueFrom = aws_ssm_parameter.observability_api_key.arn },
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.api.name
        "awslogs-region"        = data.aws_region.current.name
        "awslogs-stream-prefix" = "api"
      }
    }
  }])
}

resource "aws_ecs_service" "api" {
  name            = "${local.name_prefix}-api"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = data.aws_subnets.vpc.ids
    security_groups  = [aws_security_group.ecs_api.id]
    assign_public_ip = true
  }

  service_registries {
    registry_arn = aws_service_discovery_service.api.arn
  }

  depends_on = [aws_service_discovery_service.api]
}

resource "aws_ecs_service" "gateway" {
  name            = "${local.name_prefix}-gateway"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.gateway.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  load_balancer {
    target_group_arn = aws_lb_target_group.gateway.arn
    container_name   = "gateway"
    container_port   = 8080
  }

  network_configuration {
    subnets          = data.aws_subnets.vpc.ids
    security_groups  = [aws_security_group.ecs_gateway.id]
    assign_public_ip = true
  }

  depends_on = [aws_lb_listener.http]
}
