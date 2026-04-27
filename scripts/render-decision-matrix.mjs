#!/usr/bin/env node
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import yaml from 'js-yaml';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const criteriaPath = join(repoRoot, 'docs', 'decision-matrix', 'criteria.yaml');

const args = parseArgs(process.argv.slice(2));
if (!args.target) {
  console.error('usage: render-decision-matrix.mjs --target {phase-plan|index|snapshot} [--path FILE] [--phase N] [--final]');
  process.exit(2);
}

const VENDORS = [
  { key: 'gha', label: 'GitHub Actions' },
  { key: 'ado', label: 'Azure DevOps' },
];

const doc = yaml.load(readFileSync(criteriaPath, 'utf8'));
const criteria = doc?.criteria ?? [];

let block;
if (args.target === 'snapshot') {
  if (!args.path) {
    console.error('error: --target snapshot requires --path FILE');
    process.exit(2);
  }
  if (args.final) {
    block = renderFinalMatrix(criteria);
  } else {
    if (!args.phase || ![1, 2].includes(Number(args.phase))) {
      console.error('error: --target snapshot requires --phase {1|2} (or --final for phase 3)');
      process.exit(2);
    }
    block = renderPhaseMatrix(criteria, Number(args.phase));
  }
} else if (args.target === 'phase-plan') {
  block = renderFullMatrix(criteria);
  args.path = join(repoRoot, 'docs', 'phase-plan.md');
} else if (args.target === 'index') {
  block = renderFullMatrix(criteria);
  args.path = join(repoRoot, 'docs', 'evaluation-report.md');
} else {
  console.error(`error: unknown target "${args.target}"`);
  process.exit(2);
}

const filePath = resolve(repoRoot, args.path);
let body;
try {
  body = readFileSync(filePath, 'utf8');
} catch (err) {
  console.error(`error: cannot read ${filePath}: ${err.message}`);
  process.exit(2);
}

const updated = replaceBetweenMarkers(body, block);
if (updated === body) {
  console.log(`unchanged: ${filePath}`);
} else {
  writeFileSync(filePath, updated, 'utf8');
  console.log(`rendered: ${filePath} (target=${args.target}${args.phase ? `, phase=${args.phase}` : ''}${args.final ? ', final' : ''})`);
}

function parseArgs(argv) {
  const out = { target: null, path: null, phase: null, final: false };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--target') out.target = argv[++i];
    else if (a === '--path') out.path = argv[++i];
    else if (a === '--phase') out.phase = argv[++i];
    else if (a === '--final') out.final = true;
    else {
      console.error(`error: unknown arg "${a}"`);
      process.exit(2);
    }
  }
  return out;
}

function replaceBetweenMarkers(body, block) {
  const begin = '<!-- matrix:begin -->';
  const end = '<!-- matrix:end -->';
  const re = new RegExp(`${escapeRe(begin)}[\\s\\S]*?${escapeRe(end)}`);
  if (!re.test(body)) {
    console.error(`error: file is missing ${begin} / ${end} marker pair`);
    process.exit(2);
  }
  return body.replace(re, `${begin}\n${block}\n${end}`);
}

function escapeRe(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function renderFullMatrix(criteria) {
  const header = `| Criterion | Weight | Why it matters | ${VENDORS.map((v) => v.label).join(' | ')} |`;
  const sep = `|---|---|---|${VENDORS.map(() => '---').join('|')}|`;
  const rows = criteria.map((c) => {
    const cells = VENDORS.map((v) => formatVendorCell(c.vendors?.[v.key]));
    return `| \`${c.id}\` | ${c.weight} | ${escapeMd(c.whyItMatters ?? '')} | ${cells.join(' | ')} |`;
  });
  return [header, sep, ...rows].join('\n');
}

function renderPhaseMatrix(criteria, phase) {
  const vendor = phase === 1 ? VENDORS[0] : VENDORS[1];
  const header = '| Criterion | Weight | Evidence | Notes |';
  const sep = '|---|---|---|---|';
  const rows = criteria.map((c) => {
    const cell = c.vendors?.[vendor.key] ?? {};
    const rating = cell.rating ?? 'tbd';
    const label = cell.label ? `${rating} — ${cell.label}` : rating;
    const cites = (cell.citations ?? []).map((u) => `[link](${u})`).join('<br/>') || '_(none)_';
    const notes = escapeMd(cell.notes ?? '');
    return `| \`${c.id}\` | ${c.weight} | ${cites} | ${label}${notes ? `<br/>${notes}` : ''} |`;
  });
  return [`Vendor: **${vendor.label}** (phase ${phase})`, '', header, sep, ...rows].join('\n');
}

function renderFinalMatrix(criteria) {
  const matrix = renderFullMatrix(criteria);
  const signals = computeWeightedSignals(criteria);
  const sigHeader = '| Vendor | Weighted signal |';
  const sigSep = '|---|---|';
  const sigRows = VENDORS.map((v) => `| ${v.label} | ${signals[v.key].toFixed(2)} |`);
  return [matrix, '', '### Weighted signals (final)', '', sigHeader, sigSep, ...sigRows].join('\n');
}

function computeWeightedSignals(criteria) {
  const score = { pass: 1.0, caveat: 0.5, fail: 0.0, tbd: 0.0 };
  const out = { gha: 0, ado: 0 };
  for (const c of criteria) {
    for (const v of VENDORS) {
      const r = c.vendors?.[v.key]?.rating ?? 'tbd';
      out[v.key] += (c.weight ?? 0) * (score[r] ?? 0);
    }
  }
  out.gha /= 100;
  out.ado /= 100;
  return out;
}

function formatVendorCell(cell) {
  if (!cell) return '_n/a_';
  const rating = cell.rating ?? 'tbd';
  const label = cell.label ? `${rating} — ${cell.label}` : rating;
  const cites = (cell.citations ?? []).map((u) => `[link](${u})`).join(' ');
  return cites ? `${label} ${cites}` : label;
}

function escapeMd(s) {
  return String(s ?? '').replace(/\|/g, '\\|').replace(/\n/g, '<br/>');
}
