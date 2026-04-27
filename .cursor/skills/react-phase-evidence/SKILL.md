---
name: react-phase-evidence
description: >-
  Frontend (React) phase workflow: component and route changes, component test coverage,
  auth and environment notes, and Analyst handoff fields. Use when UI work changes the frontend app
  or when phase evidence for Frontend is needed.
---

# React / Frontend phase evidence

Use for Frontend-owned work under your app root (e.g. `src/front-end/react` or `src/front-end`).

## Required outputs

Capture these fields for the Analyst and `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` (if used):

- `summary`: what changed and why
- `pathsTouched`: pages, components, hooks, API clients, config
- `componentTests`: command, pass/fail, counts
- `allTestsGreen`: `true` only when the full required component suite for handoff is green
- `manualChecks`: noteworthy UI checks when no automated test exists
- `authOrEnvNotes`: auth guard / env var impact
- `knownGaps`: missing tests, TODOs, follow-ups

## Workflow

1. Make the UI change.
2. Run the full **component** (or project-required) test gate before SDET handoff (e.g. `npm run test:unit` in the frontend package).
3. Note route or API-contract changes for SDET and Analyst.
4. Record auth, storage, or environment-variable changes explicitly.
5. Do **not** hand off to SDET until the gate is green or the blocker is documented.

## Output template

```markdown
React evidence
- Summary: ...
- Paths touched: ...
- Component tests: `...` -> ...
- All tests green: true | false
- Manual checks: ...
- Auth / env notes: ...
- Known gaps: ...
```
