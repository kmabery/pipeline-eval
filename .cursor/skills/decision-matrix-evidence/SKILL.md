---
name: decision-matrix-evidence
description: >-
  Concrete checklists for editing docs/decision-matrix/criteria.yaml. SDET updates a single vendor
  column end-to-end during phases 1-3; Analyst owns criterion CRUD, weights, rendering, snapshots,
  and phase-4 synthesis. Use when a phase's SDET or Analyst touches the decision matrix.
---

# Decision-matrix evidence (SDET + Analyst)

Source of truth: [`docs/decision-matrix/criteria.yaml`](../../../docs/decision-matrix/criteria.yaml). Per-criterion hypotheses and links: [`docs/decision-matrix/evidence-guide.md`](../../../docs/decision-matrix/evidence-guide.md). Write-lane rule: [`../../rules/decision-matrix-authoring.mdc`](../../rules/decision-matrix-authoring.mdc). Vendor phase lock: [`../../rules/observability-vendor-phase.mdc`](../../rules/observability-vendor-phase.mdc).

## Rating icons (rendered)

| `rating` | Icon | Use when |
|----------|------|----------|
| `pass` | ✅ | Primary doc confirms an OTel-first / vendor-native path; no workaround required |
| `caveat` | ⚠️ | Works, but a trial is pending, only partial coverage, or vendor-specific workaround |
| `fail` | ❌ | Missing or requires a second product to satisfy |
| `tbd` | — | Not yet assessed in this vendor phase |

## SDET checklist (phase 1 Coralogix, phase 2 CloudWatch, phase 3 Datadog)

For **every** criterion entry in `criteria.yaml`:

1. Read the criterion's `whyItMatters` and `evidenceToCollect`; open [`evidence-guide.md`](../../../docs/decision-matrix/evidence-guide.md) to the matching section for this vendor.
2. Fetch the **primary doc URL** (vendor docs site; not a blog post unless that is the canonical source).
3. Assign a rating from the table above. Err toward `caveat` if the doc exists but a trial artifact is pending; use `tbd` only if you lack access and note why.
4. Write a short **label** (≤ 60 characters) that summarizes the finding; this is what shows in the rendered matrix cell.
5. Add at least one URL to `citations[]` (required for any rating other than `tbd`). Prefer documentation anchors over home pages.
6. Record any nuance in `notes` (one or two sentences). Reference tenant IDs, trial names, or blocking operator steps here.
7. Set `updatedInPhase: <currentPhase>` (1 for Coralogix, 2 for CloudWatch, 3 for Datadog).
8. Do **not** edit any other vendor block, the top-level criterion fields, or `commonNotes`.

After all criteria are updated, run:

```bash
node scripts/validate-decision-matrix.mjs
```

Keep cycling (fetch doc → update cell → validate) until the validator is clean. Do not run the renderer; that is the Analyst's step.

## Analyst checklist (every phase)

1. Before the SDET starts, confirm the vendor lock for this phase and the list of criteria in `criteria.yaml` still reflects stakeholder priorities. Add / remove / reword criteria as needed and rebalance `weight` so it sums to 100. Non-vendor fields (including `weight`, `group`, `commonNotes`) are yours.
2. After the SDET updates their vendor column, run the validator:

   ```bash
   node scripts/validate-decision-matrix.mjs
   ```

3. Render into all active targets:

   ```bash
   node scripts/render-decision-matrix.mjs --target phase-plan
   node scripts/render-decision-matrix.mjs --target index
   node scripts/render-decision-matrix.mjs --target snapshot --path docs/evaluation-reports/<phase-snapshot>
   ```

4. Write [`docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json`](../../../docs/phase-evidence/) (replace any prior sidecar for that phase) with:
   - `matrixEdits[]` — every criterion id whose `<vendor>` rating moved off `tbd` this phase (empty array + `matrixEditsNote` rationale allowed if you only reworded labels).
   - Usual gate fields (`phaseGateOutcome`, `deployCycleGate`, `evidenceLinks`, SDET / DevOps / observability references).

5. Validate the phase evidence:

   ```bash
   node scripts/validate-phase-evidence.mjs
   ```

6. Hand off to `phase-auditor` (readonly) to confirm the closure gate.

## Analyst checklist — Phase 4 (Final matrix)

Phase 4 is Analyst-only. **Do not touch any vendor cell.**

1. Verify every criterion has a non-`tbd` rating for Coralogix, CloudWatch, and Datadog (or an explicit `tbd` with a reason in `notes`). If gaps remain, block and request the appropriate vendor-phase rerun via `.cursor/plans/phases/phase-<n>-<vendor>.md`.
2. Compute weighted signals per vendor (`pass=1.0`, `caveat=0.5`, `fail=0.0` × `weight`, summed then divided by 100). Record in `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-4.json` under `synthesis.weightedSignals`.
3. Build `synthesis.gapList[]` — one entry per material gap:
   - `severity`: `blocker` | `major` | `minor`.
   - `vendor`: the affected vendor.
   - `mitigation`: concrete workaround or follow-up.
   - `owner`: who carries it.
4. Write `synthesis.recommendation` — one paragraph naming the preferred vendor and the conditions that would flip the recommendation. Include at least one sensitivity note (e.g. "flips to CloudWatch if RUM weight drops to 10").
5. Render the final matrix with `--final` into a locked snapshot:

   ```bash
   node scripts/render-decision-matrix.mjs --target phase-plan
   node scripts/render-decision-matrix.mjs --target index
   node scripts/render-decision-matrix.mjs --target snapshot \
     --path docs/evaluation-reports/evaluation-report-<MM-dd-yyyy-HHmmss>-phase-4-final-matrix.md --final
   ```

6. Point the **Latest archived report** row in `docs/evaluation-report.md` at the phase-4 snapshot.
7. Validate:

   ```bash
   node scripts/validate-decision-matrix.mjs
   node scripts/validate-phase-evidence.mjs
   ```

8. The phase-4 snapshot is the **program's decision artifact**. After auditor confirmation, treat it as locked — any future vendor-data change requires reopening the relevant vendor phase, not editing phase 4.

## Quick reference — files

| File | Owner (by phase) |
|------|------------------|
| `docs/decision-matrix/criteria.yaml` | SDET edits the current vendor's cells; Analyst does CRUD / weights |
| `docs/decision-matrix/evidence-guide.md` | Analyst (criterion descriptions); SDET may add vendor-specific links |
| `docs/phase-plan.md` (rendered block) | Renderer only — never hand-edit |
| `docs/evaluation-report.md` (rendered block + Latest row) | Analyst |
| `docs/evaluation-reports/<file>` (rendered block + narrative) | Analyst |
| `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` | Analyst replaces on every phase rerun (new `MM-dd-yyyy-HHmmss` stamp) |
