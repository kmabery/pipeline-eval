---
name: devops-github-actions-ci-aws
description: >-
  GitHub Actions + AWS preflight: trigger or monitor ci-cd-deploy.yml on git@github.com:kmabery/pipeline-eval.git,
  poll until completion, diagnose failures, route to Backend / Frontend / SDET, retry until green.
  Git over SSH. Use when github-actions is github-actions.
---

# GitHub Actions lifecycle — preflight, monitor, handoff, retry

Replace placeholders: **ci-cd-deploy.yml**, **{{GHA_WORKFLOW_FILE}}**, **kmabery**, **git@github.com:kmabery/pipeline-eval.git**, **main**, **pipeline-eval**. Workflows live under `.github/workflows/`; pipeline docs and webhook guidance live under **`pipelines/`** (`README.md`, `webhooks.md`). See [Workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax).

**Git:** **GitHub** remote — **`git@github.com:kmabery/pipeline-eval.git`** — per [`.cursor/rules/github-remote-ssh.mdc`](../../rules/github-remote-ssh.mdc).

**Webhooks:** Optional **`PIPELINE_MONITOR_WEBHOOK_URL`** drives **`pipeline-webhook-notify.yml`** (POST on workflow completion). Prefer webhooks for **async alerts**; when the user or phase gate requires **terminal CI evidence**, still **poll** per [`.cursor/rules/ci-pipeline-monitoring.mdc`](../../rules/ci-pipeline-monitoring.mdc). On failure, follow [`.cursor/cursor-orchestrator.md`](../../cursor-orchestrator.md) to delegate **backend-implementer** / **react-implementer** / **sdet-validator** with a Failure Handoff.

The DevOps agent’s job is **not** finished after a single `gh workflow run` — it **monitors** the run, and if anything fails, **diagnoses**, **delegates**, and **retries** after fixes until the workflow meets your team’s **definition of green**.

**Blocked CI / null run IDs in phase evidence:** Run [`.cursor/skills/phase-gate-self-heal/SKILL.md`](../phase-gate-self-heal/SKILL.md) (`gh auth status`, `gh run list`, AWS SSO, validate Terraform CLI, `terraform output`, `terraform plan`) before recording **`phaseGateOutcome`: `failed_incomplete`** in `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json`.

## Non-negotiable: required jobs

**All** jobs your team defines as mandatory for a “complete” run must **succeed** (adjust names to match your YAML).

A run with `conclusion` / status **failure** or skipped required jobs **does not** satisfy the **deployed-system gate** in `docs/phase-plan.md` (if used) or [`.cursor/cursor-orchestrator.md`](../../cursor-orchestrator.md).

## 1. Preflight (before triggering)

1. **GitHub CLI** — `gh auth status` (or ensure API access your team uses).
2. **AWS (mandatory retry loop — no optional steps):**

   ```bash
   aws sts get-caller-identity
   # On SSO/token error (e.g. "Token for default does not exist",
   # "The SSO session associated with this profile has expired"):
   aws sso login
   # If AWS_PROFILE is set, also:
   aws sso login --profile "$AWS_PROFILE"
   aws sts get-caller-identity   # retry
   ```

   Only if **`aws sso login` itself fails** (not `sts`) do you surface a resumable prompt to the user, wait for them to complete the SSO browser flow, and resume from `sts`. See **§4c — Do not stop at auth**.
3. **SDET local gate** — Do **not** treat CI as proof until SDET has recorded an all-green local handoff for the phase scope, or an explicit blocker the Analyst accepted.

Remediation: `gh auth login`, `aws sso login`. Do not commit secrets.

## 2. Queue / trigger the workflow

1. **Push** to **`main`** may already trigger `on:` **push** — confirm workflow filters (paths, branches).
2. **Manual dispatch:** If the workflow defines **`workflow_dispatch`**, use [Manually running a workflow](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow), e.g. `gh workflow run "ci-cd-deploy.yml" --ref main` (adjust flags to match your inputs).
3. **Preferred:** Read `gh` / REST tool schemas before calling MCP or automation.

Record **run ID** and **URL** (`gh run view <id> --web` or Actions tab) in `docs/evaluation-report.md` when orchestrating a phase.

## 2a. Commit and push before rerun (start of full cycle)

When the user asks to **run** or **rerun** a phase (or another phase pass), and the phase includes **deploy** or **full-cycle** evidence:

