---
name: phase-gate-self-heal
description: >-
  When phase evidence shows blocked DevOps, SDET deployed, or observability gaps, run GitHub CLI and AWS CLI
  diagnostics, fix auth or config, retry; then update phase-MM-dd-yyyy-HHmmss-<n>.json and evaluation-report. Use before
  recording phaseGateOutcome failed_incomplete.
---

# Phase gate self-heal (GitHub + AWS)

Use whenever `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` has **null** `devops.githubActions.runId`, **`blocked`** deployed SDET, or missing deploy-window observability refs (`observability.buildOrRunRef` / `observability.metricsOrGap`; legacy `sre.*` accepted for historical sidecars) — **before** accepting **failed_incomplete** as final.

## Order

1. **Git / repo** — Commands must run from a **full clone** with `.git` (not a partial copy). If missing: clone `git@github.com:kmabery/pipeline-eval.git` per [`.cursor/rules/github-remote-ssh.mdc`](../../rules/github-remote-ssh.mdc).

2. **GitHub CLI**

   ```powershell
   gh auth status
   gh auth login
   gh run list -R kmabery/pipeline-eval --workflow=ci-cd-deploy.yml -L 5
   gh run view <RUN_ID> -R kmabery/pipeline-eval
   ```

   If workflows fail: open `gh run view <id> --log-failed`, fix, push to `main`, re-run per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md).

3. **AWS** (deployed URLs, App Runner, logs) — **mandatory sso-login-retry loop; do not stop at auth.** The agent itself runs these commands; only after `aws sso login` itself fails do you surface a resumable prompt to the user. See [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md) §4c.

   ```powershell
   aws sts get-caller-identity
   # On SSO/token error (e.g. "Token for default does not exist"):
   aws sso login
   if ($env:AWS_PROFILE) { aws sso login --profile $env:AWS_PROFILE }
   aws sts get-caller-identity   # retry
   ```

   `selfHealAttempts` in `phase-MM-dd-yyyy-HHmmss-<n>.json` must record the retry, not just the initial `sts` failure.

   From [`iac/terraform`](../../../iac/terraform) (after SSO):

   **Validate Terraform CLI first** — `terraform version` must exit 0 (meet **`required_version`** in [`versions.tf`](../../../iac/terraform/versions.tf)). If missing, install (e.g. `winget install HashiCorp.Terraform`), fix `PATH`, re-run `terraform version`. Optionally `terraform -help`.

   ```powershell
   cd iac/terraform
   terraform init
   terraform output -json
   terraform plan
   ```

   Use **`terraform apply`** only when fixing **documented drift** and the plan matches the intended fix (see [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md) §4a). After `terraform init`, confirm providers/backend load before relying on **`output`** or **`plan`** output.

   Map outputs to **`manualVerificationUrls`**: `apprunner_service_url`, `cloudfront_domain_name` (prefix `https://`) per [`outputs.tf`](../../../iac/terraform/outputs.tf).

4. **Post-deploy verify** — With URLs, run checks [`.cursor/skills/post-deploy-verify/SKILL.md`](../post-deploy-verify/SKILL.md); update SDET deployed fields.

   **Order reminder:** GitHub diagnostics → AWS auth → **validate Terraform CLI** → `terraform output` / **`plan`** (and **`apply`** if warranted) → retry **`ci-cd-deploy.yml`** per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md).

5. **SDET (observability)** — Attach deploy-window metrics / traces / logs (or explicit documented gap) to the **same** run ID per the deploy-window section of [`.cursor/skills/sdet-phase-evidence/SKILL.md`](../sdet-phase-evidence/SKILL.md).

## When to stop self-heal

- **Succeeded** `gh run view` + populated JSON + evaluation-report — gate can move to **passed** for DevOps.
- **`phaseGateOutcome`: `failed_incomplete`** with **"SSO token missing"** (or any variant of "could not authenticate to AWS") as the **sole** reason is **not acceptable**. Before recording `failed_incomplete`, the agent must have:
  1. Attempted `aws sso login` (and `aws sso login --profile $AWS_PROFILE` if set) and re-run `aws sts get-caller-identity`.
  2. If `aws sso login` itself failed, surfaced a resumable prompt to the user and waited for them to complete the SSO browser flow before resuming from `sts`.
  3. Run the full §4a Terraform loop (`terraform init`/`plan`/`apply`) in [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md).
  4. If Terraform could not run, attempted the agent-initiated §4b step 4 emergency `aws iam put-role-policy` path with diff review.
- Only after the full loop above ran and a **downstream** failure (terraform plan error, real `AccessDenied` on `put-role-policy`, non-drift app bug, etc.) persisted may you record `failed_incomplete` — and `reasons[]` in the phase JSON must name that downstream failure, with `selfHealAttempts` showing the commands/timestamps/stderr that demonstrate the loop ran. Per [`.cursor/rules/phase-gate-outcomes.mdc`](../../rules/phase-gate-outcomes.mdc).
