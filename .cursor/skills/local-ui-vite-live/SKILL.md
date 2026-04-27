---
name: local-ui-vite-live
description: >-
  Runs the PipelineEval React app with Vite dev server for fast refresh (HMR) and
  documents how ports and API proxy align with repo root `.env`. Use when iterating
  on local UI (CSS, components, layout), when the user asks for live reload, hot
  reload, Vite dev, or to verify changes in the browser without a production build.
---

# Local UI development with Vite (live update)

## Goal

Edit `src/front-end/PipelineEval.Web` and see changes in the browser **without** running `npm run build` each time. Vite provides **HMR** (hot module replacement) for TS/TSX/CSS; the dev server keeps running while files change.

## Ports and config (single source of truth)

- Repo root **`.env`** supplies **`LOCAL_WEB_PORT`** (default **5173**) and **`LOCAL_API_PORT`** (default **5101**).
- [`src/front-end/PipelineEval.Web/vite.config.ts`](../../../src/front-end/PipelineEval.Web/vite.config.ts) uses `loadEnv` from the **repo root** (`envDir` points there), binds the dev server to **`127.0.0.1:${LOCAL_WEB_PORT}`**, and **proxies `/api`** to `http://127.0.0.1:${LOCAL_API_PORT}`.

Open the UI at:

`http://127.0.0.1:<LOCAL_WEB_PORT>/`  
(use the same port shown in the AppHost log line `ports -> ... web=...` if you run the full stack).

## Mode A — Full stack (Aspire AppHost, recommended for API + auth + uploads)

1. From the repository root, start AppHost (starts API, Vite, dependencies per [`AppHost`](../../../src/PipelineEval.Host/AppHost.cs)):

   ```powershell
   dotnet run --project src/PipelineEval.Host
   ```

2. Wait until the distributed application is up and the web endpoint matches your `LOCAL_WEB_PORT`.
3. Change UI files under `src/front-end/PipelineEval.Web/src/**`. Save; the browser updates via HMR (or soft-refresh for some edge cases).

Use this mode when you need Cognito, presigned uploads, or the same wiring as CI.

## Mode B — Frontend only (Vite alone, faster UI-only loops)

Requires the **API** already running on `LOCAL_API_PORT` (e.g. AppHost running elsewhere, or API started standalone) **if** the page calls `/api`.

1. Ensure repo root `.env` exists (copy from `.env.example` if needed).
2. In a terminal:

   ```powershell
   cd src/front-end/PipelineEval.Web
   npm run dev
   ```

3. Vite prints the local URL; it must match **`LOCAL_WEB_PORT`** from `.env`.

If the API is not up, the shell may still run Vite, but API-backed screens will error until the backend is available.

## Agent / developer checklist

- [ ] Prefer **saving files** and relying on **HMR** rather than rebuilding for routine UI tweaks.
- [ ] Use **`npm run build`** when validating TypeScript + production bundle (CI parity), not for every edit.
- [ ] After finishing a session that started **Vite** or **AppHost**, follow [local-dev-teardown](../local-dev-teardown/SKILL.md) so ports are not left occupied.

## Troubleshooting (short)

| Symptom | Check |
|--------|--------|
| Port in use | Another `node` or old AppHost may hold `LOCAL_WEB_PORT`; see teardown skill or change the port in `.env`. |
| API 502 / proxy errors | API not listening on `LOCAL_API_PORT`; start AppHost or API. |
| Changes not appearing | Hard refresh once; confirm edits are under `PipelineEval.Web` and the dev server terminal shows HMR updates. |
