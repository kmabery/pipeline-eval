# Per-criterion evidence guide

For each criterion in [`criteria.yaml`](criteria.yaml), this document records the **hypothesis** going into the evaluation and the **reference URLs** SDET should cite. SDET overwrites `vendors.<vendor>.{rating,label,citations,notes,updatedInPhase}` per phase based on **observed** evidence from the actual pipeline runs, not just the references below.

---

## `approval` (weight 40, group `core`)

**Why it matters:** Production deploys must require a human reviewer; the system must record who approved, when, and why, and surface that as a traceable URL.

### GitHub Actions (phase 1)

- **Hypothesis:** GitHub **Environments** with required reviewers + wait timers + branch protection cover the contract.
- **References:**
  - [Using environments for deployment](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
  - [Environments — protection rules](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment#environment-protection-rules)
  - [REST: list environments / get environment](https://docs.github.com/en/rest/deployments/environments)
  - [REST: list deployment approvals for a run](https://docs.github.com/en/rest/actions/workflow-runs#list-required-reviewers-for-a-run)
- **Evidence to capture:** `gh api repos/kmabery/pipeline-eval/actions/runs/<runId>/approvals` JSON (actor, comment, approved-at) plus the deep-link review URL.

### Azure DevOps (phase 2)

- **Hypothesis:** ADO **Environment** approval check on the deployment job covers the contract; supports business-hours / branch-control / required-template checks alongside.
- **References:**
  - [Approvals and other checks](https://learn.microsoft.com/en-us/azure/devops/pipelines/process/approvals)
  - [Define approvals on environments](https://learn.microsoft.com/en-us/azure/devops/pipelines/process/environments)
  - [REST: Approvals - Get](https://learn.microsoft.com/en-us/rest/api/azure/devops/approvalsandchecks/approvals/get)
- **Evidence to capture:** `az pipelines runs show --id <buildId>` output + the **Approvals** deep link in the run UI.

---

## `package` (weight 40, group `core`)

**Why it matters:** Builds depend on **npm** and **NuGet** packages — first-party (this repo) and third-party. The vendor must offer reliable hosting/caching with the right auth model.

### GitHub Actions (phase 1)

- **Hypothesis:** **GitHub Packages** hosts npm + NuGet feeds; **`actions/setup-node@v6`** + `actions/cache` provide cache hits keyed on `package-lock.json`.
- **References:**
  - [GitHub Packages — npm registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-npm-registry)
  - [GitHub Packages — NuGet registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)
  - [Caching dependencies to speed up workflows](https://docs.github.com/en/actions/using-workflows/caching-dependencies-to-speed-up-workflows)
  - [`actions/setup-node` — caching](https://github.com/actions/setup-node#caching-global-packages-data)
- **Evidence to capture:** Cache hit/miss line from the run logs, plus a `npm view <pkg>` or `dotnet nuget list source` proof against `https://npm.pkg.github.com` / `https://nuget.pkg.github.com/kmabery/index.json`.

### Azure DevOps (phase 2)

- **Hypothesis:** **Azure Artifacts** hosts npm + NuGet feeds at `https://pkgs.dev.azure.com/ECI-LBMH/_packaging/<feed>/{npm|nuget}/`. Pipeline restore proves the feed works.
- **References:**
  - [Azure Artifacts overview](https://learn.microsoft.com/en-us/azure/devops/artifacts/start-using-azure-artifacts)
  - [npm feeds](https://learn.microsoft.com/en-us/azure/devops/artifacts/get-started-npm)
  - [NuGet feeds](https://learn.microsoft.com/en-us/azure/devops/artifacts/get-started-nuget)
  - [Pipeline caching](https://learn.microsoft.com/en-us/azure/devops/pipelines/release/caching)
- **Evidence to capture:** Restore log excerpt (10-20 lines) showing package resolution against the Azure Artifacts feed URL.

---

## `webhook` (weight 20, group `agents`)

**Why it matters:** Agents (and external dashboards / Slack / monitoring) react to pipeline state via webhooks. Reliable delivery + retries + introspection matter.

### GitHub Actions (phase 1)

- **Hypothesis:** Repo **webhooks** + **`workflow_run`** events provide a stable surface; `gh api hooks/<id>/deliveries` lets agents replay deliveries.
- **References:**
  - [About webhooks](https://docs.github.com/en/webhooks)
  - [Webhook events and payloads — `workflow_run`](https://docs.github.com/en/webhooks/webhook-events-and-payloads#workflow_run)
  - [REST: list deliveries](https://docs.github.com/en/rest/webhooks/repos#list-deliveries-for-a-repository-webhook)
  - Repo-local: [`pipelines/webhooks.md`](../../pipelines/webhooks.md), [`pipeline-webhook-notify.yml`](../../.github/workflows/pipeline-webhook-notify.yml)
- **Evidence to capture:** Hook id + last delivery id + status code + delivery URL.

### Azure DevOps (phase 2)

- **Hypothesis:** ADO **service hooks** subscribe to `Build completed` / `Run state changed` and POST to a target URL; the project lists notifications and response codes.
- **References:**
  - [Service hooks overview](https://learn.microsoft.com/en-us/azure/devops/service-hooks/overview)
  - [Subscribe to events](https://learn.microsoft.com/en-us/azure/devops/service-hooks/events)
  - [REST: Notifications - Get](https://learn.microsoft.com/en-us/rest/api/azure/devops/hooks/notifications/get)
- **Evidence to capture:** Subscription id + notification id + response code + notification URL.
