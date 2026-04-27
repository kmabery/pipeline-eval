---
name: add-spruce-next-service
description: Scaffolds and wires up a new LBMH Spruce Next ASP.NET Core service using the `nextService` dotnet template. Use when adding a new API service, back-end microservice, or REST endpoint project to a Spruce Next solution. Handles template scaffolding, namespace renaming, Aspire AppHost registration, and solution file wiring.
---

# Add a Spruce Next Service

## Scaffold

```bash
dotnet new nextService -n {ServiceName} -o src/services/{ServiceName}
```

Creates a single project targeting `net10.0` with:

| Folder/File | Purpose |
|---|---|
| `Startup/SwaggerConfigurator.cs` | Registers Swagger/OpenAPI, enables Swagger UI in development |
| `Startup/OpenTelemetryConfigurator.cs` | Wires LBMH.Observability with meters via Coralogix |
| `Startup/ServiceRegistration.cs` | Registers service dependencies via `InjectServiceDependencies()` |
| `Startup/Map{ServiceName}Endpoints.cs` | Maps minimal API endpoints |
| `Services/I{ServiceName}Service.cs` + `.cs` | Service interface and primary implementation |
| `Context/{ServiceName}ContextSession.cs` | Structured log context (TransactionId, UserName, etc.) |
| `Meters/{ServiceName}MeterNames.cs` | OTel meter name constants |
| `Dockerfile` | Multi-stage Linux build using private Azure Artifacts feed |
| `nuget.config` | ECI-LBMH private feed + nuget.org |
| `appsettings.json` | Observability config block (update with real values) |

## After scaffolding — required changes

### 1. Fix the UserSecretsId

The template `.csproj` contains a hardcoded `UserSecretsId`. Regenerate it:

```bash
dotnet user-secrets init --project src/services/{ServiceName}/{ServiceName}.csproj
```

### 2. Update Observability config

Edit `appsettings.json` — set real values for your service:

```json
{
  "Observability": {
    "ApplicationName": "{ServiceName}",
    "SubSystem": "{ServiceName}",
    "ServiceName": "{ServiceName}",
    "ApiKey": "YOUR_CORALOGIX_API_KEY",
    "useLocal": false
  }
}
```

### 3. Rename endpoint map file

The template generates `MapSampleServiceEndpoints.cs`. Rename to `Map{ServiceName}Endpoints.cs` and update the class/method names to match your service.

### 4. Update the Dockerfile paths

The Dockerfile references `LBMH.SampleService` paths. Update both `dotnet restore` and `ENTRYPOINT` to use `{ServiceName}`.

## Wire up to Aspire AppHost

### AppHost `.csproj`

```xml
<ProjectReference Include="..\services\{ServiceName}\{ServiceName}.csproj" />
```

### `AppHost.cs`

```csharp
builder.AddProject<Projects.{ServiceName}>("kebab-case-service-name");
```

> The type name in `Projects.` uses the assembly name (dots replaced with underscores if any).

## Wire up to the solution

```bash
dotnet sln {SolutionName}.slnx add src/services/{ServiceName}/{ServiceName}.csproj
```

## Key patterns

- **All app configuration** belongs in `Startup/` as chained extension methods on `IHostApplicationBuilder` or `WebApplication`.
- **All endpoints** for the service are mapped in `Map{ServiceName}Endpoints.cs` using private helper methods per route.
- **Meters** are declared as constants in `{ServiceName}MeterNames.cs` and injected via `IMeterService`.
- **Structured logs** use `{ServiceName}ContextSession` with `BeginScope`.
- **New dependencies** are registered in `ServiceRegistration.cs` only.
