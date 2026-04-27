---
name: scaffold-spruce-next-solution
description: Scaffolds a complete Spruce Next solution from scratch: Aspire AppHost, LBMH Spruce Next API services, LBMH Blazor FluentUI frontend, and AWS CDK C# IaC project. Use when creating a new Spruce Next solution, starting a new full-stack .NET 10 project, or setting up the standard LBMH solution structure with Aspire orchestration and AWS infrastructure.
---

# Scaffold a Spruce Next Solution

## Solution layout

```
{SolutionName}/
├── {SolutionName}.slnx
├── src/
│   ├── {SolutionName}.Host/          ← Aspire AppHost
│   ├── services/
│   │   └── {ServiceName}/            ← one folder per service
│   └── front-end/
│       └── {AppName}/
│           ├── {AppName}/            ← Blazor WASM host
│           └── {AppName}.Pages/      ← Razor class library
└── iac/
    └── {SolutionName}.Iac/           ← AWS CDK C# project
```

## Step 1 — Solution file

```bash
mkdir {SolutionName} && cd {SolutionName}
dotnet new sln -n {SolutionName}
dotnet sln migrate   # converts .sln → .slnx
```

## Step 2 — Aspire AppHost

```bash
dotnet new aspire-apphost -n {SolutionName}.Host -o src/{SolutionName}.Host
dotnet sln {SolutionName}.slnx add src/{SolutionName}.Host/{SolutionName}.Host.csproj
```

`AppHost.cs` starts empty — services and the frontend are registered here as they are added.

## Step 3 — Add services

For each API service use the `nextService` template. See the **add-spruce-next-service** skill for full details.

Quick summary per service:

```bash
dotnet new nextService -n {ServiceName} -o src/services/{ServiceName}
dotnet sln {SolutionName}.slnx add src/services/{ServiceName}/{ServiceName}.csproj
```

Then in AppHost:

```xml
<!-- {SolutionName}.Host.csproj -->
<ProjectReference Include="..\services\{ServiceName}\{ServiceName}.csproj" />
```

```csharp
// AppHost.cs
builder.AddProject<Projects.{ServiceName}>("kebab-case-name");
```

## Step 4 — Add Blazor frontend

Use the `lbmhui` template. See the **add-lbmh-blazor-frontend** skill for full details.

Quick summary:

```bash
dotnet new lbmhui -n {AppName} -o src/front-end/{AppName}
dotnet sln {SolutionName}.slnx add src/front-end/{AppName}/{AppName}/{AppName}.csproj
dotnet sln {SolutionName}.slnx add src/front-end/{AppName}/{AppName}.Pages/{AppName}.Pages.csproj
```

Then in AppHost:

```xml
<!-- {SolutionName}.Host.csproj -->
<ProjectReference Include="..\front-end\{AppName}\{AppName}\{AppName}.csproj" />
```

```csharp
// AppHost.cs  (Projects type name uses underscores for dots)
builder.AddProject<Projects.{AppName_underscored}>("kebab-case-name");
```

## Step 5 — IaC (AWS CDK C#)

```bash
mkdir -p iac/{SolutionName}.Iac
```

Create `iac/{SolutionName}.Iac/{SolutionName}.Iac.csproj` targeting `net10.0`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Amazon.CDK.Lib" Version="2.*" />
    <PackageReference Include="Constructs" Version="[10.0.0,11.0.0)" />
  </ItemGroup>
</Project>
```

Create `iac/{SolutionName}.Iac/cdk.json`:

```json
{
  "app": "dotnet run",
  "context": {
    "@aws-cdk/core:newStyleStackSynthesis": true,
    "@aws-cdk/aws-apigateway:usagePlanKeyOrderInsensitiveId": true,
    "@aws-cdk/aws-cloudfront:defaultSecurityPolicyTLSv1.2_2021": true
  }
}
```

Add to solution:

```bash
dotnet sln {SolutionName}.slnx add iac/{SolutionName}.Iac/{SolutionName}.Iac.csproj
```

### Standard stack structure

| Stack | Constructs |
|---|---|
| `EcsStack` | VPC, ECS Fargate cluster, ECR repos, ALBs per service |
| `FrontendStack` | Private S3 bucket, CloudFront with OAC (`S3BucketOrigin.WithOriginAccessControl`) |
| `ApiGatewayStack` | Cognito User Pool, Lambda Token Authorizer (JWT validation), REST API Gateway, API Key + Usage Plan |

In `Program.cs`, pass ALB references from `EcsStack` to `ApiGatewayStack` as cross-stack dependencies:

```csharp
var ecsStack = new EcsStack(app, "EcsStack", new StackProps { Env = env });
_ = new FrontendStack(app, "FrontendStack", new StackProps { Env = env });
_ = new ApiGatewayStack(app, "ApiGatewayStack", new ApiGatewayStackProps
{
    Env = env,
    ServiceOneAlb = ecsStack.ServiceOneAlb,
    // ... additional ALBs
});
```

## Step 6 — Deploy locally with Aspire

```bash
cd src/{SolutionName}.Host
dotnet run
```

Stop the AppHost with **Ctrl+C** in that same terminal (do not kill the `dotnet` PID). See [`.cursor/rules/dotnet-local-graceful-shutdown.mdc`](../../rules/dotnet-local-graceful-shutdown.mdc).

## Step 7 — Deploy to AWS

```bash
cd iac/{SolutionName}.Iac
cdk bootstrap   # first time only
cdk deploy --all
```

## Naming conventions

| Thing | Convention | Example |
|---|---|---|
| Solution / repo | PascalCase | `Spruce.Next.Sample` |
| Service project | PascalCase, no dots | `SampleServiceOne` |
| Blazor host app | dot-separated | `Spruce.Next.Sample` |
| Aspire resource name | kebab-case | `"sample-service-one"` |
| CDK stack ID | PascalCase + Stack | `SampleEcsStack` |
| AWS resource names | kebab-case | `spruce-next-sample-cluster` |
