resource "random_password" "db" {
  length  = 24
  special = false
}

resource "aws_db_subnet_group" "main" {
  name       = "${local.name_prefix}-db"
  subnet_ids = data.aws_subnets.vpc.ids
}

resource "aws_security_group" "aurora" {
  name        = "${local.name_prefix}-aurora"
  description = "Aurora for PipelineEval API"
  vpc_id      = data.aws_vpc.main.id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_rds_cluster" "main" {
  cluster_identifier = "${local.name_prefix}-aurora"

  engine         = "aurora-postgresql"
  engine_mode    = "provisioned"
  engine_version = "16.4"

  database_name   = "pipelineeval"
  master_username = "todoapp"
  master_password = random_password.db.result

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.aurora.id]

  skip_final_snapshot = true
  storage_encrypted   = true

  serverlessv2_scaling_configuration {
    min_capacity = 0.5
    max_capacity = 4
  }
}

resource "aws_rds_cluster_instance" "main" {
  identifier         = "${local.name_prefix}-aurora-1"
  cluster_identifier = aws_rds_cluster.main.id
  instance_class     = "db.serverless"
  engine             = aws_rds_cluster.main.engine
  engine_version     = aws_rds_cluster.main.engine_version
}

locals {
  db_connection_string = format(
    "Host=%s;Port=%s;Database=%s;Username=%s;Password=%s",
    aws_rds_cluster.main.endpoint,
    aws_rds_cluster.main.port,
    aws_rds_cluster.main.database_name,
    aws_rds_cluster.main.master_username,
    random_password.db.result
  )
}
