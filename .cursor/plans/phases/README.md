# Per-phase single-prompt launchers

One markdown file per phase. **Copy-paste the body of the file** as the first message in a fresh main Agent chat (and **attach** [`../../cursor-orchestrator.md`](../../cursor-orchestrator.md)) to start or redo that phase end-to-end. Each launcher is **idempotent**: rerunning the same phase replaces the matching `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` (new stamp) and appends a new dated snapshot under `docs/evaluation-reports/`.

| Phase | Launcher | Vendor lock |
|-------|----------|-------------|
| 1 | [`phase-1-gha.md`](phase-1-gha.md) | GitHub Actions |
| 2 | [`phase-2-ado.md`](phase-2-ado.md) | Azure DevOps |
| 3 | [`phase-3-final-matrix.md`](phase-3-final-matrix.md) | Analyst-only — produces the final matrix |

For ad-hoc work outside the program, use [`../full-cycle-single-prompt.md`](../full-cycle-single-prompt.md) instead.
