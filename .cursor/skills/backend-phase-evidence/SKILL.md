---
name: backend-phase-evidence
description: >-
  Backend phase workflow: API or service changes, unit and integration verification, contract
  notes, and Analyst handoff fields. Use when Backend changes services, IaC-backed APIs, or phase evidence.
---

# Backend phase evidence

Use for Backend-owned work under your repo’s backend paths (e.g. `src/services/`, `tests/unit/`, `tests/integration/` — **adjust to your layout**).

## Required outputs

Capture these fields for the Analyst and `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` (if used):

- `summary`: what changed and why
- `pathsTouched`: key files or directories
- `unitTests`: command, pass/fail, counts
- `integrationTests`: command, pass/fail, counts, environment assumptions
- `allTestsGreen`: `true` only when every required Backend suite for the handoff is green
- `contractNotes`: changes Frontend or SDET need
- `knownGaps`: deferred, flaky, or blocked work

## Workflow

1. Make the Backend change.
2. Run **all** required unit tests (example: `dotnet test <YourUnitTestProject>.csproj`).
3. Run **all** required integration tests.
4. When you stop local .NET hosts you started in the integrated terminal (**`dotnet run`**, AppHost), use **Ctrl+C** in that terminal — not PID kill ([`.cursor/rules/dotnet-local-graceful-shutdown.mdc`](../../rules/dotnet-local-graceful-shutdown.mdc)).
5. Avoid colliding with E2E-owned local ports; use dynamic or alternate ports for manual API runs.
6. Record contract-impacting changes: paths, methods, payloads, auth, env vars.
7. Do **not** hand off to SDET until suites are green or the blocker is documented.
8. Hand off a compact evidence block to Analyst / SDET / Frontend.

## Output template

```markdown
Backend evidence
- Summary: ...
- Paths touched: ...
- Unit tests: `...` -> ...
- Integration tests: `...` -> ...
- All tests green: true | false
- Contract notes: ...
- Known gaps: ...
```
