---
name: sdet-phase-evidence
description: >-
  SDET phase workflow for the GHA vs ADO pipeline evaluation: targeted local test runs,
  deployed smoke against the same AWS stack from each vendor, vendor-specific pipeline-evidence
  capture (approval event URL, artifact-feed proof, webhook delivery proof), decision-matrix
  vendor-cell pointer, and standardized Failure Handoff output.
---

# SDET phase evidence

Use for `tests/PipelineEval.{UnitTests,IntegrationTests,E2ETests}`, the Playwright projects under `src/front-end/PipelineEval.Web/tests/e2e`, deployed smoke against the URLs DevOps publishes, and per-vendor pipeline evidence capture.

## Required outputs

Capture these fields for the Analyst and `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` (if used):

- `localTests`: command list, projects, pass/fail, counts (Unit, Integration, E2E + Playwright `local`)
- `deployedSmoke`: command, environment URLs, pass/fail, counts (Playwright `deployed`)
- `pipelineRun`:
  - **Phase 1:** `{vendor: "gha", runId, runUrl, conclusion, environment: "production"}`
  - **Phase 2:** `{vendor: "ado", buildId, runUrl, conclusion, environment: "production"}`
- `criterionEvidence`:
  - `approval`: `{actor, approvedAt, urlToApprovalEvent}` (GH `actions/runs/<id>/approvals` or ADO build `Approvals` deep link)
  - `package`: `{feed: "github-packages"|"azure-artifacts"|"npm cache hit"|...,  evidenceUrlOrLogExcerpt}`
  - `webhook`: `{hookId, deliveryId, statusCode, deliveryUrl}` (GH `hooks/<id>/deliveries` or ADO `serviceHooks/_apis/notifications`)
- `allureOrArtifacts`: links or artifact names (Playwright report, ADO test attachments, GH Actions artifact)
- `failureHandoffs`: zero or more standardized handoffs
- `allLocalGatesGreen`: `true` only when local `dotnet test` + Playwright `local` are all green
- `knownGaps`: unrun coverage, flaky tests, blocked deploy validation, missing approval event URL

## Failure Handoff format

```markdown
Failure Handoff
- Run ID / build ID / run URL: ...
- Scope: local | deployed | pipeline
- Failed project / spec / job: ...
- Error excerpt: ...
- Suspected root cause: ...
- Assigned: Backend | Frontend | DevOps
- Required regression before re-run: ...
```

Use `scope: pipeline` when the break is the vendor pipeline itself (approval misconfigured, artifact feed empty, webhook missing) — not test flake.

## Workflow

1. Prefer targeted projects/suites when diagnosing a failure.
2. If you start the Aspire AppHost or **`dotnet run`** manually for local E2E, stop it with **Ctrl+C** in that terminal ([`.cursor/rules/dotnet-local-graceful-shutdown.mdc`](../../rules/dotnet-local-graceful-shutdown.mdc)); do not kill by PID.
3. Before **DevOps** queues proof pipelines, run the **local validation gate**: `dotnet test` for Unit + Integration + E2E and Playwright `local` project.
4. When red, write a **Failure Handoff** instead of vague prose.
5. After a fix, require evidence of unit/component regression before broad reruns.
6. Distinguish **local** vs **deployed** results clearly.
7. Do **not** hand off to DevOps until the local gate is green or the blocker is documented.

## Pipeline run URL gate (mandatory phases 1-2)

For every full-cycle vendor phase, the SDET must capture and stamp the **pipeline run URL** into the phase sidecar:

```bash
node scripts/stamp-pipeline-run.mjs docs/phase-evidence/phase-<stamp>-<n>.json \
  --url <https-pipeline-run-url> --runId <runOrBuildId>
```

The stamp writes `evidenceLinks.pipelineRunUrl` and re-runs `scripts/validate-phase-evidence.mjs`. Without this URL, the phase **cannot** record `phaseGateOutcome: passed`. See [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc) gate 4.

## Per-criterion evidence (phases 1-2)

