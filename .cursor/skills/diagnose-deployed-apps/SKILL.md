---
name: diagnose-deployed-apps
description: >-
  Diagnoses connectivity and runtime failures in deployed AWS workloads (ECS, logs, databases, secrets).
  Primary: SDET (owns deployed smoke + observability); DevOps for post-deploy triage. Use when UI or API health fails in prod-like envs.
---

# Diagnose deployed applications

Diagnoses failures in **deployed** environments (example: **us-east-1**, account from your CDK/infra). Replace placeholders: **{{ECS_CLUSTER_NAME}}**, **{{ECS_SERVICE_NAME}}**, **{{LOG_GROUP_NAME}}**, **{{AURORA_CLUSTER_ID}}**, **{{API_HEALTH_URL}}**, **{{UI_BASE_URL}}**.

## Prerequisites for evaluation-report evidence

For phase closure, use **deployed** evidence **after** DevOps confirms deploy validation green and URLs match that run. Do not mix localhost metrics with production closure. See the deploy-window observability section of [`.cursor/skills/sdet-phase-evidence/SKILL.md`](../sdet-phase-evidence/SKILL.md).

## Prerequisites (CLI)

- AWS CLI v2 with a profile that has read access to the target account.

## Quick health check

```bash
curl -s -o /dev/null -w "%{http_code}" "{{API_HEALTH_URL}}"
curl -s -o /dev/null -w '%{http_code}\n' '{{UI_BASE_URL}}/'
```

## Decision tree (high level)

```
UI shows error / no data
├── API /health non-200? → ECS/tasks, ALB/target groups, logs
├── API 200 but UI broken? → Frontend routing, env, CORS, double /api prefix
├── Tasks crash-looping? → DB, secrets, security groups, migrations
└── Intermittent 5xx? → cold start, timeouts, downstream dependency
```

## ECS (example)

```bash
aws ecs describe-services \
  --cluster {{ECS_CLUSTER_NAME}} \
  --services {{ECS_SERVICE_NAME}} \
  --query 'services[0].events[:10]' \
  --region us-east-1
```

## CloudWatch logs (example)

```bash
aws logs filter-log-events \
  --log-group-name {{LOG_GROUP_NAME}} \
  --start-time $(( $(date +%s) - 1800 ))000 \
  --filter-pattern "ERROR" \
  --region us-east-1
```

## Force new deployment (example)

```bash
aws ecs update-service \
  --cluster {{ECS_CLUSTER_NAME}} \
  --service {{ECS_SERVICE_NAME}} \
  --force-new-deployment \
  --region us-east-1
```

Adjust commands for EKS, App Service, or other runtimes your project uses.
