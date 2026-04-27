# Cursor starter pack (generic)

This folder contains **project-agnostic** Cursor configuration: subagent definitions ([`agents/`](./agents/)), skills ([`skills/`](./skills/)), rules ([`rules/`](./rules/)), and workflow docs.

## Placeholders

Search and replace these tokens across `.cursor/` (and optionally copy into `docs/`) for your repository:

| Placeholder | Example |
|-------------|---------|
| `pipeline-eval` | `MyProduct.Api` |
| `github-actions` | `github-actions` (canonical CI for this repo) |
| `main` | `main` |
| `kmabery` | `my-org` |
| `git@github.com:kmabery/pipeline-eval.git` | `git@github.com:my-org/my-service.git` |
| `{{ADO_ORG}}` | `my-org` (Azure DevOps org — for **pipelines API** only) |
| `{{ADO_PROJECT}}` | `MyProject` |
| `CI-Deploy-main` | `CI-Deploy-Main` |
| `ci-cd-deploy.yml` | `ci-deploy.yml` or workflow display name |
| `{{GHA_WORKFLOW_FILE}}` | `.github/workflows/ci-deploy.yml` (optional) |
| `src/front-end` | `src/client` |
| `us-east-1` | `us-east-1` |

Infrastructure placeholders (`{{ECS_CLUSTER_NAME}}`, etc.) appear in skills that reference AWS CLI examples.

**Git** is assumed to be **GitHub** using **SSH** remotes — see [`rules/github-remote-ssh.mdc`](./rules/github-remote-ssh.mdc).

## Entry points

| Path | Purpose |
|------|---------|
| [`cursor-orchestrator.md`](./cursor-orchestrator.md) | **Attach first** in Agent chat — delegation order, gates, links to [Cursor Subagents](https://cursor.com/docs/subagents) |
| [`plans/`](./plans/) | **Plan templates** — per-phase single-prompt launchers under [`plans/phases/`](./plans/phases/), the [`plans/full-cycle-single-prompt.md`](./plans/full-cycle-single-prompt.md) ad-hoc prompt, the evaluation manifest [`plans/evaluation-topic.yaml`](./plans/evaluation-topic.yaml), and the [`plans/eval-pack-generator.md`](./plans/eval-pack-generator.md) generator contract. |
| [`plans/phases/`](./plans/phases/) | **Per-phase single-prompt launchers** (Coralogix → CloudWatch → Datadog → Final matrix). Start or redo any phase end-to-end. |
| [`plans/full-cycle-single-prompt.md`](./plans/full-cycle-single-prompt.md) | Generic ad-hoc full-cycle prompt (use `plans/phases/` for the vendor program) |
| [`agents/`](./agents/) | Subagent markdown (YAML frontmatter: `name`, `description`, optional `model`, `readonly`, `is_background`) |
| [`skills/`](./skills/) | Detailed procedures and checklists |
| [`rules/`](./rules/) | Glob or always-on rules that point agents at skills |
| [`rules/decision-matrix-authoring.mdc`](./rules/decision-matrix-authoring.mdc) | Write-lanes for `docs/decision-matrix/criteria.yaml`; renderer + validator gates |
| [`rules/observability-vendor-phase.mdc`](./rules/observability-vendor-phase.mdc) | Vendor-phase lock for the Coralogix vs CloudWatch vs Datadog program |
| [`rules/phase-gate-outcomes.mdc`](./rules/phase-gate-outcomes.mdc) | Deploy-cycle **Failed** vs **passed** for all `phase-MM-dd-yyyy-HHmmss-<n>.json`; pair with [`skills/phase-gate-self-heal/SKILL.md`](./skills/phase-gate-self-heal/SKILL.md) |

Optional: copy `cursor-orchestrator.md` into `docs/` if your team wants the workflow contract outside `.cursor/`.

## Third-party: Impeccable

[Impeccable](https://github.com/pbakaus/impeccable) is a design-language bundle for AI-assisted UI work: a core **`impeccable`** skill (reference files for typography, color, motion, layout, and more) plus separate skills for steering commands such as **`audit`**, **`critique`**, **`polish`**, **`distill`**, **`shape`**, and **`layout`**. It is licensed under [Apache 2.0](https://github.com/pbakaus/impeccable/blob/main/LICENSE); it builds on [Anthropic’s frontend-design skill](https://github.com/anthropics/skills/tree/main/skills/frontend-design). See upstream [NOTICE.md](https://github.com/pbakaus/impeccable/blob/main/NOTICE.md).

**Cursor setup:** Use the [Nightly](https://cursor.com/docs) channel and enable **Agent Skills** in Cursor Settings (see [Cursor Skills](https://cursor.com/docs/context/skills)). For React or front-end tasks, invoke skills by name or use slash-style workflows described in each skill (for example `/audit`, `/polish`).

**Refresh from upstream:** Clone or pull [pbakaus/impeccable](https://github.com/pbakaus/impeccable) and recopy `.cursor/skills/` into this folder, or download a bundle from [impeccable.style](https://impeccable.style).

## Install into another workspace

Prerequisites: **Python** 3.11+ and **[uv](https://docs.astral.sh/uv/)**. From the pack repository root, run `uv sync`, then:

```bash
uv run cursorpack -t /path/to/other/repo
```

For a guided flow (path, placeholders, primary Git branch, GitHub + CI provider, options, then first-session paths):

```bash
uv run cursorpack --wizard
```

Optional: `--copy-root-docs` / `--no-copy-root-docs` controls whether root docs (`README.md`, `cursor-orchestrator.md`, `full-cycle-single-prompt.md`) are copied (default: copy). The CLI uses **Questionary** and **Rich** for prompts when `--use-menu` is on (default). Set `CURSORPACK_TUI_DISABLE=1` for plain prompts and numbered multi-select, or pass `--no-use-menu`.

## Portable evaluation pack (any topic, not just observability)

This repo is configured for the **Coralogix vs CloudWatch vs Datadog** observability evaluation, but the `.cursor/` + `docs/` scaffold is designed to be re-templated for **other phased evaluations** (pipelines, gateways, AWS stacks, …). The topic is declared in [`./plans/evaluation-topic.yaml`](./plans/evaluation-topic.yaml) and consumed by the external [`cursorpack`](https://github.com/kmabery/cursorpack) CLI:

```bash
uv run cursorpack eval new    --topic pipelines --candidates "GitHub Actions,Azure Pipelines,GitLab CI" --target /path/to/repo
uv run cursorpack eval rename --from observability --to aws-stacks --target /path/to/repo
uv run cursorpack eval sync   --target /path/to/repo           # rewrite generated files from the manifest
uv run cursorpack eval sync   --target /path/to/repo --check   # CI drift guard
```

See [`./plans/eval-pack-generator.md`](./plans/eval-pack-generator.md) for the manifest schema, module layout, and regeneration safety rules. Generated files carry a `Generated from .cursor/plans/evaluation-topic.yaml` HTML-comment banner.

## Conventions

- **Subagents:** [Cursor docs — Subagents](https://cursor.com/docs/subagents)
- **Skills vs subagents:** Single-purpose checklists → skills; isolated long tasks → subagents (see orchestrator doc).
