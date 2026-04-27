# Phase 1 — GitHub Actions (single-prompt launcher)

<!-- Generated from .cursor/plans/evaluation-topic.yaml by `cursorpack eval sync`. See .cursor/plans/eval-pack-generator.md. -->

Use as the **first message** in a main Agent chat. **Attach** [`../../cursor-orchestrator.md`](../../cursor-orchestrator.md) (and optionally [`../../../docs/phase-plan.md`](../../../docs/phase-plan.md)) before sending. Rerunning this prompt replaces `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-1.json` (fresh date/time stamp) and appends a new dated snapshot under `docs/evaluation-reports/`.

---

You are the Analyst Orchestrator. Start or redo **Phase 1 — GitHub Actions** for
pipeline-eval end-to-end. Attach `.cursor/cursor-orchestrator.md`.

Vendor lock (`.cursor/rules/pipeline-vendor-phase.mdc` and
`.cursor/rules/decision-matrix-authoring.mdc`): phase 1 = GitHub Actions only.
Do not edit `vendors.ado.*` in
`docs/decision-matrix/criteria.yaml`.

**Git before full cycle (mandatory when deploy proof is in scope):** At the **start** of a full-cycle phase pass, **commit and push** all intended work to **`main`** on **`git@github.com:kmabery/pipeline-eval.git`** (SSH). Leave a **clean working tree** (or only ignorable artifacts) before local SDET and before `ci-cd-deploy.yml`. Canonical CI validates the **remote** `main` revision, not uncommitted local files. See [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../../skills/devops-github-actions-ci-aws/SKILL.md) §2a.

Delegate in this order using /name invocation:

0. **Main agent (Git)** — If this run is a **full delivery cycle** (not doc-only): stage, commit, and push to `main`; confirm `git status` is clean. Do not proceed to SDET or DevOps until this is done.

1. **/sdet-validator** — for EVERY criterion in
   `docs/decision-matrix/criteria.yaml` (`approval`, `package`, `webhook`),
   update `vendors.gha.{rating,label,citations,notes,updatedInPhase: 1}`.
   Rating = `pass` | `caveat` | `fail` (or `tbd` with explicit reason in
   `notes`). Use `docs/decision-matrix/evidence-guide.md` for hypotheses
   and reference URLs (GitHub Environments protection rules; GitHub Packages
   for npm + NuGet; GitHub repo webhooks). Then run
   `node scripts/validate-decision-matrix.mjs` until clean.
2. **/backend-implementer** and **/react-implementer** — only if the user's
   scope adds product code; otherwise skip.
3. **/sdet-validator** — local gate: `dotnet test` against `tests/PipelineEval.UnitTests`,
   `tests/PipelineEval.IntegrationTests`, `tests/PipelineEval.E2ETests`, plus
   Playwright `local` project under `src/front-end/PipelineEval.Web/tests/e2e`.
   Failure Handoff if red.
4. **/devops-github-actions-operator** — trigger and monitor
   `ci-cd-deploy.yml` on `git@github.com:kmabery/pipeline-eval.git`
   (branch `main`) to terminal state. Record run id, run url, conclusion,
   and the **environment `production`** approval event (capture the actor
   and timestamp via `gh api repos/kmabery/pipeline-eval/actions/runs/<runId>/approvals`).
5. **/sdet-validator** — deployed smoke against the URLs DevOps provides
   (CloudFront web + ALB API), capture **artifact-feed proof** for
   `package` (npm cache hit / setup-node logs), **webhook-delivery proof**
   for `webhook` (`gh api repos/kmabery/pipeline-eval/hooks/<id>/deliveries`
   or the `pipeline-webhook-notify.yml` summary), and **approval proof**
   for `approval` (the URL captured in step 4).

6. **Analyst finalization (you):**
   - `node scripts/validate-decision-matrix.mjs`
   - `node scripts/render-decision-matrix.mjs --target phase-plan`
   - `node scripts/render-decision-matrix.mjs --target index`
   - Create or refresh
     `docs/evaluation-reports/evaluation-report-MM-dd-yyyy-HHmmss-phase-1-gha-<runId>.md`
     containing only these sections: `## Executive summary` (SHORT — problems and deltas vs. the previous run only), `## Link bundle`, and `## Rendered decision matrix` with a `matrix:begin` / `matrix:end` marker block.
     Then `node scripts/render-decision-matrix.mjs --target snapshot --path <that file> --phase 1` (emits a 4-column `Criterion | Weight | Evidence | Notes` table scoped to the gha column).
   - Populate `## Link bundle` with: Deployed API, Deployed web, GitHub Actions run URL, environment `production` approval URL, repo webhook delivery URL, and a sample setup-node cache log line for the `package` criterion. **If the GitHub Actions run URL cannot be captured, set `phaseGateOutcome: failed_incomplete`** — the full phase run is not successful without it.
   - Write `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-1.json` (replace any prior phase **1** sidecar in `docs/phase-evidence/`):
     - `phase: 1`, `vendorPhase: "gha"`, `scope: "GHA_only"`.
     - `matrixEdits[]` = criterion ids whose gha rating moved off `tbd`
       (or `[]` plus `matrixEditsNote` if you only reworded labels).
     - Standard gate fields (`phaseGateOutcome`, `deployCycleGate`,
       `evidenceLinks`, `devops`, `sdet`, `observability`).
     - `evidenceLinks.pipelineRunUrl` — non-empty HTTPS GitHub Actions run URL **required** for `phaseGateOutcome: passed`. Stamp it via
       `node scripts/stamp-pipeline-run.mjs docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-1.json --url <https-url> --runId <runId>`.
   - `node scripts/validate-phase-evidence.mjs`.
   - Update `docs/evaluation-report.md` Latest archived report row.
7. **Evidence commit and push** — per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../../skills/devops-github-actions-ci-aws/SKILL.md) **§2b** (after `ci-cd-deploy` is **terminal** and the Analyst finalization above is complete): **commit and push** `docs/phase-evidence/`, `docs/evaluation-reports/`, and `docs/evaluation-report.md` to **`main`**. **Doc-only:** if this run skips deploy, still **§2b** after finalization. Applies to **passed** and **failed_incomplete**.

Do not claim **Verified** or **Complete** unless
`.cursor/rules/phase-closure-gate.mdc` conditions hold (including the **pipeline run URL** rule); otherwise set
`phaseGateOutcome: failed_incomplete` in the phase **1** sidecar (`phase-MM-dd-yyyy-HHmmss-1.json`) and Failed
(incomplete) in the snapshot. Do not edit phase-2/3 artifacts.
