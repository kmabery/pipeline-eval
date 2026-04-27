#!/usr/bin/env node
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import yaml from 'js-yaml';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const criteriaPath = join(repoRoot, 'docs', 'decision-matrix', 'criteria.yaml');

const VENDORS = ['gha', 'ado'];
const VENDOR_PHASE = { gha: 1, ado: 2 };
const VALID_RATINGS = new Set(['pass', 'caveat', 'fail', 'tbd']);

const errors = [];
const warnings = [];

let raw;
try {
  raw = readFileSync(criteriaPath, 'utf8');
} catch (err) {
  console.error(`error: cannot read ${criteriaPath}: ${err.message}`);
  process.exit(2);
}

let doc;
try {
  doc = yaml.load(raw);
} catch (err) {
  console.error(`error: cannot parse ${criteriaPath}: ${err.message}`);
  process.exit(2);
}

if (!doc || typeof doc !== 'object') {
  errors.push('top-level YAML is not an object');
}

const criteria = Array.isArray(doc?.criteria) ? doc.criteria : null;
if (!criteria) {
  errors.push('expected `criteria:` to be a non-empty list');
}

let weightSum = 0;
const seenIds = new Set();

for (const [idx, c] of (criteria ?? []).entries()) {
  const where = `criteria[${idx}]${c?.id ? ` (id="${c.id}")` : ''}`;
  if (typeof c?.id !== 'string' || !c.id.trim()) {
    errors.push(`${where}: missing required string \`id\``);
    continue;
  }
  if (seenIds.has(c.id)) errors.push(`${where}: duplicate criterion id`);
  seenIds.add(c.id);

  for (const k of ['name', 'group', 'whyItMatters']) {
    if (typeof c[k] !== 'string' || !c[k].trim()) errors.push(`${where}: missing string \`${k}\``);
  }

  if (typeof c.weight !== 'number' || !Number.isFinite(c.weight)) {
    errors.push(`${where}: \`weight\` must be a number`);
  } else {
    weightSum += c.weight;
  }

  const v = c.vendors;
  if (!v || typeof v !== 'object') {
    errors.push(`${where}: missing \`vendors:\` block`);
    continue;
  }
  const keys = Object.keys(v);
  for (const k of VENDORS) {
    if (!keys.includes(k)) errors.push(`${where}: missing \`vendors.${k}\``);
  }
  for (const k of keys) {
    if (!VENDORS.includes(k)) errors.push(`${where}: unexpected vendor \`${k}\` (allowed: ${VENDORS.join(', ')})`);
  }
  for (const vk of VENDORS) {
    const cell = v[vk];
    if (!cell || typeof cell !== 'object') continue;
    const cellWhere = `${where}.vendors.${vk}`;
    const rating = cell.rating ?? 'tbd';
    if (!VALID_RATINGS.has(rating)) {
      errors.push(`${cellWhere}: rating="${rating}" not in {${[...VALID_RATINGS].join(', ')}}`);
    }
    const citations = Array.isArray(cell.citations) ? cell.citations : [];
    if (rating !== 'tbd' && citations.length === 0) {
      errors.push(`${cellWhere}: rating="${rating}" requires at least one URL in citations[]`);
    }
    if (rating !== 'tbd' && cell.updatedInPhase !== VENDOR_PHASE[vk]) {
      errors.push(
        `${cellWhere}: rating="${rating}" requires updatedInPhase=${VENDOR_PHASE[vk]} (vendor ${vk}); found ${cell.updatedInPhase ?? 'null'}`,
      );
    }
    if (rating === 'tbd' && cell.updatedInPhase != null) {
      warnings.push(`${cellWhere}: rating="tbd" but updatedInPhase=${cell.updatedInPhase} (should be null)`);
    }
    for (const [i, url] of citations.entries()) {
      if (typeof url !== 'string' || !/^https?:\/\//i.test(url)) {
        errors.push(`${cellWhere}.citations[${i}]: must be an http(s) URL`);
      }
    }
  }
}

if (criteria && Math.abs(weightSum - 100) > 0.001) {
  errors.push(`weights sum to ${weightSum}; must sum to 100`);
}

for (const w of warnings) console.warn(`warn: ${w}`);
for (const e of errors) console.error(`error: ${e}`);

if (errors.length) {
  console.error(`\n${errors.length} error(s) in ${criteriaPath}`);
  process.exit(1);
}
console.log(`ok: ${criteriaPath} (${(criteria ?? []).length} criteria, weights=${weightSum})`);