1. At the **beginning** of the pass, phase-owned fixes and any related docs are **committed** so nothing material is left uncommitted before local SDET.
2. Changes are **pushed** to **`main`** on **`git@github.com:kmabery/pipeline-eval.git`** (SSH) before you rely on **`ci-cd-deploy.yml`** as proof.
3. Do **not** assume CI ran on stale remote code, and do **not** treat a green run as validating uncommitted local work.

## 2b. Commit and push after deploy + evidence (end of full cycle)

This is the **second** bookend. It does **not** replace **§2a** (pre-pipeline commit). Run **after** the workflow run is **terminal** and the **Analyst** (or main orchestrator) has updated evidence for that window.

1. **When** — `ci-cd-deploy.yml` has reached **completed** (or **cancelled** with a recorded outcome), **and** `docs/phase-evidence/`, `docs/evaluation-reports/`, and/or `docs/evaluation-report.md` have been created or changed for this pass. Applies to **`phaseGateOutcome`: `passed`** and **`failed_incomplete`** (persist the record on the remote, not only locally). **Doc-only** phases: run this step after Analyst finalization; there is no deploy wait.
2. **What to stage** — at minimum: `docs/phase-evidence/`, `docs/evaluation-reports/`, `docs/evaluation-report.md` when touched. Do not bundle unrelated uncommitted product changes unless the phase requires them; prefer a focused **evidence** commit.
3. **Commit** — use a message that includes the program phase and, when known, the GitHub Actions run id, e.g. `docs(phase-1): evidence gha-24833863155`.
4. **Push** — to **`main`** on **`git@github.com:kmabery/pipeline-eval.git`** (see [`.cursor/rules/github-remote-ssh.mdc`](../../rules/github-remote-ssh.mdc)). Confirm `git status` is clean for those paths (or only acceptable follow-up work the Analyst documented). A push that only updates `docs/**` will **not** start `CI-CD-Deploy`; use **Run workflow** on that workflow in GitHub Actions if a full run is still required.

**Closure:** [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc) and [`.cursor/skills/analyst-evaluation-report-evidence/SKILL.md`](../analyst-evaluation-report-evidence/SKILL.md) require this push before **Complete** / **Verified** when structured or narrative evidence was produced.

## 2c. Stakeholder feedback triage — redeploy

When ad-hoc triage produces deployable changes (see `.cursor/rules/stakeholder-feedback-triage.mdc`):

1. After push, ensure a run completes for **`ci-cd-deploy.yml`** (or trigger per workflow design).
2. Capture **run ID** for the Analyst report.
3. **Monitor** per §3 when full green is expected.

## 3. Monitor until terminal state (mandatory)

1. Poll until the workflow run is **completed** (or **cancelled**).
2. Use **30–120s** intervals; see [`.cursor/rules/ci-pipeline-monitoring.mdc`](../../rules/ci-pipeline-monitoring.mdc).
3. Read **conclusion**: success | failure | cancelled | skipped (and neutral).

**Do not** stop after “workflow started.”

### Efficient monitoring (automation / agents)

For **Cursor agents** and other automation, **prefer JSON polling** over **`gh run watch`**. The default watch loop refreshes about **every 3 seconds**, which floods logs, burns context, and often **exceeds tool timeouts** before long jobs finish. That conflicts with the **30–120s** cadence in [`.cursor/rules/ci-pipeline-monitoring.mdc`](../../rules/ci-pipeline-monitoring.mdc).

1. After you have a **`RUN_ID`**, poll until `status` is terminal:

   ```bash
   gh run view <RUN_ID> --json status,conclusion,updatedAt,jobs
   ```

2. Wait **30–120 seconds** between polls. After the run has been **in progress for more than ~10 minutes**, you may stretch toward **2–3 minutes** between polls (same spirit as the rule). **Do not** stop monitoring before a terminal state.

3. Use **`jobs`** in the JSON to see which stage is active (e.g. `ci / build`, `deploy / deploy`, `Deployed smoke (Playwright)`) without streaming full logs.

4. Reserve **`gh run watch`** for **humans** who want a live tail, or very short runs, with awareness of session time limits. If you use watch in automation, expect **10–15+ minute** wall times for a full **CI-CD-Deploy** on this repo when deploy + smoke are included.

**Rough expected durations (this repo, `CI-CD-Deploy`):** **ci / build** often ~**3 minutes**; **deploy** (e.g. ECR, S3, CloudFront, App Runner) often ~**8–10 minutes**; **deployed smoke** (Playwright) often ~**1–2 minutes** when green. Total **~12–15+ minutes** is normal; failures can terminate earlier.

