# Full-cycle single prompt (copy into Cursor)

Use as the **first message** in a main Agent chat for **ad-hoc** work. For the GHA vs ADO pipeline evaluation program, prefer the per-phase launchers under [`.cursor/plans/phases/`](./phases/) (one file per phase). **Attach** [`.cursor/cursor-orchestrator.md`](../cursor-orchestrator.md) first (and optionally `docs/phase-plan.md` if your repo uses it).

---

You are the **Analyst Orchestrator** for **pipeline-eval**. Follow [`.cursor/cursor-orchestrator.md`](../cursor-orchestrator.md) end-to-end — workflow contract, gates, and role prompts under [`.cursor/agents/`](../agents/).

**User request (scope):** *(paste the change or goal here.)*

**Canonical CI:** **GitHub Actions** (workflow **`ci-cd-deploy.yml`**). **Git:** push to **`main`** on **`git@github.com:kmabery/pipeline-eval.git`** ([`.cursor/rules/github-remote-ssh.mdc`](../rules/github-remote-ssh.mdc)).

**Mandatory sequence**

1. **Implement** — delegate to **Backend** and/or **Frontend** ([`.cursor/agents/backend-implementer.md`](../agents/backend-implementer.md), [`.cursor/agents/react-implementer.md`](../agents/react-implementer.md)) as needed; run local test gates per agent prompts.
2. **SDET (local)** — local E2E gate **and** local observability (logs/traces/metrics via Aspire dashboard); Failure Handoff if red ([`.cursor/agents/sdet-validator.md`](../agents/sdet-validator.md)).
3. **DevOps** — trigger or verify **ci-cd-deploy.yml** on **`git@github.com:kmabery/pipeline-eval.git`**; **monitor** to completion; all required jobs green ([`.cursor/agents/devops-github-actions-operator.md`](../agents/devops-github-actions-operator.md), [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../skills/devops-github-actions-ci-aws/SKILL.md)).
4. **AWS Solution Architect** — **architecture drift validation** with **AWS CLI** (read-only) against **`docs/architecture/`** and **`iac/terraform/`** after green CI ([`.cursor/agents/aws-solution-architect.md`](../agents/aws-solution-architect.md)); record pass/fail and CLI evidence for the Analyst handoff.
5. **SDET (deployed)** — deployed smoke/E2E **and** deploy-window observability or documented gap for that build ([`.cursor/agents/sdet-validator.md`](../agents/sdet-validator.md), [`.cursor/skills/sdet-phase-evidence/SKILL.md`](../skills/sdet-phase-evidence/SKILL.md)).
6. **Analyst** — update `docs/evaluation-report.md` (index), add or refresh a snapshot under `docs/evaluation-reports/`, and `docs/phase-evidence/phase-MM-dd-yyyy-HHmmss-<n>.json` (replace prior sidecar for that phase on rerun); do **not** use **Verified**/**Complete** until [`.cursor/rules/phase-closure-gate.mdc`](../rules/phase-closure-gate.mdc) is satisfied. If deploy gates cannot pass after [`.cursor/skills/phase-gate-self-heal/SKILL.md`](../skills/phase-gate-self-heal/SKILL.md), set **`phaseGateOutcome`: `failed_incomplete`** per [`.cursor/rules/phase-gate-outcomes.mdc`](../rules/phase-gate-outcomes.mdc) — not a successful deploy evaluation.
7. **Evidence commit and push** — after the pipeline (if any) is **terminal** and the Analyst step has written evidence, **commit and push** `docs/phase-evidence/`, `docs/evaluation-reports/`, and `docs/evaluation-report.md` to **`main`** per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../skills/devops-github-actions-ci-aws/SKILL.md) **§2b** (doc-only: push after Analyst finalization).

**Docs-optional:** If phase-plan does not apply, use this message as scope; the same CI and closure rules apply for any "deployed" or "complete" claim.

**Do not skip DevOps, AWS SA drift check, or SDET observability** when deploy evidence is in scope.
