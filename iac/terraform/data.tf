data "aws_caller_identity" "current" {}

data "aws_region" "current" {}

data "aws_vpc" "main" {
  id = var.vpc_id
}

data "aws_subnets" "vpc" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.main.id]
  }
}

# Internet-facing ALB must sit in subnets with a path to the internet (typically
# map-public-ip-on-launch). Pick one subnet per AZ from the public set only.
data "aws_subnets" "public" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.main.id]
  }
  filter {
    name   = "map-public-ip-on-launch"
    values = ["true"]
  }
}

data "aws_subnet" "public_subnet" {
  for_each = toset(data.aws_subnets.public.ids)
  id       = each.value
}

locals {
  account_id         = data.aws_caller_identity.current.account_id
  project_slug       = coalesce(var.project_name, var.evaluation_id)
  name_prefix        = "${var.evaluation_id}-${var.environment}"
  ssm_secrets_prefix = "/${var.evaluation_id}/${var.environment}/secrets"
  public_subnets_by_az = {
    for id, sn in data.aws_subnet.public_subnet :
    sn.availability_zone => id...
  }
  alb_subnet_ids = [
    for az in sort(keys(local.public_subnets_by_az)) :
    local.public_subnets_by_az[az][0]
  ]
}
