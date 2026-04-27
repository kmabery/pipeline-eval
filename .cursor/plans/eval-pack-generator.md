# Portable evaluation pack generator — `cursorpack eval`

This document describes the **external** Python (uv-managed) CLI subcommand that makes this repo's `.cursor/` + `docs/` evaluation scaffold portable to other topics — pipelines (GitHub Actions vs Azure Pipelines vs GitLab CI), gateways (YARP vs Envoy vs API Gateway), AWS stacks (CDK vs Terraform vs SAM), and others we haven't identified yet.

**Work-product location.** The generator lives in the external [`cursorpack`](https://github.com/kmabery/cursorpack) Python package referenced from [`.cursor/README.md`](../README.md). This repo contributes only the **manifest** ([`evaluation-topic.yaml`](evaluation-topic.yaml)), the **generated artifacts** (marked with a `Generated from .cursor/plans/evaluation-topic.yaml` HTML-comment banner), and this contract.

## Why portable

The current pack was designed around the Coralogix vs CloudWatch vs Datadog observability evaluation, but the same scaffold (phased single-prompt launchers, rating model, decision matrix, phase-closure gates, Analyst-only finalization) works for any **N-candidate, phased evaluation**. Swapping topics should not require find-and-replace across `.cursor/**` and `docs/**`.

## Manifest contract

Single source of truth: [`.cursor/plans/evaluation-topic.yaml`](evaluation-topic.yaml).

Required keys (see the file for full schema):

- `topic.id`, `topic.slug`, `topic.title`, `topic.shortTitle`, `topic.verifyByDate`
- `candidates[]` — each with `id`, `displayName`, `phase` (unique integer ≥ 1), optional `expertBlock` pointer into `.cursor/agents/sdet-validator.md`
- `finalMatrix.phase` — equals `max(candidates[].phase) + 1`
- `ratingModel` — default `pass-caveat-fail-tbd` with `icon` / `score` / `meaning` per value
- `weightTotal` — integer; criterion weights in `criteria.yaml` must sum to this
- `generated[]` — files the generator owns (rewritten on `sync`)
- `handAuthored[]` — files the generator leaves alone (criterion CRUD, agent bodies, phase-evidence skill)
- `deliverables` — topic-specific required evidence (observability: dashboards / SLIs / SLOs / alerts / rumReleases; future topics: build-time / rollback drills / cost ceilings / etc.)

The generator **validates** the manifest before writing anything:

- `weightTotal` is a positive integer.
- `candidates[].phase` values are unique and form a contiguous sequence starting at 1.
- `finalMatrix.phase == len(candidates) + 1`.
- `topic.slug` is kebab-case and matches the filenames in `generated.skills` / `generated.rules`.

## Subcommand surface

```bash
# Scaffold a new evaluation in a fresh repo
uv run cursorpack eval new \
  --topic pipelines \
  --title "CI/CD pipelines (GitHub Actions vs Azure Pipelines vs GitLab CI)" \
  --candidates "GitHub Actions,Azure Pipelines,GitLab CI" \
  --target /path/to/repo

# Re-template this repo for a different topic (keeps hand-authored files,
# rewrites generated files, leaves criteria.yaml and evaluation-report.md alone)
uv run cursorpack eval rename --from observability --to aws-stacks --target /path/to/repo

# Re-render generated files from the current manifest (idempotent;
# the default `new` run is `sync` under the hood).
uv run cursorpack eval sync --target /path/to/repo

# Fail if generated files drift from the manifest (CI gate)
uv run cursorpack eval sync --target /path/to/repo --check
```

## Module layout (inside the external `cursorpack` repo)

```
src/cursorpack/eval/
  __init__.py
  manifest.py          # pydantic models for evaluation-topic.yaml
  generator.py         # orchestrates templating + writes
  templates/
    phase-launcher.md.jinja
    candidate-phase.mdc.jinja
    evaluation-phases.SKILL.md.jinja
    phase-plan.md.jinja
    decision-matrix-README.md.jinja
    criteria.yaml.jinja      # only for `eval new`; `sync` never overwrites criteria.yaml
tests/eval/
  test_manifest.py     # round-trip + validation failures
  test_generator.py    # render observability manifest, diff against this repo
  fixtures/
    observability/    # manifest + expected output tree
    pipelines/        # manifest + expected output tree
    aws-stacks/       # manifest + expected output tree
```

## Initial seed: observability round-trip

1. Copy this repo's `.cursor/plans/evaluation-topic.yaml` into `tests/eval/fixtures/observability/manifest.yaml`.
2. Snapshot the current `.cursor/plans/phases/`, `.cursor/rules/observability-vendor-phase.mdc`, `.cursor/skills/observability-evaluation-phases/SKILL.md`, `docs/phase-plan.md`, and `docs/decision-matrix/README.md` into `tests/eval/fixtures/observability/expected/`.
3. `test_generator.py` asserts that running the generator against the manifest produces the expected tree byte-for-byte.
4. Any deliberate change to the templates must update the fixture in the same commit.

## Regeneration safety

- `sync` runs in two passes: **render to a temp tree**, **diff**, then atomically replace.
- **Git bookends** — hand-authored workflow docs in this repo require **§2a** (pre-pipeline commit) and **§2b** (post-evidence commit of `docs/phase-evidence/`, `docs/evaluation-reports/`, `docs/evaluation-report.md`) per [`.cursor/skills/devops-github-actions-ci-aws/SKILL.md`](../skills/devops-github-actions-ci-aws/SKILL.md). When updating `cursorpack` templates (`phase-launcher.md.jinja`, `evaluation-phases.SKILL.md.jinja`), keep that pair so `eval sync` does not drop the second gate.
- Files in `handAuthored[]` are never touched.
- `criteria.yaml` (criterion CRUD + weights) is in `handAuthored` — only the Analyst adds / removes / reweights rows.
- The HTML-comment banner (`<!-- Generated from .cursor/plans/evaluation-topic.yaml ... -->`) anchors the idempotency check: files without it are treated as `new` and written; files with it are rewritten only inside the banner section.

## Topic playbook — swapping observability for something else

1. Copy this repo (or start from a fresh pack install).
2. Edit `.cursor/plans/evaluation-topic.yaml`: new `topic.id`, new `candidates[]` (with fresh phase numbers), new `deliverables`.
3. Replace the rows in `docs/decision-matrix/criteria.yaml` (analyst lane).
4. Replace the **Phase 1 / 2 / 3 expert blocks** in `.cursor/agents/sdet-validator.md` with the new topic's tool commands (`hand-authored`, manual swap).
5. Run `uv run cursorpack eval sync --target .` — generated phase launchers, rules, and skills are rewritten with the new candidate names and phase wording.
6. Run `node scripts/validate-decision-matrix.mjs` and `node scripts/render-decision-matrix.mjs --target phase-plan` to refresh the rendered matrix.

## Related

- [`.cursor/README.md`](../README.md) — placeholders and `cursorpack` install instructions.
- [`evaluation-topic.yaml`](evaluation-topic.yaml) — the manifest for this repo.
- [`.cursor/agents/sdet-validator.md`](../agents/sdet-validator.md) — hand-authored, carries the per-candidate expert blocks.
- [`docs/decision-matrix/criteria.yaml`](../../docs/decision-matrix/criteria.yaml) — criterion CRUD stays Analyst-owned even when the topic is re-templated.
