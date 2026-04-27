---
name: local-dev-teardown
description: >-
  After local testing (E2E, Playwright, Aspire AppHost, dotnet run), always stop
  long-running dev processes so ports and child services are not left running.
  Use when finishing a test session, closing a terminal task, or handing off after
  any work that started PipelineEval.Host, the API, or the Vite dev server.
---

# Local dev teardown (mandatory after testing)

## When this applies

- Any session that ran **`dotnet run --project src/PipelineEval.Host`**, **`dotnet test`** against E2E/AppHost fixtures, **`npm run test:e2e:local`**, or started **Vite** / **LocalStack** via Aspire.
- Before marking testing work complete or ending the agent turn.

## What to stop

| Process / role | Typical name (Windows) |
|----------------|-------------------------|
| Aspire AppHost | `PipelineEval.Host` |
| API (child of Aspire) | `PipelineEval.Api` |
| Standalone API run | `PipelineEval.Api` |

If you started **only** `dotnet run` on the API project, stop that process too.

## How to close (Windows PowerShell)

1. List remaining app processes:

   ```powershell
   Get-Process | Where-Object { $_.ProcessName -match '^PipelineEval\.' }
   ```

2. Stop them (replace IDs or use pipeline):

   ```powershell
   Get-Process -Name PipelineEval.Host,PipelineEval.Api -ErrorAction SilentlyContinue | Stop-Process -Force
   ```

3. Confirm nothing is left:

   ```powershell
   Get-Process | Where-Object { $_.ProcessName -match '^PipelineEval\.' }
   ```

If **dotnet.exe** is still running only as the IDE or unrelated tooling, do not kill it blindly; only stop processes clearly tied to this repo’s AppHost/API runs.

## Agent checklist

1. Run the verification `Get-Process` step after tests complete.
2. **Stop** any `PipelineEval.Host` / `PipelineEval.Api` still running.
3. Confirm **zero** `PipelineEval.*` processes before considering the task done.

## AppHost: stale Vite / Node on `LOCAL_WEB_PORT` (Windows)

On Windows, **`PipelineEval.Host`** tries to stop a leftover **`node.exe`** still listening on **`LOCAL_WEB_PORT`** in two places:

1. **When you stop the AppHost** (e.g. Ctrl+C): a hosted service runs during application shutdown and terminates that Node listener so the port is not left occupied.
2. **On the next `dotnet run`**: before port preflight, the AppHost attempts the same reclaim so a prior run does not block startup.

If the port is held by a **non-Node** process, preflight still fails; free it manually (below).

## Optional: ports

Pinned ports live in the repo root **`.env`** (`LOCAL_API_PORT`, `LOCAL_WEB_PORT`, etc.). If something still holds a port after processes exit, use `Get-NetTCPConnection -LocalPort <port>` to find the owning PID, then stop that process.
