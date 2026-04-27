# Phase 3 — Final comparison (single-prompt launcher)

<!-- Generated from .cursor/plans/evaluation-topic.yaml by `cursorpack eval sync`. See .cursor/plans/eval-pack-generator.md. -->

Use as the **first message** in a main Agent chat. **Attach** [`../../cursor-orchestrator.md`](../../cursor-orchestrator.md) before sending. Phases 1 (GHA) and 2 (ADO) must each have a recorded outcome before this phase is meaningful. The phase-3 snapshot produced here is the **program's decision artifact**.

---

You are the Analyst Orchestrator. Start or redo **Phase 3 — Final comparison**
for pipeline-eval. Attach `.cursor/cursor-orchestrator.md`.

Phase 3 is **Analyst-only**. Do not delegate vendor-cell edits to
`/sdet-validator` and do not modify any `vendors.*` block in
`docs/decision-matrix/criteria.yaml`. Per
`.cursor/rules/decision-matrix-authoring.mdc`, vendor cells are frozen
in this phase.

Preconditions:

- `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-1.json` and `phase-MM-dd-yyyy-HHmmss-2.json`
  both exist with a recorded `phaseGateOutcome`
  (`passed` or `failed_incomplete`).
- Every criterion in `docs/decision-matrix/criteria.yaml` has a non-`tbd`
  rating for `gha` and `ado` (or an explicit `tbd`
  with reason in `notes`). If gaps exist, **block** and request the
  relevant vendor-phase rerun via `.cursor/plans/phases/phase-<n>-<vendor>.md`.

Analyst work:

1. Sanity-check `criteria.yaml`:
   - `node scripts/validate-decision-matrix.mjs`.
   - Confirm both vendor blocks per criterion are non-`tbd` (or
     documented).
2. Compute weighted signals per vendor:
   `signal = sum(weight * score) / 100`,
   where `score = pass:1.0 | caveat:0.5 | fail:0.0 | tbd:0`. Record in
   `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-3.json` under `synthesis.weightedSignals`
   with one numeric field per vendor (`gha`, `ado`).
3. Build `synthesis.gapList[]` — one entry per material gap with
   `severity` (`blocker` | `major` | `minor`), `vendor`, `mitigation`,
   `owner`.
4. Write `synthesis.recommendation` — one paragraph naming the preferred
   vendor and the conditions that would flip the recommendation.
   Include a one-line **sensitivity note** (e.g. recompute weighted
   signals with `caveat=0.25` and `caveat=0.75` and report whether the
   ranking is stable).
5. Render the final matrix:
   - `node scripts/validate-decision-matrix.mjs`
   - `node scripts/render-decision-matrix.mjs --target phase-plan`
   - `node scripts/render-decision-matrix.mjs --target index`
   - Create
     `docs/evaluation-reports/evaluation-report-MM-dd-yyyy-HHmmss-phase-3-final.md`
     containing only these sections: `## Executive summary` (SHORT — problems and deltas vs. the previous run only), `## Link bundle` (deployed URLs + GitHub Actions run + Azure DevOps build URLs), and `## Rendered decision matrix` with a `matrix:begin` / `matrix:end` HTML-comment marker block plus a brief narrative quoting `synthesis.recommendation`, the gap list, and the weighted-signal table.
   - `node scripts/render-decision-matrix.mjs --target snapshot --path <that file> --phase 3 --final` (emits the full two-vendor matrix with the weighted-signals footer).
   - Update `docs/evaluation-report.md` Latest archived report row to
     point at the new phase-3 snapshot.
6. Write `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-3.json` (replace any prior phase **3** sidecar in `docs/phase-evidence/`):
   - `phase: 3`, `scope: "finalMatrix"`.
   - `matrixEdits: []` (must be empty).
   - `synthesis.{weightedSignals, gapList, recommendation}` populated.
   - Standard gate fields if a deploy is in scope; otherwise
     `phasePlanException: "doc_only"` and
     `phaseGateOutcome: "not_applicable_doc_only"`.
   - `node scripts/validate-phase-evidence.mjs`.
7. **Evidence commit and push** — per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../../skills/devops-github-actions-ci-aws/SKILL.md) **§2b** after the Analyst work above: **commit and push** `docs/phase-evidence/`, `docs/evaluation-reports/`, and `docs/evaluation-report.md` to **`main`** (when a deploy was in scope, the pipeline must be **terminal** first; **doc-only** runs **§2b** after finalization).

The locked phase-3 snapshot is the program's final decision artifact.
Do not edit vendor cells without reopening the relevant vendor phase
via its launcher.
