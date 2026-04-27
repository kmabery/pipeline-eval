#!/usr/bin/env node
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const argv = process.argv.slice(2);
if (argv.length < 3) {
  console.error('usage: stamp-pipeline-run.mjs <sidecar.json> --url <https-url> [--runId <id>]');
  process.exit(2);
}

const sidecarRel = argv[0];
let url = null;
let runId = null;
for (let i = 1; i < argv.length; i++) {
  if (argv[i] === '--url') url = argv[++i];
  else if (argv[i] === '--runId') runId = argv[++i];
  else {
    console.error(`error: unknown arg "${argv[i]}"`);
    process.exit(2);
  }
}

if (!url || !/^https?:\/\//i.test(url)) {
  console.error(`error: --url <https-url> is required and must be http(s)`);
  process.exit(2);
}

const sidecar = resolve(process.cwd(), sidecarRel);
let json;
try {
  json = JSON.parse(readFileSync(sidecar, 'utf8'));
} catch (err) {
  console.error(`error: cannot read ${sidecar}: ${err.message}`);
  process.exit(2);
}

json.evidenceLinks = json.evidenceLinks ?? {};
const before = json.evidenceLinks.pipelineRunUrl ?? null;
json.evidenceLinks.pipelineRunUrl = url;

if (runId) {
  json.devops = json.devops ?? {};
  if (json.vendorPhase === 'gha') {
    json.devops.githubActions = json.devops.githubActions ?? {};
    json.devops.githubActions.runId = runId;
    json.devops.githubActions.runUrl = url;
  } else if (json.vendorPhase === 'ado') {
    json.devops.azurePipelines = json.devops.azurePipelines ?? {};
    json.devops.azurePipelines.buildId = runId;
    json.devops.azurePipelines.buildUrl = url;
  }
}

writeFileSync(sidecar, JSON.stringify(json, null, 2) + '\n', 'utf8');

if (before === url) {
  console.log(`unchanged: pipelineRunUrl already set to ${url}`);
} else {
  console.log(`stamped: ${sidecar}\n  pipelineRunUrl: ${before ?? '(none)'} -> ${url}${runId ? `\n  runId: ${runId}` : ''}`);
}
