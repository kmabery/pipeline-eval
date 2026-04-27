# Workflow contract — coordinating subagents

**Attach this file in the main Agent chat** when running multi-phase work. Specialist prompts live in [`.cursor/agents/`](./agents/); this document is the **coordination contract** for the parent session.

**Project placeholders:** Replace tokens your team documents (for example in `docs/`): `pipeline-eval`, `main`, `kmabery`, `git@github.com:kmabery/pipeline-eval.git`, `ci-cd-deploy.yml`, `{{GHA_WORKFLOW_FILE}}`, `src/front-end`, `us-east-1`. **Canonical CI** is **GitHub Actions** (`.github/workflows/`). **Git** is **GitHub** over SSH — see [`.cursor/rules/github-remote-ssh.mdc`](./rules/github-remote-ssh.mdc).

Official Cursor behavior for subagents is documented in **[Subagents](https://cursor.com/docs/subagents)** and the **[Cursor Docs](https://cursor.com/docs)** index.

## Why this file exists

Subagents start with a **clean context** and **do not** see prior main-chat history. The parent Agent must include task context in each delegation ([How subagents work](https://cursor.com/docs/subagents)). This file keeps the **main** session aligned on phases, gates, and which specialist to invoke.

## Subagents vs skills

| Use **subagents** (`.cursor/agents/*.md`) when… | Use **skills** (`.cursor/skills/*/SKILL.md`) when… |
|------------------------------------------------|---------------------------------------------------|
| You need **context isolation** for long work | The task is **single-purpose** and quick |
| You want **parallel** workstreams | You want a **repeatable checklist** |
| The task needs **specialized expertise** across many steps | One-shot procedural guidance is enough |

See the table in [When to use subagents](https://cursor.com/docs/subagents).

## Orchestrator pattern (phases)

For complex workflows, coordinate specialists **in order** with **structured handoffs** ([Common patterns — Orchestrator pattern](https://cursor.com/docs/subagents)):

1. **Plan / triage** — Analyst orchestrator (or main agent) clarifies scope, artifacts, and gates.
2. **Implement** — Backend and/or Frontend implementers; local test gates per skills.
3. **Validate (local)** — SDET: local E2E and smoke **plus** local observability (logs/traces/metrics via Aspire) before CI evidence.
4. **CI/CD** — DevOps: trigger or verify **ci-cd-deploy.yml**, **monitor to terminal state**, diagnose, retry (GitHub Actions). When deploy fails due to **infra drift** (IAM, ECR, etc.), DevOps may **validate the Terraform CLI** and run **`terraform plan` / `apply`** in **`iac/terraform`** per [`.cursor/agents/devops-github-actions-operator.md`](./agents/devops-github-actions-operator.md), then re-run the workflow.
5. **Architecture drift (AWS CLI)** — After CI is **green**, AWS Solution Architect: compare **live** AWS state to **`docs/architecture/`** and **`iac/terraform/`** using **read-only AWS CLI** only ([`.cursor/agents/aws-solution-architect.md`](./agents/aws-solution-architect.md)). Not a substitute for Terraform remediation (DevOps) or observability (SDET).
6. **Validate (deployed)** — SDET: deployed smoke/E2E against real URLs **and** deploy-window observability evidence (metrics/logs/traces tied to the succeeded run, or documented gap).
7. **Close** — Analyst updates `docs/evaluation-report.md` (index), `docs/phase-evidence/`, and adds/archives a snapshot under `docs/evaluation-reports/`; **then** commit and push those evidence paths to **`main`** (see [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](./skills/devops-github-actions-ci-aws/SKILL.md) §2b). Phase auditor may run readonly verification.

Pipeline order for deploy evidence: **pre-pipeline commit (§2a) → CI green → AWS SA (CLI drift) → SDET deployed smoke + observability → Analyst → post-evidence commit (§2b)**.

Each handoff should include: **what changed**, **commands run**, **pass/fail**, **links/IDs**, **blockers**.

## Delegation mechanics

- **Automatic:** Agent uses each subagent file’s `description` to decide delegation ([Using subagents](https://cursor.com/docs/subagents)).
- **Explicit:** Invoke by **`/name`** (filename without `.md`), e.g. `/backend-implementer`, `/sdet-validator` ([Using subagents](https://cursor.com/docs/subagents)).
- **Parallel:** Independent workstreams may run in parallel ([Parallel execution](https://cursor.com/docs/subagents)); dependent steps stay **sequential**.

## Foreground vs background

| Mode | Behavior | Best for |
|------|----------|----------|
| **Foreground** | Blocks until the subagent completes | Sequential steps that need the result immediately |
| **Background** | Returns immediately; subagent continues | Long-running or parallel streams |

See [Foreground vs background](https://cursor.com/docs/subagents). Set `is_background` in subagent frontmatter when appropriate.

## Subagent file format (reminder)

Each specialist under `.cursor/agents/` is a Markdown file with YAML frontmatter ([Custom subagents](https://cursor.com/docs/subagents)):

- `name`, `description` (important for routing)
- Optional: `model` (`inherit`, `fast`, or a model id), `readonly`, `is_background`

## Expanding feedback loop (strict order)

For phases that require production evidence:

1. **Developer gates** — Backend and Frontend finish scoped work and record **local** evidence (see respective skills).
2. **SDET local gate** — Local E2E / smoke green **and** local observability (logs/traces/metrics via Aspire) green or documented blocker **before** CI is treated as proof of readiness.
3. **DevOps gate** — CI **queued/triggered, monitored to completion**; **all required jobs** green — `.github/workflows/` (canonical: **`ci-cd-deploy.yml`**), with **`pipelines/`** docs for webhook monitoring and handoffs (adjust names to match **your** YAML). If failure is **Terraform drift** in AWS, align **`iac/terraform`** (validate Terraform CLI, then **`plan` / `apply`**) before claiming green CI.
4. **AWS SA gate (architecture drift)** — After green CI, **AWS Solution Architect** validates deployed resources against **`docs/architecture/`** and the Terraform contract using **AWS CLI** ([`.cursor/agents/aws-solution-architect.md`](./agents/aws-solution-architect.md)). On drift, remediate via DevOps/Backend before treating the cycle as green.
5. **SDET observability gate** — Deploy-window observability for the succeeded run **or** explicit **documented gap** with reason.
6. **Analyst gate** — Report and phase narrative align with the same build/deploy window.
7. **Evidence persistence gate** — After the Analyst step, `docs/phase-evidence/`, `docs/evaluation-reports/`, and `docs/evaluation-report.md` (when changed) are **committed and pushed** to **`main`**, per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](./skills/devops-github-actions-ci-aws/SKILL.md) §2b. Does **not** replace §2a (pre-pipeline commit).

Rules: [`.cursor/rules/expanding-feedback-loop.mdc`](./rules/expanding-feedback-loop.mdc), [`.cursor/rules/phase-closure-gate.mdc`](./rules/phase-closure-gate.mdc), [`.cursor/rules/phase-gate-outcomes.mdc`](./rules/phase-gate-outcomes.mdc) (failed vs passed deploy gates; all `phase-MM-dd-yyyy-HHmmss-<n>.json`).

## Full cycle (mandatory when deploy evidence is in scope)

For phases that ship or validate **deployed** environments (default for phase-plan work unless `docs/phase-plan.md` documents a **doc-only** exception):

0. **Commit and push** — At the **beginning** of the cycle, commit **all** scoped product and docs changes and push to **`main`** on **`git@github.com:kmabery/pipeline-eval.git`**. SDET and CI must run against a **clean** tree and prove the same revision that exists on the remote default branch. Skipping this step invalidates deploy-cycle evidence. See [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](./skills/devops-github-actions-ci-aws/SKILL.md) §2a.

1. **Implement** — Backend and/or Frontend; local test gates per skills.
2. **SDET (local)** — Local E2E/smoke green **and** local observability (logs/traces/metrics via Aspire dashboard) green per [`.cursor/skills/sdet-phase-evidence/SKILL.md`](./skills/sdet-phase-evidence/SKILL.md).
3. **DevOps** — Canonical workflow **`ci-cd-deploy.yml`** succeeded; record **run ID** and URL. If blocked by IAM/ECR or similar, resolve via **`iac/terraform`** after validating the Terraform CLI (see [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](./skills/devops-github-actions-ci-aws/SKILL.md) §4a).
4. **AWS Solution Architect** — **Architecture drift validation** using **AWS CLI** only: compare live ECS, ALB, SSM, CloudFront, and related resources to **`docs/architecture/`** and **`iac/terraform/`** (see [`iac/terraform/outputs.tf`](../iac/terraform/outputs.tf)). Record **pass/fail**, **resource-level deltas**, and **CLI commands run** for the Analyst handoff. On drift, route remediation to **DevOps** or **Backend** as appropriate before treating the cycle as green ([`.cursor/agents/aws-solution-architect.md`](./agents/aws-solution-architect.md)).
5. **SDET (deployed)** — Deployed smoke/E2E against real URLs from DevOps ([`.cursor/skills/post-deploy-verify/SKILL.md`](./skills/post-deploy-verify/SKILL.md)) **and** deploy-window observability evidence for that run or **documented gap** ([`.cursor/skills/sdet-phase-evidence/SKILL.md`](./skills/sdet-phase-evidence/SKILL.md)).
6. **Analyst** — `docs/evaluation-report.md` (index), optional archived narrative under `docs/evaluation-reports/`, and `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` aligned with the same window. If gates are incomplete after self-heal ([`.cursor/skills/phase-gate-self-heal/SKILL.md`](./skills/phase-gate-self-heal/SKILL.md)), set **`phaseGateOutcome`: `failed_incomplete`** — not a successful deploy evaluation run.
7. **Evidence commit and push** — After `ci-cd-deploy.yml` is **terminal** and the Analyst step has produced or updated the artifacts above, commit and push `docs/phase-evidence/`, `docs/evaluation-reports/`, and `docs/evaluation-report.md` to **`main`** (SSH) per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](./skills/devops-github-actions-ci-aws/SKILL.md) **§2b** — for both **`passed`** and **`failed_incomplete`**. **Doc-only** phases: commit after Analyst finalization (no deploy in scope; still persist evidence to the remote).

**GHA vs ADO pipeline evaluation:** Read vendor phase **before** delegating ([`.cursor/rules/pipeline-vendor-phase.mdc`](./rules/pipeline-vendor-phase.mdc), [`.cursor/skills/pipeline-evaluation-phases/SKILL.md`](./skills/pipeline-evaluation-phases/SKILL.md)). Phase 1 = **GitHub Actions** (`/devops-github-actions-operator`, `ci-cd-deploy.yml`), phase 2 = **Azure DevOps** (`/devops-pipeline-operator`, `pipeline-eval-cd` in `ECI-LBMH/LBMH-POC`), phase 3 = **Final matrix** (Analyst-only) — one-shot launchers live under [`.cursor/plans/phases/`](./plans/phases/). Criteria source of truth: [`docs/decision-matrix/criteria.yaml`](../docs/decision-matrix/criteria.yaml). During a vendor phase, edit only that vendor's cells.

## Artifact conventions (customize per repo)

| Artifact | Purpose |
|----------|---------|
| `docs/evaluation-report.md` | Index + pointers to latest phase snapshot and structured evidence |
| `docs/evaluation-reports/*.md` | Archived evaluation reports per phase run — filename stamp **`MM-dd-yyyy-HHmmss`** (see [`docs/evaluation-report.md`](../docs/evaluation-report.md)); optional GitHub run id in `-gha-<runId>`; each snapshot should include **`### What changed in code (on main)`** (commits merged during the attempt, or explicit none) |
| `docs/phase-plan.md` | Planned program phases (if used) |
| `docs/decision-matrix/criteria.yaml` | Decision-matrix source of truth (rows = criteria; columns = GitHub Actions / Azure DevOps) |
| `.cursor/plans/phases/phase-<n>-*.md` | Per-phase single-prompt launchers (start or redo a phase) |
| `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` | Structured evidence (one file per program phase; replace prior sidecar for that phase on rerun) |

Copy this contract into `docs/` if you want the canonical workflow version-controlled outside `.cursor/` (optional).

## Specialist roster (this pack)

| Role | Agent file |
|------|------------|
| Analyst orchestrator | [`agents/analyst-orchestrator.md`](./agents/analyst-orchestrator.md) |
| Backend | [`agents/backend-implementer.md`](./agents/backend-implementer.md) |
| Frontend (e.g. React) | [`agents/react-implementer.md`](./agents/react-implementer.md) |
| SDET (E2E + observability) | [`agents/sdet-validator.md`](./agents/sdet-validator.md) — owns local + deploy-window observability as well as E2E |
| DevOps (GitHub Actions) | [`agents/devops-github-actions-operator.md`](./agents/devops-github-actions-operator.md) — owns `ci-cd-deploy.yml` (phase 1) |
| DevOps (Azure DevOps) | [`agents/devops-pipeline-operator.md`](./agents/devops-pipeline-operator.md) — owns `pipeline-eval-cd` in `ECI-LBMH/LBMH-POC` (phase 2) |
| Phase auditor (readonly) | [`agents/phase-auditor.md`](./agents/phase-auditor.md) |
| AWS Solution Architect (YARP, SSM, Terraform alignment, CLI drift) | [`agents/aws-solution-architect.md`](./agents/aws-solution-architect.md) — **mandatory** for full deploy-cycle phases: **AWS CLI** architecture drift check after green CI; optional for ad-hoc gateway/routing design **without** editing GitHub Actions workflows |

**END:** Do not claim **Verified** or **Complete** for a milestone that requires deploy evidence until **DevOps + AWS SA drift check (passed) + SDET observability + Analyst** conditions and **evidence on `main` (§2b)** in [`.cursor/rules/phase-closure-gate.mdc`](./rules/phase-closure-gate.mdc) are satisfied.
