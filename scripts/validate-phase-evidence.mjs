#!/usr/bin/env node
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import Ajv from 'ajv';
import addFormats from 'ajv-formats';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const evidenceDir = join(repoRoot, 'docs', 'phase-evidence');

const SIDECAR_RE = /^phase-\d{2}-\d{2}-\d{4}-\d{6}-\d+\.json$/;

const schema = {
  type: 'object',
  additionalProperties: true,
  required: ['phase', 'vendorPhase', 'scope', 'phaseGateOutcome', 'evidenceLinks', 'devops', 'sdet', 'observability'],
  properties: {
    phase: { type: 'integer', minimum: 1, maximum: 3 },
    vendorPhase: { type: 'string', enum: ['gha', 'ado', 'final'] },
    scope: { type: 'string', minLength: 1 },
    phaseGateOutcome: { type: 'string', enum: ['passed', 'failed_incomplete', 'in_progress', 'doc_only'] },
    fullCycleMode: { type: ['string', 'null'] },
    phasePlanException: { type: ['string', 'null'] },
    deployCycleGate: {
      type: 'object',
      required: ['outcome'],
      properties: {
        outcome: { type: 'string', enum: ['passed', 'failed', 'not_applicable'] },
        reasons: { type: 'array', items: { type: 'string' } },
      },
    },
    evidenceLinks: {
      type: 'object',
      required: ['pipelineRunUrl'],
      properties: {
        pipelineRunUrl: { type: ['string', 'null'], format: 'uri' },
        vendorEvidence: {
          type: 'array',
          items: {
            type: 'object',
            required: ['kind', 'url'],
            properties: {
              kind: { type: 'string' },
              url: { type: 'string', format: 'uri' },
            },
          },
        },
      },
    },
    manualVerificationUrls: {
      type: 'object',
      additionalProperties: true,
    },
    devops: { type: 'object' },
    sdet: { type: 'object' },
    observability: { type: 'object' },
    auditorProposedState: { type: ['string', 'null'] },
  },
};

const ajv = new Ajv({ allErrors: true, allowUnionTypes: true });
addFormats(ajv);
const validate = ajv.compile(schema);

let dirEntries;
try {
  dirEntries = readdirSync(evidenceDir);
} catch (err) {
  if (err.code === 'ENOENT') {
    console.log(`ok: ${evidenceDir} does not exist; nothing to validate`);
    process.exit(0);
  }
  console.error(`error: cannot read ${evidenceDir}: ${err.message}`);
  process.exit(2);
}

const sidecars = dirEntries.filter((n) => SIDECAR_RE.test(n));
if (sidecars.length === 0) {
  console.log(`ok: no phase-MM-dd-yyyy-HHmmss-<n>.json sidecars in ${evidenceDir} (vacuously valid)`);
  process.exit(0);
}

let failed = 0;
for (const name of sidecars) {
  const fpath = join(evidenceDir, name);
  if (!statSync(fpath).isFile()) continue;
  let parsed;
  try {
    parsed = JSON.parse(readFileSync(fpath, 'utf8'));
  } catch (err) {
    console.error(`error: ${name}: cannot parse JSON: ${err.message}`);
    failed++;
    continue;
  }
  const ok = validate(parsed);
  if (!ok) {
    failed++;
    console.error(`error: ${name}: schema violations:`);
    for (const e of validate.errors ?? []) {
      console.error(`  - ${e.instancePath || '/'} ${e.message}`);
    }
    continue;
  }
  if (parsed.phaseGateOutcome === 'passed' && parsed.vendorPhase !== 'final') {
    if (!parsed.evidenceLinks?.pipelineRunUrl) {
      console.error(`error: ${name}: phaseGateOutcome=passed requires evidenceLinks.pipelineRunUrl for non-final phases`);
      failed++;
      continue;
    }
  }
  console.log(`ok: ${name}`);
}

if (failed > 0) {
  console.error(`\n${failed}/${sidecars.length} sidecar(s) failed validation`);
  process.exit(1);
}
console.log(`\nok: ${sidecars.length} sidecar(s) validated`);
