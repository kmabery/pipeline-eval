---
name: post-deploy-verify
description: >-
  Post-deploy checklist: workload steady state, logs, load balancer health, API smoke, E2E deployed smoke,
  CORS. Primary: DevOps; SDET for deployed smoke. Use after infra deploy, rolling restart, or CI deploy stage.
---

# Post-deploy verification and validation

After any **infrastructure deploy**, **rolling restart**, or **CI deploy stage** completes, run checks **in order** before declaring success. Replace placeholders with values from your CDK/terraform outputs or pipeline logs.

**Shell:** Commands below use bash-style examples; on Windows use **Git Bash**, **WSL**, or PowerShell equivalents.

## 1. Workload steady state (example: ECS)

```bash
aws ecs describe-services \
  --cluster {{ECS_CLUSTER_NAME}} \
  --services {{ECS_SERVICE_NAME}} \
  --query 'services[0].{desired:desiredCount,running:runningCount,status:status,deployments:deployments[*].{id:id,status:status,running:runningCount,desired:desiredCount}}' \
  --region us-east-1
```

- `runningCount` must equal `desiredCount` where applicable.
- Crash-looping tasks: inspect `stoppedReason` via `describe-tasks`.

## 2. Startup logs

```bash
aws logs filter-log-events \
  --log-group-name {{LOG_GROUP_NAME}} \
  --start-time $(( $(date +%s) - 600 ))000 \
  --region us-east-1 | head -50
```

PowerShell:

```powershell
$startTimeMs = [DateTimeOffset]::UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds()
aws logs filter-log-events `
  --log-group-name {{LOG_GROUP_NAME}} `
  --start-time $startTimeMs `
  --region us-east-1 |
  Select-Object -First 50
```

## 3. Load balancer target health

```bash
aws elbv2 describe-target-health \
  --target-group-arn {{TARGET_GROUP_ARN}} \
  --region us-east-1
```

## 4. API smoke (before UI E2E)

Do **not** run full Playwright smoke until the API is healthy.

```bash
curl -sf {{API_HEALTH_URL}}
curl -sf {{API_SAMPLE_URL}}
```

## 5. Deployed E2E smoke

From your E2E package, set base URLs from pipeline outputs, then run your **deployed smoke** config (e.g. `npx playwright test --config=playwright.deployed-smoke.config.ts`).

Verify at minimum:

- UI root loads
- API health responds
- Critical user path works against **real** backend (not localhost)

## 6. CORS

If the browser shows CORS errors: verify server CORS config, API gateway CORS, and that the UI targets the correct API base URL (no double `/api`, no localhost in prod).

## 7. On failure

Capture CLI output and logs. Triage with [`.cursor/skills/diagnose-deployed-apps/SKILL.md`](../diagnose-deployed-apps/SKILL.md). Fix, redeploy, re-run this checklist.
