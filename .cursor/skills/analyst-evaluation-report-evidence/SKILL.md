---
name: analyst-evaluation-report-evidence
description: >-
  Analyst-owned evidence requirements for docs/evaluation-report.md and archived snapshots: per-agent
  rows (Backend, Frontend, SDET — including pipeline run evidence, DevOps), link bundle (deployed
  URLs, pipeline run URLs), manual verification URL loop, executive-summary vocabulary, and
  alignment with phase-closure gate.
---

# Evaluation report evidence requirements

When editing `docs/evaluation-report.md` (index) and the **latest archived snapshot** under `docs/evaluation-reports/`, ensure the report contains evidence from **all** agent roles in your roster (see [`.cursor/cursor-orchestrator.md`](../../cursor-orchestrator.md)):

| Agent | Required evidence |
|-------|-------------------|
| **Backend** | Unit and integration test results; API observations |
| **Frontend** | Component/unit test results; UX notes |
| **SDET** | `dotnet test` (Unit/Integration/E2E) results, Playwright `local` + `deployed` results, vendor pipeline run URL (GH Actions in P1, ADO build in P2), per-criterion proof (approval URL, artifact-feed log, webhook delivery URL) |
| **DevOps** | Phase 1: GitHub Actions **run ID** + outcomes for `ci-cd-deploy.yml`. Phase 2: Azure DevOps **build ID** + outcomes for `pipeline-eval-cd`. Distinguish **queued/in progress** vs **succeeded** with every required job/stage green. |
| **Analyst** | Phase narrative, scoring, recommendation where applicable |

**Per-phase closure:** Evidence (or a documented gap) from **all** applicable rows is required before **Complete** or **Verified**.

Do **not** claim **complete** or **verified** without: (1) **DevOps** proof of a **succeeded** pipeline, (2) **SDET** pipeline-run URL + per-criterion proof for **that** run, (3) **Analyst** alignment, (4) **evidence on `main`** — commit and push per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md) **§2b** when this pass touched `docs/phase-evidence/`, `docs/evaluation-reports/`, or the index.

Pushes to `main` that change only `docs/**` do **not** start `ci-cd-deploy.yml` (path filter in [`.github/workflows/ci-cd-deploy.yml`](../../../.github/workflows/ci-cd-deploy.yml)). Use **Actions → CI-CD-Deploy → Run workflow** if you need a full pipeline after a docs-only commit.

**Failed (incomplete):** If full-cycle deploy proof is missing after self-heal ([`.cursor/skills/phase-gate-self-heal/SKILL.md`](../phase-gate-self-heal/SKILL.md)), set **`phaseGateOutcome`: `failed_incomplete`** in `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` and state **Deploy cycle gate: Failed (incomplete)** in the index and/or snapshot per [`.cursor/rules/phase-gate-outcomes.mdc`](../../rules/phase-gate-outcomes.mdc). Do not describe the phase as a successful deploy evaluation.

## Link bundle (full delivery cycle)

When **`phaseGateOutcome`** is **`passed`**, the phase sidecar and narrative must include:

| Link / ID | Where |
|-----------|--------|
| **Deployed API** (HTTPS base URL) | `evidenceLinks.deployedApiUrl` in `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json`; repeat in snapshot |
| **Deployed web / SPA** (HTTPS) | `evidenceLinks.deployedWebUrl` |
| **Pipeline run URL** (phases 1-2; mandatory) | `evidenceLinks.pipelineRunUrl` — non-empty HTTPS GitHub Actions run URL (phase 1) or Azure DevOps build URL (phase 2). **If absent → `phaseGateOutcome: failed_incomplete`.** Stamp via `node scripts/stamp-pipeline-run.mjs <phase-evidence.json> --url <url> --runId <id>` before flipping `phaseGateOutcome` to `passed`. |
| **Approval event URL** | `evidenceLinks.approvalEventUrl` — phase 1: GitHub Environment approval review URL; phase 2: ADO approval check deep link. |
| **Webhook delivery URL** | `evidenceLinks.webhookDeliveryUrl` — phase 1: `gh api repos/.../hooks/<id>/deliveries/<id>`; phase 2: ADO service hook notification record. |

Update **`docs/evaluation-report.md`** (index) **Latest archived report** row to point at the new snapshot file when a phase completes or reruns.

## Archived snapshot shape (phases 1-3)

Every archived snapshot under `docs/evaluation-reports/` must contain **only** these sections, in this order:

1. `## Executive summary` — **SHORT** (2-3 sentences). Call out problems with the run and deltas vs. the previous run. Do **not** recite the full run log.
2. `## Link bundle` — the bullets above, including the mandatory pipeline run URL for phases 1-2.
3. `## Rendered decision matrix` — a `<!-- matrix:begin -->` / `<!-- matrix:end -->` marker block, populated by `node scripts/render-decision-matrix.mjs --target snapshot --path <file> --phase <n>`.
   - Phases 1-2 render a 4-column `Criterion | Weight | Evidence | Notes` table scoped to that phase's vendor.
   - Phase 3 renders the full two-vendor matrix (`--phase 3 --final`) with the weighted-signals footer.

