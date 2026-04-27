# Phase 2 — Azure DevOps (single-prompt launcher)

<!-- Generated from .cursor/plans/evaluation-topic.yaml by `cursorpack eval sync`. See .cursor/plans/eval-pack-generator.md. -->

Use as the **first message** in a main Agent chat. **Attach** [`../../cursor-orchestrator.md`](../../cursor-orchestrator.md) before sending. Phase 1 (GitHub Actions) must have a recorded outcome (`passed` or `failed_incomplete`) in `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-1.json` before this phase is meaningful. Rerunning this prompt replaces `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-2.json` and appends a new dated snapshot under `docs/evaluation-reports/`.

---

You are the Analyst Orchestrator. Start or redo **Phase 2 — Azure DevOps** for
pipeline-eval end-to-end. Attach `.cursor/cursor-orchestrator.md`.

Vendor lock (`.cursor/rules/pipeline-vendor-phase.mdc` and
`.cursor/rules/decision-matrix-authoring.mdc`): phase 2 = Azure DevOps only.
Do not edit `vendors.gha.*` in
`docs/decision-matrix/criteria.yaml`.

**Git before full cycle (mandatory when deploy proof is in scope):** At the **start** of a full-cycle phase pass, **commit and push** all intended work to **`main`** on **`git@github.com:kmabery/pipeline-eval.git`** (SSH). Both pipelines build the **same** GitHub commit; the ADO pipeline pulls source via the GitHub service connection. See [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../../skills/devops-github-actions-ci-aws/SKILL.md) §2a.

Preconditions: `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-1.json` exists and
`phase-auditor` has classified phase 1 as `Implemented`, `Verified`,
`Complete`, or `Failed (incomplete)`. Do not start phase 2 while phase 1
is still `In Progress`.

Delegate in this order using /name invocation:

0. **Main agent (Git)** — If this run is a **full delivery cycle** (not doc-only): stage, commit, and push to `main`; confirm `git status` is clean.

1. **/sdet-validator** — for EVERY criterion in
   `docs/decision-matrix/criteria.yaml` (`approval`, `package`, `webhook`),
   update `vendors.ado.{rating,label,citations,notes,updatedInPhase: 2}`.
   Use `docs/decision-matrix/evidence-guide.md` (Azure DevOps sections) for
   hypotheses; cite `learn.microsoft.com/azure/devops` primary doc URLs
   (Environment approvals, Azure Artifacts npm + NuGet feeds, Service hooks).
   Then `node scripts/validate-decision-matrix.mjs`.
2. **/backend-implementer** and **/react-implementer** — only if scope adds
   product code; otherwise skip.
3. **/sdet-validator** — local gate: same `dotnet test` and Playwright `local`
   suites as phase 1; produce a Failure Handoff if red.
4. **/devops-pipeline-operator** — queue and monitor pipeline
   **`pipeline-eval-cd`** in **`https://dev.azure.com/ECI-LBMH/LBMH-POC`**
   to terminal state. Record build id, run url, conclusion, and the
   **environment `production`** approval event (capture via
   `az pipelines runs show --id <buildId>` plus
   `az devops invoke --area pipelinepermissions --resource Approvals` or the
   web UI deep link).
5. **/sdet-validator** — deployed smoke against the URLs DevOps provides
   (same CloudFront + ALB; ADO deploys to the same AWS account); capture
   **artifact-feed proof** for `package` (Azure Artifacts npm + NuGet feed
   restore log), **service-hook delivery proof** for `webhook` (Azure DevOps
   service hook history for the build/release event), and **approval proof**
   for `approval` (the URL captured in step 4).

6. **Analyst finalization (you):**
   - `node scripts/validate-decision-matrix.mjs`
   - `node scripts/render-decision-matrix.mjs --target phase-plan`
   - `node scripts/render-decision-matrix.mjs --target index`
   - Create or refresh
     `docs/evaluation-reports/evaluation-report-MM-dd-yyyy-HHmmss-phase-2-ado-<buildId>.md`
     containing only these sections: `## Executive summary` (SHORT — problems and deltas vs. the previous run only), `## Link bundle`, and `## Rendered decision matrix` with a `matrix:begin` / `matrix:end` marker block.
     Then `node scripts/render-decision-matrix.mjs --target snapshot --path <that file> --phase 2` (emits a 4-column `Criterion | Weight | Evidence | Notes` table scoped to the ado column).
   - Populate `## Link bundle` with: Deployed API, Deployed web, ADO build URL, environment `production` approval URL, service-hook delivery URL, and a sample Azure Artifacts feed restore log line for the `package` criterion. **If the ADO build URL cannot be captured, set `phaseGateOutcome: failed_incomplete`** — the full phase run is not successful without it.
   - Write `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-2.json` (replace any prior phase **2** sidecar in `docs/phase-evidence/`):
     - `phase: 2`, `vendorPhase: "ado"`, `scope: "ADO_only"`.
     - `matrixEdits[]` = criterion ids whose ado rating moved off
       `tbd` (or `[]` + `matrixEditsNote`).
     - Standard gate fields.
     - `evidenceLinks.pipelineRunUrl` — non-empty HTTPS Azure DevOps build URL **required** for `phaseGateOutcome: passed`. Stamp it via
       `node scripts/stamp-pipeline-run.mjs docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-2.json --url <https-url> --runId <buildId>`.
   - `node scripts/validate-phase-evidence.mjs`.
   - Update `docs/evaluation-report.md` Latest archived report row.
7. **Evidence commit and push** — per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../../skills/devops-github-actions-ci-aws/SKILL.md) **§2b** (after the ADO pipeline is **terminal** and finalization is complete): **commit and push** `docs/phase-evidence/`, `docs/evaluation-reports/`, and `docs/evaluation-report.md` to **`main`**. **Doc-only:** still **§2b** after finalization. **Passed** and **failed_incomplete** both get pushed.

Do not edit phase-1/3 artifacts. Do not claim **Verified** or
**Complete** unless `.cursor/rules/phase-closure-gate.mdc` conditions
hold (including the **pipeline run URL** rule); otherwise set
`phaseGateOutcome: failed_incomplete`.