On **failure**, use `gh run view <RUN_ID> --log-failed` as in §4.

## 4. If the workflow fails — diagnose

1. Identify failing **job/step** from Actions UI or [workflow run logs](https://docs.github.com/en/actions/how-tos/monitor-workflows/use-workflow-run-logs).
2. Pull logs (`gh run view --log-failed` or UI).
3. Classify owner: **Backend**, **Frontend**, **SDET**, **DevOps**, or **Human** (environments/approvals).
4. Write a **Failure Handoff** markdown: run ID, job, error excerpt, suspected cause, assigned owner.

See [Re-running workflows and jobs](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/re-run-workflows-and-jobs) when appropriate after fixes land.

### 4a. IaC self-heal (Terraform drift in `iac/terraform`)

When logs show **AWS permission denials** or **missing resources** that match resources in **[`iac/terraform`](../../../iac/terraform)** (for example ECR `InitiateLayerUpload` denied on a repository defined in [`ecr.tf`](../../../iac/terraform/ecr.tf), while [`iam_github.tf`](../../../iac/terraform/iam_github.tf) already grants the GitHub Actions role access to both API and gateway repositories), treat this as **DevOps / Terraform alignment** before routing to Backend or Frontend.

1. **Validate Terraform CLI** — Run **`terraform version`** (must exit 0; satisfy **`required_version`** in [`versions.tf`](../../../iac/terraform/versions.tf)). If the command is missing, install Terraform (e.g. Windows: `winget install HashiCorp.Terraform`), ensure it is on `PATH`, then re-run **`terraform version`**. Optionally **`terraform -help`** to confirm the binary runs.

2. **AWS auth (mandatory retry loop — do not stop at auth):**

   ```bash
   aws sts get-caller-identity
   # On SSO/token error, the agent itself runs:
   aws sso login
   aws sso login --profile "$AWS_PROFILE"   # if AWS_PROFILE is set
   aws sts get-caller-identity              # retry
   ```

   Only if `aws sso login` itself fails do you pause and ask the user to complete the browser flow, then resume. See **§4c**.

3. From repo root:

   ```bash
   cd iac/terraform
   terraform init
   terraform plan
   ```

   **`terraform init`** may require backend credentials if you use remote state (see comments in [`versions.tf`](../../../iac/terraform/versions.tf)); do not commit secrets.

4. **Interpret** the plan (e.g. updates to `aws_iam_role_policy.github_actions_deploy` including gateway ECR ARNs per [`iam_github.tf`](../../../iac/terraform/iam_github.tf)).

5. **`terraform apply`** — Only after reviewing the plan; use interactive approval or **`-auto-approve`** per team policy. Prefer **plan output** before apply for safety.

6. **Retry CI** — Re-run the failed job or full workflow (`gh workflow run`, push to `main`, or **Re-run jobs** in the Actions UI), then **monitor** per §3 until terminal state.

If the failure is **not** explained by Terraform drift (application bug, test flake), continue with §5 and delegate to the right implementer.

### 4b. ECR `InitiateLayerUpload` denied for GitHub Actions OIDC role (pattern)

**Log shape:** `User: arn:aws:sts::<account>:assumed-role/<name_prefix>-github-actions/GitHubActions is not authorized to perform: ecr:InitiateLayerUpload on resource: arn:aws:ecr:<region>:<account>:repository/<name_prefix>-gateway` (API repo is a separate ARN).

**Root cause class:** Missing or stale **inline policy on the IAM role** [`aws_iam_role_policy.github_actions_deploy`](../../../iac/terraform/iam_github.tf) — *not* fixed by creating **IAM users** or static access keys for GitHub. OIDC must keep using the **assumed role** only.

**Fix order (mandatory):**

1. **Terraform first** — Complete §4a steps 1–6. The repo already declares `ecr:InitiateLayerUpload` (and related layer actions) on **both** `aws_ecr_repository.api` and `aws_ecr_repository.gateway` ARNs in `iam_github.tf`. If `terraform plan` shows updates to `aws_iam_role_policy.github_actions_deploy`, **apply** after review.

2. **AWS CLI diagnosis (read-only)** — After `aws sts get-caller-identity` confirms the correct account:

   ```bash
   aws iam list-role-policies --role-name <name_prefix>-github-actions
   aws iam get-role-policy --role-name <name_prefix>-github-actions --policy-name <name_prefix>-github-deploy
   ```

   Compare the returned `PolicyDocument` to [`iac/terraform/iam_github.tf`](../../../iac/terraform/iam_github.tf). If gateway repository ARNs or `ecr:InitiateLayerUpload` are missing in **live** AWS but present in `.tf`, drift is confirmed.

3. **Optional simulation** — `aws iam simulate-principal-policy` with `ActionNames` including `ecr:InitiateLayerUpload` and `ResourceArns` set to the gateway repository ARN to see `implicitDeny` vs `allowed`.

4. **Emergency CLI alignment (agent-initiated after plan review)** — When Terraform cannot be run (broken backend, state lock, wrong workspace), the agent itself runs this fallback under a strict review protocol:

   1. Extract the statements from `aws_iam_role_policy.github_actions_deploy` in [`iam_github.tf`](../../../iac/terraform/iam_github.tf).
   2. Compose a `PolicyDocument` JSON that **verbatim mirrors** those statements — same actions, same resource ARNs, least privilege. **Never** widen ECR data-plane actions to `Resource: "*"`.
   3. Print the review diff in the transcript using a fenced code block. Layout:

      ```text
      --- iac/terraform/iam_github.tf (aws_iam_role_policy.github_actions_deploy)
      +++ proposed aws iam put-role-policy document
      <unified diff showing statement-for-statement equivalence>
      ```

      Do **not** proceed if the diff shows added actions, widened resources, missing `Condition` blocks, or dropped statements.
   4. Apply:

      ```bash
      aws iam put-role-policy \
        --role-name <name_prefix>-github-actions \
        --policy-name <name_prefix>-github-deploy \
        --policy-document file://<generated>.json
      ```

   5. Log a TODO in `selfHealAttempts` (phase-evidence JSON) and/or `docs/evaluation-report.md` to reconcile Terraform state (import or re-apply) on the next maintenance window so `.tf` and live AWS stay aligned.
   6. Continue the self-heal loop: re-dispatch the workflow (`gh workflow run`), monitor per §3.

**Security:** Do not add **IAM users** for GitHub Actions; do not store **AWS access keys** in GitHub secrets for this flow when OIDC + role is already in use. Prefer SSO/session locally for `terraform apply` / `aws iam put-role-policy`.

### 4c. Do not stop at auth

A missing AWS SSO token is **not** a terminal state for this agent. The order is strict:

1. `aws sts get-caller-identity` fails → run `aws sso login` (and `aws sso login --profile $AWS_PROFILE` if set) → retry `sts`.
2. Only if **`aws sso login` itself** fails (not the initial `sts`) do you surface a resumable prompt to the user asking them to complete the SSO browser flow.
3. Once the user signals completion, the agent resumes from `sts` — it does **not** restart the phase or demote the run to `failed_incomplete`.
4. Recording `phaseGateOutcome: failed_incomplete` with "SSO token missing" or "could not run terraform" as the **sole** reason is **not acceptable**. The reason must be a downstream failure (terraform plan error, `put-role-policy` denial with a real AccessDenied, non-drift app bug, etc.) that persisted after the full §4a + §4b loop ran.

This section is the single source of truth referenced by [`.cursor/agents/devops-github-actions-operator.md`](../../agents/devops-github-actions-operator.md) ("Never stop at auth") and [`.cursor/skills/phase-gate-self-heal/SKILL.md`](../phase-gate-self-heal/SKILL.md).

## 5. Delegate to implementers

Route to the right subagent with the Failure Handoff and log pointers. Fix root cause before cosmetic test tweaks.

## 6. Retry after fixes

Commit, push, trigger or wait for a **new** run, monitor again. Repeat until **success** and required jobs are green — or until blocked by human-only approval (document in evaluation-report).

## 7. When the run succeeds

Copy **run ID**, **job summary**, artifact links, **deployed URLs** into `docs/evaluation-report.md`. Hand off to **SDET** with run ID and URLs for deployed smoke + deploy-window observability per [`.cursor/skills/sdet-phase-evidence/SKILL.md`](../sdet-phase-evidence/SKILL.md).

## 8. DevOps does not alone close the phase

Green deploy validation is **necessary** not **sufficient** for **Complete** / **Verified**: **SDET** (deployed smoke + observability) and **Analyst** must still align per [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc).

## 9. Preflight commands (examples)

```bash
gh auth status
aws sts get-caller-identity
terraform version
```
