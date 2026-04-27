# Phase evidence sidecars

One JSON file per phase **run** under this directory: `phase-MM-dd-yyyy-HHmmss-<n>.json`. Each new run replaces the prior sidecar for the same phase number (the **MM-dd-yyyy-HHmmss** stamp is the freshest one). UTC time stamps. Validate with:

```bash
node scripts/validate-phase-evidence.mjs
```

## Required fields

| Field | Type | Notes |
|-------|------|-------|
| `phase` | integer | 1, 2, or 3 |
| `vendorPhase` | string | `"gha"`, `"ado"`, or `"finalMatrix"` |
| `scope` | string | `"GHA_only"`, `"ADO_only"`, or `"finalMatrix"` |
| `phaseGateOutcome` | string | `"passed"`, `"failed_incomplete"`, or `"not_applicable_doc_only"` |
| `phasePlanException` | string \| null | `"doc_only"` when the phase plan documents a doc-only exception |
| `deployCycleGate` | object | `{outcome: "passed" \| "failed", reasons: [string]}` |
| `evidenceLinks` | object | See below; `pipelineRunUrl` mandatory for vendor phases targeting full delivery |
| `devops` | object | Vendor-specific run/build info (see below) |
| `sdet` | object | Local + deployed test summary |
| `matrixEdits` | array of strings | Criterion ids whose vendor cell moved off `tbd` (or `[]` + `matrixEditsNote` rationale) |
| `synthesis` | object | Phase 3 only: `{weightedSignals, gapList, recommendation}` |
| `manualVerificationUrls` | object \| null | Optional: `{instructions, apiBaseUrl, webBaseUrl}` |

## `evidenceLinks` shape

```json
{
  "deployedApiUrl": "https://api.example.com",
  "deployedWebUrl": "https://web.example.com",
  "pipelineRunUrl": "https://github.com/kmabery/pipeline-eval/actions/runs/<runId>",
  "approvalEventUrl": "https://github.com/kmabery/pipeline-eval/actions/runs/<runId>#approval-<id>",
  "webhookDeliveryUrl": "https://github.com/kmabery/pipeline-eval/settings/hooks/<id>/deliveries/<deliveryId>"
}
```

`pipelineRunUrl` is **mandatory** (HTTPS) for vendor phases (1 and 2) targeting full delivery cycle; absence triggers `phaseGateOutcome: "failed_incomplete"` per [`../.cursor/rules/phase-closure-gate.mdc`](../.cursor/rules/phase-closure-gate.mdc) gate 4. Stamp via `node scripts/stamp-pipeline-run.mjs <sidecar.json> --url <url> --runId <id>`.

## `devops` shape

Phase 1 (GitHub Actions):

```json
{
  "githubActions": {
    "runId": "12345678",
    "runUrl": "https://github.com/kmabery/pipeline-eval/actions/runs/12345678",
    "conclusion": "success",
    "branch": "main",
    "commit": "<sha>"
  }
}
```

Phase 2 (Azure DevOps):

```json
{
  "azurePipelines": {
    "buildId": "234",
    "runUrl": "https://dev.azure.com/ECI-LBMH/LBMH-POC/_build/results?buildId=234",
    "conclusion": "succeeded",
    "branch": "main",
    "commit": "<sha>"
  }
}
```

## Sample (phase 1, GHA, passed)

```json
{
  "phase": 1,
  "vendorPhase": "gha",
  "scope": "GHA_only",
  "phaseGateOutcome": "passed",
  "phasePlanException": null,
  "deployCycleGate": { "outcome": "passed", "reasons": [] },
  "evidenceLinks": {
    "deployedApiUrl": "https://api.example.com",
    "deployedWebUrl": "https://d123abc.cloudfront.net",
    "pipelineRunUrl": "https://github.com/kmabery/pipeline-eval/actions/runs/12345678",
    "approvalEventUrl": "https://github.com/kmabery/pipeline-eval/actions/runs/12345678",
    "webhookDeliveryUrl": "https://github.com/kmabery/pipeline-eval/settings/hooks/9999/deliveries/abc123"
  },
  "devops": {
    "githubActions": {
      "runId": "12345678",
      "runUrl": "https://github.com/kmabery/pipeline-eval/actions/runs/12345678",
      "conclusion": "success",
      "branch": "main",
      "commit": "deadbeef"
    }
  },
  "sdet": {
    "localTests": { "status": "passed", "counts": { "unit": 1, "integration": 1, "e2e": 1, "playwrightLocal": 1 } },
    "deployedSmoke": { "status": "passed", "counts": { "playwrightDeployed": 1 } }
  },
  "matrixEdits": ["approval", "package", "webhook"],
  "synthesis": null,
  "manualVerificationUrls": null
}
```

## Sample (phase 1, GHA, failed_incomplete)

```json
{
  "phase": 1,
  "vendorPhase": "gha",
  "scope": "GHA_only",
  "phaseGateOutcome": "failed_incomplete",
  "phasePlanException": null,
  "deployCycleGate": {
    "outcome": "failed",
    "reasons": ["ci-cd-deploy.yml run not in success state", "evidenceLinks.pipelineRunUrl missing"]
  },
  "evidenceLinks": {
    "deployedApiUrl": null,
    "deployedWebUrl": null,
    "pipelineRunUrl": null,
    "approvalEventUrl": null,
    "webhookDeliveryUrl": null
  },
  "devops": { "githubActions": { "runId": null, "runUrl": null, "conclusion": "failure" } },
  "sdet": { "localTests": { "status": "passed" }, "deployedSmoke": { "status": "blocked" } },
  "matrixEdits": [],
  "synthesis": null,
  "manualVerificationUrls": {
    "instructions": "Resolve OIDC trust + re-run ci-cd-deploy.yml; rerun phase-1-gha launcher.",
    "apiBaseUrl": null,
    "webBaseUrl": null
  }
}
```

## Filename rules

- Stamp pattern: `MM-dd-yyyy-HHmmss` (UTC).
- Phase number: `1`, `2`, or `3`.
- Filename: `phase-MM-dd-yyyy-HHmmss-<n>.json` (e.g. `phase-04-27-2026-093500-1.json`).
- Rerunning a phase **replaces** the prior sidecar for that phase number — keep the freshest stamp; archive the previous one out of `phase-evidence/` if you need history.
