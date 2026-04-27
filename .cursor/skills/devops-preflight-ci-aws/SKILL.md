---
name: devops-preflight-ci-aws
description: >-
  Azure Pipelines + AWS preflight: queue CI-Deploy-main in {{ADO_ORG}}/{{ADO_PROJECT}},
  monitor runs until completion, diagnose failures, route work to Backend / Frontend / SDET with
  concrete handoffs, and re-queue until required stages pass. Source repo is GitHub (SSH). Use when
  github-actions is azure-pipelines.
---

# Azure Pipelines lifecycle — preflight, monitor, handoff, retry

Replace placeholders: **CI-Deploy-main**, **{{ADO_ORG}}**, **{{ADO_PROJECT}}**, **pipeline-eval**, **kmabery**, **git@github.com:kmabery/pipeline-eval.git**, **main**. Your pipeline YAML path is typically `pipelines/azure-pipelines.yml` or similar (team-specific).

**Git:** Treat **GitHub** as the canonical remote — **`git@github.com:kmabery/pipeline-eval.git`** — per [`.cursor/rules/github-remote-ssh.mdc`](../../rules/github-remote-ssh.mdc). **{{ADO_ORG}}** / **{{ADO_PROJECT}}** identify only where **Azure Pipelines** runs, not where the repository is hosted.

The DevOps agent’s job is **not** finished after a single `az pipelines run` or one MCP call — it **monitors** the run, and if anything fails, **diagnoses**, **delegates**, and **retries** after fixes until the pipeline meets your team’s **definition of green**.

## Non-negotiable: required stages

**All** stages your team defines as mandatory for a “complete” run must **succeed** (adjust names to match your YAML):

Example: `Build` → `UnitTest` → `IntegrationTest` → `E2ETest` → `InfrastructureDeploy` → `Deploy` → `DeployedValidation`

A run with `result: failed` or skipped required stages **does not** satisfy the **deployed-system gate** in `docs/phase-plan.md` (if used) or [`.cursor/cursor-orchestrator.md`](../../cursor-orchestrator.md).

## 1. Preflight (before queueing)

1. **Azure DevOps CLI / API** — `az account show` or equivalent auth.
2. **AWS** (if CDK/deploy stages apply) — `aws sts get-caller-identity` when relevant.
3. **SDET local gate** — Do **not** queue until SDET has recorded an all-green local handoff for the phase scope, or an explicit blocker the Analyst accepted.

Remediation: `az login`, `aws sso login` in the **user’s** terminal. Do not commit secrets.

## 2. Queue the pipeline

1. **Preferred:** Your ADO MCP or automation — read tool schemas before calling (`pipelines_run_pipeline`, `pipelines_get_run`, etc.).
2. **Fallback:** `az pipelines run --name <name> --project {{ADO_PROJECT}} --organization https://dev.azure.com/{{ADO_ORG}}`

Record **build ID** and **URL** in `docs/evaluation-report.md` when orchestrating a phase.

## 2a. Commit and push before rerun

When the user asks to **run** or **rerun** a phase (or another phase pass):

1. Phase-owned fixes are **committed** before queueing.
2. Changes are **pushed** to **`main`** on the GitHub remote (**SSH**), or the branch your Azure Pipeline is configured to build if different — document which.
3. Do **not** rerun against stale remote code if the fix only exists locally.

## 2b. Commit and push after pipeline + evidence (end of full cycle)

Second bookend; does **not** replace **§2a**. After the pipeline has reached **terminal** state and **Analyst** (or the orchestrator) has updated `docs/phase-evidence/`, `docs/evaluation-reports/`, and `docs/evaluation-report.md` (when changed), **commit and push** those paths to the branch Azure builds from (typically **`main`**, SSH to **`git@github.com:kmabery/pipeline-eval.git`**). Use a message that includes **build id** and phase when known. **Doc-only** phases: commit after Analyst finalization, no deploy wait. **Passed** and **failed** deploy-cycle outcomes both get persisted to the remote.

**Closure:** [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc).

## 2c. Stakeholder feedback triage — redeploy

When ad-hoc triage produces deployable changes (see `.cursor/rules/stakeholder-feedback-triage.mdc`):

1. Queue **CI-Deploy-main** after push.
2. Capture **build ID** for the Analyst report.
3. **Monitor** per §3 when full green is expected.

## 3. Monitor until terminal state (mandatory)

1. Poll until `status` is `completed` (or `canceled`).
2. Use **30–120s** intervals; see [`.cursor/rules/ci-pipeline-monitoring.mdc`](../../rules/ci-pipeline-monitoring.mdc).
3. Read **`result`**: `succeeded` | `failed` | `canceled` | `partiallySucceeded`.

**Do not** stop after “run started.”

## 4. If the pipeline fails — diagnose

1. Identify failing **stage/job** from ADO or logs.
2. Pull logs (MCP or `az pipelines runs show` / ADO UI).
3. Classify owner: **Backend**, **Frontend**, **SDET**, **DevOps**, or **Human** (approvals).
4. Write a **Failure Handoff** markdown: build ID, stage, error excerpt, suspected cause, assigned owner.

## 5. Delegate to implementers

Route to the right subagent with the Failure Handoff and log pointers. Fix root cause before cosmetic test tweaks.

## 6. Retry after fixes

Commit, push, queue a **new** run, monitor again. Repeat until **`result: succeeded`** and required stages are green — or until blocked by human-only approval (document in evaluation-report).

## 7. When the run succeeds

Copy **build ID**, **stage summary**, artifact links, **deployed URLs** into `docs/evaluation-report.md`. Hand off to **SDET** with build ID and URLs for deployed smoke + deploy-window observability per [`.cursor/skills/sdet-phase-evidence/SKILL.md`](../sdet-phase-evidence/SKILL.md).

## 8. DevOps does not alone close the phase

Green **DeployedValidation** (or your final stage) is **necessary** not **sufficient** for **Complete** / **Verified**: **SDET** (deployed smoke + observability) and **Analyst** must still align per [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc).

## 9. Preflight commands (examples)

```bash
az account show
aws sts get-caller-identity
```