The matrix has three criteria. Each one needs vendor-specific proof captured during the deploy window:

### `approval` (weight 40)

- **Phase 1 (GHA):** GitHub **Environment `production`** with required reviewer.
  - Trigger: `ci-cd-deploy.yml` deploy job blocks on the environment.
  - Evidence: `gh api repos/kmabery/pipeline-eval/actions/runs/<runId>/approvals` — record actor, comment, approved-at, and the run URL deep-linked to the approval review.
- **Phase 2 (ADO):** ADO **Environment `production`** approval check on the deployment job.
  - Trigger: ADO pipeline `pipeline-eval-cd` deploy stage blocks pending approval.
  - Evidence: `az pipelines runs show --id <buildId>` plus the **approval check** deep link in the run UI.

### `package` (weight 40)

- **Phase 1 (GHA):**
  - npm: `actions/setup-node@v6` `cache: npm` cache hit/miss line in the run logs; or GitHub Packages npm registry `npm publish --registry=https://npm.pkg.github.com`.
  - NuGet: GitHub Packages NuGet feed (`https://nuget.pkg.github.com/kmabery/index.json`).
- **Phase 2 (ADO):**
  - npm: Azure Artifacts npm feed restore log (`npm install` against `https://pkgs.dev.azure.com/ECI-LBMH/_packaging/<feed>/npm/registry/`).
  - NuGet: Azure Artifacts NuGet feed restore log (`dotnet restore` against the same feed).

Capture the relevant log excerpt (10-20 lines) showing the cache key / feed URL / package resolution.

### `webhook` (weight 20)

- **Phase 1 (GHA):** repo webhook (or `pipeline-webhook-notify.yml` job posting to `PIPELINE_MONITOR_WEBHOOK_URL`).
  - Evidence: `gh api repos/kmabery/pipeline-eval/hooks/<id>/deliveries` last delivery ID + status code + duration; or `pipeline-webhook-notify.yml` job summary.
- **Phase 2 (ADO):** ADO **service hook** for `Build completed`.
  - Evidence: `az devops invoke --area hooks --resource subscriptions` listing + the latest **notification** record (delivery URL, status, response code).

## Decision-matrix vendor-cell edits

Vendor phases 1-2 require the SDET validator to update the current vendor's column in `docs/decision-matrix/criteria.yaml` per the checklist in [`.cursor/skills/decision-matrix-evidence/SKILL.md`](../decision-matrix-evidence/SKILL.md). Record the criterion ids touched in the phase sidecar's `matrixEdits[]` (or set `matrixEdits: []` with a `matrixEditsNote` rationale). Phase 3 is **Analyst-only** — do not edit any vendor cell.

## Output template

```markdown
SDET evidence
- Local tests: dotnet test (Unit/Integration/E2E) -> ...; Playwright local -> ...
- Deployed smoke: ...
- Pipeline run: vendor=<gha|ado>, runId=..., runUrl=..., conclusion=...
- Criterion evidence:
  - approval: actor=..., approvalUrl=...
  - package: feed=..., evidenceUrl=...
  - webhook: hookId=..., deliveryId=..., status=...
- Matrix edits: [criterion ids] (or none + rationale)
- Allure / artifacts: ...
- All local gates green: true | false
- Failure handoffs: ...
- Known gaps: ...
```

## Contract alignment

- [`.cursor/rules/phase-closure-gate.mdc`](../../rules/phase-closure-gate.mdc)
- [`.cursor/rules/phase-gate-outcomes.mdc`](../../rules/phase-gate-outcomes.mdc)
- [`.cursor/rules/pipeline-vendor-phase.mdc`](../../rules/pipeline-vendor-phase.mdc)
- [`.cursor/skills/analyst-evaluation-report-evidence/SKILL.md`](../analyst-evaluation-report-evidence/SKILL.md)
- [`.cursor/skills/diagnose-deployed-apps/SKILL.md`](../diagnose-deployed-apps/SKILL.md) for live triage
