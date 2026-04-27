# Plan templates

Plan-template inputs for Cursor agents. Files in this folder are **inputs** to the workflow — drop their bodies into a fresh main Agent chat or feed them to the [Cursor CLI](https://cursor.com/docs/cli/overview) — and the **manifest contract** that drives the optional [`cursorpack`](https://github.com/kmabery/cursorpack) generator. Rendered outputs (phase plan, evaluation report, decision matrix, snapshots, phase-evidence sidecars) live in [`docs/`](../../docs/).

| Path | Purpose |
|------|---------|
| [`phases/`](./phases/) | Per-phase single-prompt launchers (Coralogix → CloudWatch → Datadog → Final matrix). One file per phase; idempotent on rerun. |
| [`full-cycle-single-prompt.md`](./full-cycle-single-prompt.md) | Generic ad-hoc full-cycle prompt for work outside the phased program. |
| [`evaluation-topic.yaml`](./evaluation-topic.yaml) | Single source of truth for the evaluation topic: candidates, phase numbers, rating model, weight total, generated/hand-authored file lists. |
| [`eval-pack-generator.md`](./eval-pack-generator.md) | Contract for the external [`cursorpack`](https://github.com/kmabery/cursorpack) Python CLI that re-templates this scaffold from the manifest. |

## How the plans are consumed

- **Cursor IDE / agents.** Copy the body of a phase launcher into a fresh Agent chat (with [`../cursor-orchestrator.md`](../cursor-orchestrator.md) attached) to start or redo that phase end-to-end.
- **Cursor CLI.** `cursor-agent --print --output-format json < .cursor/plans/phases/phase-1-coralogix.md` runs the same prompt headlessly. See [Cursor CLI overview](https://cursor.com/docs/cli/overview).
- **Eval Scaffolder.** [`src/front-end/`](../../src/front-end/) (Vite + React UI) plus [`src/eval_scaffolder/`](../../src/eval_scaffolder/) (FastAPI) scaffolds new evaluation folders or zip downloads from `base/` and `.cursor/`. For day-to-day editing of manifest + criteria in an **existing** evaluation repo, use your editor and the Node validate / render scripts and the `cursor-agent` CLI as documented in the repo root README.
- **`cursorpack eval`.** The external [`cursorpack`](https://github.com/kmabery/cursorpack) CLI scaffolds a fresh repo from the manifest and re-renders generated artifacts with `eval sync`. Ideal for **re-templating** to a different topic (pipelines, AWS stacks, …).

## What stays in `docs/`

- [`docs/phase-plan.md`](../../docs/phase-plan.md) — rendered, public phase plan.
- [`docs/evaluation-report.md`](../../docs/evaluation-report.md) — index + matrix snapshot.
- [`docs/evaluation-reports/`](../../docs/evaluation-reports/) — archived per-phase narrative snapshots.
- [`docs/phase-evidence/`](../../docs/phase-evidence/) — structured JSON sidecars per run.
- [`docs/decision-matrix/`](../../docs/decision-matrix/) — criteria source-of-truth + evidence guide.

## Closure gates still apply

Phase closure language (**Verified**, **Complete**, **Failed (incomplete)**) is governed by [`.cursor/rules/phase-closure-gate.mdc`](../rules/phase-closure-gate.mdc) and [`.cursor/rules/phase-gate-outcomes.mdc`](../rules/phase-gate-outcomes.mdc). Editing a launcher here does **not** change those gates.
