---
name: deploy-lbmh-poc-todo-cdk
description: >-
  Deploys the LBMH POC Spruce Next Todo CDK stack (TodoSuiteInfra) to AWS:
  ECS Fargate + ALB APIs, S3 UI bucket, existing VPC. Covers SSO/profile,
  VPC context, cdk deploy, stuck deletes, and cdk.out locks. Use when deploying
  or redeploying TodoSuiteInfra, LBMH.POC.Spruce.Next.Todo Iac, CDK todo suite
  infrastructure, or when the user asks to deploy the POC Todo stack to AWS.
---

# Deploy LBMH POC Todo CDK stack (AWS)

## Scope

- **App / stack**: AWS CDK app in the **LBMH.POC.Spruce.Next.Todo** solution; CloudFormation stack **`TodoSuiteInfra`**.
- **Project directory** (from repo root): `iac/LBMH.POC.Spruce.Next.Todo.Iac/`
- **Region**: Default **`us-east-1`** unless `CDK_DEFAULT_REGION` is set (`Program.cs`).
- **Account**: From `CDK_DEFAULT_ACCOUNT` when set; otherwise CDK resolves from credentials.

## Prerequisites

- AWS CLI v2, **AWS CDK CLI** (`cdk`), **.NET SDK** (project targets `net10.0`).
- **Credentials**: IAM or **AWS SSO**. Example profile name used in this workflow: **`standard`**.
- VPC ID available for **`Vpc.fromLookup`** (same account/region). Repo ships **`cdk.json`** `context.todoSuiteVpcId`; optional script `scripts/pick-vpc-for-todo-cdk.ps1` at solution root.

## Before deploy

1. **Sign in** (if SSO):
   ```bash
   aws sso login --profile standard
   ```
2. **Verify identity**:
   ```bash
   aws sts get-caller-identity --profile standard
   ```
3. **VPC context** — one of:
   - `cdk.json` → `"todoSuiteVpcId": "vpc-..."`  
   - Env: `TODO_SUITE_VPC_ID=vpc-...`  
   - CLI: `-c todoSuiteVpcId=vpc-...`
4. **Working directory**:
   ```bash
   cd iac/LBMH.POC.Spruce.Next.Todo.Iac
   ```

## Deploy

Set profile for the session, then deploy the single stack:

```bash
export AWS_PROFILE=standard   # Linux/macOS
# PowerShell: $env:AWS_PROFILE = "standard"

cdk deploy TodoSuiteInfra --require-approval never
```

Optional sanity check:

```bash
cdk synth TodoSuiteInfra --quiet
```

### If `cdk.out` is locked

Another CDK process may hold the default output dir. Use a dedicated output:

```bash
cdk deploy TodoSuiteInfra --require-approval never --output cdk.out.deploy
```

### If CloudFormation says stack is `DELETE_IN_PROGRESS`

Wait until the stack is **gone** (`describe-stacks` returns “does not exist”), then run **`cdk deploy`** again. Do not update a stack that is still deleting.

## After deploy

- **Outputs** include **`TodoApiLoadBalancerDns`**, **`ProjectApiLoadBalancerDns`**, **`UiBucketName`**, **`ClusterName`**, **`VpcIdUsed`** (and CDK-generated duplicates for URLs).
- **ALB health**: Target groups use **port 8080**, health check **`GET /`**. Placeholder image **`hello-app-runner`** must have **`PORT=8080`** in the task definition (already wired in `TodoSuiteInfraStack.cs`) so the process listens on 8080.
- Quick check: HTTP **200** on `http://{TodoApiLoadBalancerDns}/` and the Project API DNS.

## Troubleshooting

| Symptom | Action |
|--------|--------|
| No credentials / SSO expired | `aws sso login --profile standard` |
| Wrong VPC / lookup fails | Fix `todoSuiteVpcId` in `cdk.json` or `-c` / env; VPC must exist in deploy region |
| Targets unhealthy | Confirm TG port **8080**, path **`/`**, task **`PORT=8080`** for hello-app-runner; for .NET images, **no HTTPS redirection** behind HTTP ALB and a **200** on `/` (or align health path) |

## Related paths (repo root = LBMH.POC.Spruce.Next.Todo)

| Path | Purpose |
|------|---------|
| `iac/LBMH.POC.Spruce.Next.Todo.Iac/TodoSuiteInfraStack.cs` | Stack: cluster, two ALB+Fargate services, S3 bucket, TG port + health |
| `iac/LBMH.POC.Spruce.Next.Todo.Iac/cdk.json` | `app`, `context.todoSuiteVpcId` |
| `iac/LBMH.POC.Spruce.Next.Todo.Iac/Program.cs` | Stack id `TodoSuiteInfra`, env + VPC resolution |
| `scripts/pick-vpc-for-todo-cdk.ps1` | Pick VPC id for context |
