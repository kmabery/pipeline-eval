---
name: aspire-multi-frontend
description: Sets up a .NET Aspire AppHost with PostgreSQL, an ASP.NET Core API service, and one or more JavaScript front-ends (React/Vite, Angular, Next.js) alongside a Blazor WASM front-end. Use when adding JavaScript front-ends to an Aspire orchestrator, wiring up AddJavaScriptApp, connecting a local Postgres data store, or setting up a multi-framework evaluation with a shared API backend.
---

# Aspire Multi-Frontend AppHost

Sets up an Aspire AppHost that orchestrates a PostgreSQL database, a .NET API service, and front-end apps in Blazor WASM, React (Vite), Angular, and/or Next.js.

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL container)
- Node.js 22.x (for JavaScript front-ends)
- Aspire workload: `dotnet workload install aspire`

## AppHost project setup

### .csproj packages

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.2.1">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.JavaScript" Version="13.2.0" />
    <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="13.2.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\services\{ServiceName}\{ServiceName}.csproj" />
    <ProjectReference Include="..\front-end\blazor\{BlazorApp}\{BlazorApp}.csproj" />
  </ItemGroup>
</Project>
```

Key: `Aspire.Hosting.JavaScript` (not the deprecated `Aspire.Hosting.NodeJs`) provides `AddJavaScriptApp()`.

### AppHost.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with a named database
var postgres = builder.AddPostgres("{prefix}-postgres").AddDatabase("{db-name}");

// API service with stable port for front-end consumption
var api = builder.AddProject<Projects.{ServiceName}>("{service-kebab}")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithHttpEndpoint(port: 5100, name: "api");

// Blazor WASM
builder.AddProject<Projects.{BlazorApp}>("{blazor-kebab}")
    .WithExternalHttpEndpoints();

// React (Vite) — default "dev" script, listens on 5173
builder.AddJavaScriptApp("{react-kebab}", "../front-end/react")
    .WithHttpEndpoint(targetPort: 5173)
    .WithExternalHttpEndpoints();

// Angular — "start" script runs ng serve, listens on 4200
builder.AddJavaScriptApp("{angular-kebab}", "../front-end/angular", "start")
    .WithHttpEndpoint(targetPort: 4200)
    .WithExternalHttpEndpoints();

// Next.js — default "dev" script, listens on 3000
builder.AddJavaScriptApp("{nextjs-kebab}", "../front-end/nextjs")
    .WithHttpEndpoint(targetPort: 3000)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

## API wiring per framework

Each front-end needs the API base URL. Pin the API to port 5100 with `.WithHttpEndpoint(port: 5100, name: "api")`.

### Blazor WASM

Reads from `wwwroot/appsettings.json` (and `appsettings.Development.json`):

```json
{
  "AppSettings": {
    "BaseApiUrl": "http://localhost:5100"
  }
}
```

Uses Refit for typed HTTP clients:

```csharp
var baseApiUrl = builder.Configuration.GetValue<string>("AppSettings:BaseApiUrl")
    ?? throw new InvalidOperationException("AppSettings:BaseApiUrl is not configured");

builder.Services.AddRefitClient<IProductProvider>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseApiUrl))
    .AddStandardResilienceHandler();
```

### React (Vite)

API client reads `VITE_API_URL` env var with fallback:

```typescript
const BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5100";
```

Also add a Vite dev server proxy in `vite.config.ts`:

```typescript
server: {
  port: 5173,
  proxy: { "/api": { target: "http://localhost:5100", changeOrigin: true } },
},
```

### Angular

Environment file (`src/environments/environment.ts`):

```typescript
export const environment = { production: false, apiUrl: 'http://localhost:5100' };
```

Plus `proxy.conf.json` for ng serve:

```json
{ "/api": { "target": "http://localhost:5100", "secure": false } }
```

### Next.js

API client reads `NEXT_PUBLIC_API_URL`:

```typescript
const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
```

If using Next.js 16+ with `next-pwa`, set the dev script to `next dev --webpack` to avoid the Turbopack conflict.

## CORS configuration

The API service must allow requests from all Aspire-assigned ports. In Development, use permissive CORS:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("https://your-cloudfront-domain.net")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
```

## PostgreSQL + EF Core data store

### DbContext registration

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("{db-name}")));
```

The connection string name must match the database name in `AddDatabase("{db-name}")`.

### Auto-migrate and seed on startup

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}
```

Generate the initial migration:

```bash
dotnet ef migrations add InitialCreate --project src/services/{ServiceName}
```

## Gotchas

- **Aspire.Hosting.NodeJs is deprecated** — use `Aspire.Hosting.JavaScript` (v13.2.0+)
- **`AddNpmApp` is removed in Aspire 13.x** — use `AddJavaScriptApp` instead
- **Blazor WASM `appsettings.Development.json`** — do NOT set `BaseApiUrl` to empty string `""`; it causes `UriFormatException` silently breaking all API calls
- **EF migrations must exist** — `MigrateAsync()` is a no-op without migrations; the seeder will crash on missing tables
- **Angular uses "start" not "dev"** — the npm script is `ng serve` mapped to `"start"`
- **Next.js 16+ Turbopack** — `next-pwa` webpack wrapper conflicts; use `next dev --webpack`
- **Port stability** — `.WithHttpEndpoint(port: 5100)` gives the API a fixed port so all front-end configs can hardcode it

## Run

```bash
cd src/{AppHostProject}
dotnet run
```

Stop the AppHost with **Ctrl+C** in that same terminal (do not kill the `dotnet` PID). See [`.cursor/rules/dotnet-local-graceful-shutdown.mdc`](../../rules/dotnet-local-graceful-shutdown.mdc).

Opens the Aspire dashboard showing all resources with clickable endpoint URLs.

## Related skills

- **add-spruce-next-service** — scaffold the .NET API service
- **add-lbmh-blazor-frontend** — scaffold the Blazor WASM front-end using the `lbmhui` template
- **scaffold-spruce-next-solution** — full solution structure with Aspire + CDK
