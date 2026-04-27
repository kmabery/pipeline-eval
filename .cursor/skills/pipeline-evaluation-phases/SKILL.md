---
name: pipeline-evaluation-phases
description: >-
  Single source of truth for GitHub Actions vs Azure DevOps program phases: vendor locks per
  phase, the matrix source of truth, and full-cycle vs doc-only gates. Use when planning phases,
  delegating agents, or editing evaluation-report for this pipeline evaluation.
---

# Pipeline evaluation phases (GitHub Actions vs Azure DevOps)

<!-- Generated from .cursor/plans/evaluation-topic.yaml by `cursorpack eval sync`. Hand-edits survive only when the manifest still resolves the same values; see .cursor/plans/eval-pack-generator.md. -->

Canonical docs:

- [`docs/decision-matrix/criteria.yaml`](../../../docs/decision-matrix/criteria.yaml) — source of truth (rows = criteria; columns = `gha`, `ado`)
- [`docs/decision-matrix/evidence-guide.md`](../../../docs/decision-matrix/evidence-guide.md) — per-criterion evidence pointers for each vendor
- [`docs/phase-plan.md`](../../../docs/phase-plan.md) — phase order, weights, rubric
- [`docs/evaluation-report.md`](../../../docs/evaluation-report.md) — index with latest rendered matrix
- [`docs/evaluation-reports/`](../../../docs/evaluation-reports/) — archived snapshots per phase run
- [`docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json`](../../../docs/phase-evidence/) — structured evidence per phase

Rule: [`.cursor/rules/pipeline-vendor-phase.mdc`](../../rules/pipeline-vendor-phase.mdc). Matrix write-lanes: [`.cursor/rules/decision-matrix-authoring.mdc`](../../rules/decision-matrix-authoring.mdc).

## Phase map

| Phase | Vendor lock | Primary deliverable |
|-------|-------------|---------------------|
| **1** | GitHub Actions | `vendors.gha` filled in `criteria.yaml`; succeeded `ci-cd-deploy.yml` run; rendered matrix updated |
| **2** | Azure DevOps | `vendors.ado` filled; succeeded `pipeline-eval-cd` run in `ECI-LBMH/LBMH-POC`; rendered matrix updated |
| **3** | Final matrix (Analyst-only) | Locked final snapshot with weighted signals, gap list, recommendation |

Do **not** interleave vendors in one phase pass.

## Full delivery cycle (when deploy evidence is in scope)

Unless **`docs/phase-plan.md`** documents an explicit **doc-only** exception for that phase, expect:

1. **Backend / Frontend** — scoped changes + local test evidence.
2. **SDET (local)** — local `dotnet test` (Unit + Integration + E2E) green and Playwright `local` project green.
3. **DevOps**
   - **Phase 1:** **`ci-cd-deploy.yml`** succeeded; record run ID/URL.
   - **Phase 2:** **`pipeline-eval-cd`** in `ECI-LBMH/LBMH-POC` succeeded; record build ID/URL.
4. **SDET (deployed)** — deployed smoke/E2E against URLs from DevOps; capture vendor-specific evidence for each criterion (approval event URL, artifact-feed log, webhook delivery URL); update the current vendor's cells in `criteria.yaml`.
5. **Analyst** — run the renderer and validators; refresh `docs/evaluation-report.md` (index) and the archived snapshot under `docs/evaluation-reports/`; update `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` (new filename stamp on rerun).
6. **Evidence commit and push** — after the pipeline is **terminal** and Analyst work is done, **commit and push** the evidence paths to **`main`** per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../devops-github-actions-ci-aws/SKILL.md) **§2b** (for **`passed`** and **`failed_incomplete`**; **doc-only:** push after Analyst, no deploy wait).

Closure vocabulary: [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc).

## Doc-only exception

If the phase is explicitly **doc-only** (no deploy proof):

- Do **not** claim **Verified** or **Complete** for deploy-dependent milestones.
- Still obey **vendor phase** rules (no ado edits during phase 1; no gha edits during phase 2).

## Agent hints

- **Analyst orchestrator:** Resolve phase from docs / JSON **before** delegating; never assign another vendor's work in the wrong phase. Phase 3 is Analyst-only and produces the final matrix.
- **SDET validator:** P1 = gha cells, P2 = ado cells. Walk every criterion in `criteria.yaml` and record `rating` + `label` + `citations` + `updatedInPhase`. Capture the vendor's pipeline run URL plus per-criterion proof (approval event, artifact-feed, webhook delivery).
- **DevOps:** P1 = `/devops-github-actions-operator` for `ci-cd-deploy.yml`; P2 = `/devops-pipeline-operator` for `pipeline-eval-cd`. Both deploy to the **same** AWS account and stack — the comparison is the pipeline, not the runtime.