Do **not** add `### What changed in code (on main)`, `### Per-agent rows`, or `## Phase auditor (readonly)` sections to snapshots; those have been retired in favor of the JSON sidecar + short executive summary.

**Archived snapshot filenames:** use **`evaluation-report-MM-dd-yyyy-HHmmss-phase-<n>[-<vendor>-<runId>].md`** (same **MM-dd-yyyy-HHmmss** stamp rules as phase JSON). For example: `evaluation-report-04-27-2026-093500-phase-1-gha-12345.md`. See **How to add a new snapshot** in [`docs/evaluation-report.md`](../../../docs/evaluation-report.md).

**Phase evidence JSON filenames:** use **`phase-MM-dd-yyyy-HHmmss-<n>.json`**. See [`docs/phase-evidence/README.md`](../../../docs/phase-evidence/README.md).

## Manual verification URLs (Analyst loop)

`manualVerificationUrls` in `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` complements **`evidenceLinks`** when deploy proof is delayed or agents cannot fetch Terraform outputs in-session: **manual smoke** evidence — HTTPS API/web base URLs plus **`instructions`** for how to verify.

**When `apiBaseUrl` and/or `webBaseUrl` are missing from the evaluation-report package** (JSON and, where applicable, the index / latest snapshot), the **Analyst** treats that as **actionable**, not a silent default:

1. **Root cause** — Record why URLs are absent (for example: no AWS session for `terraform output`, GitHub→AWS OIDC or deploy job not green, wrong output names, pipeline summary not yet exposing bases).
2. **Remediate** — Use the right path: [`.cursor/skills/phase-gate-self-heal/SKILL.md`](../phase-gate-self-heal/SKILL.md), DevOps re-run of canonical CI, operator `aws sso login`, or fixing trust/OIDC so deploy completes.
3. **Re-attempt** — Populate from Terraform outputs, workflow summary, pipeline logs, or a succeeded deploy's known HTTPS bases; keep JSON and narrative aligned.

**Stop condition:** Either both HTTPS bases are populated (and reflected in `docs/evaluation-report.md` / the latest snapshot when manual smoke is part of the story), **or** the phase is finalized honestly as **`failed_incomplete`** (or a **doc-only** exception per plan) with **`deployCycleGate.reasons`** and narrative explaining why manual URLs could not be obtained **after** self-heal — not **null with no diagnosis or retry**. Do not claim **Complete** or **Verified** without the usual DevOps + SDET pipeline-run + Analyst bar in [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc).

See [`.cursor/rules/phase-gate-outcomes.mdc`](../../rules/phase-gate-outcomes.mdc) and [`docs/phase-evidence/README.md`](../../../docs/phase-evidence/README.md).

## Status line vocabulary (executive summary)

- **`Status:`** must **not** imply **Complete** or **Verified** unless DevOps + SDET pipeline-run evidence + Analyst conditions and **evidence on `main` (§2b)** are met.
- Prefer: **In Progress** — local OK; **awaiting** pipeline / **deployed smoke** / final Analyst pass.

## Structured phase evidence

JSON sidecars: `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` — validate with `node scripts/validate-phase-evidence.mjs`. Treat JSON as structured source; index + `docs/evaluation-reports/` snapshots as narrative rollup. See `docs/phase-evidence/README.md` for the full-cycle field list.

## Finalize: commit and push (mandatory when files changed)

After `validate-phase-evidence`, index/snapshot updates, and any `render-decision-matrix` work, if **`docs/phase-evidence/`**, **`docs/evaluation-reports/`**, or **`docs/evaluation-report.md`** were created or modified, run the **post-evidence** commit per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md) **§2b** so the default branch matches your closure narrative — including for **`failed_incomplete`**.

## GHA vs ADO program (this repo)

Phase order and vendor locks: [`.cursor/skills/pipeline-evaluation-phases/SKILL.md`](../pipeline-evaluation-phases/SKILL.md) and [`.cursor/rules/pipeline-vendor-phase.mdc`](../../rules/pipeline-vendor-phase.mdc). Phase 1 = GitHub Actions; phase 2 = Azure DevOps; phase 3 = final matrix. Do not edit the other vendor's column during a vendor phase. Decision-matrix authoring write-lanes: [`.cursor/rules/decision-matrix-authoring.mdc`](../../rules/decision-matrix-authoring.mdc); per-criterion checklists: [`.cursor/skills/decision-matrix-evidence/SKILL.md`](../decision-matrix-evidence/SKILL.md).
