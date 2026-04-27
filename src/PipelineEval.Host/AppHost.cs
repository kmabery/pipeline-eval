using PipelineEval.Host.Configuration;
using PipelineEval.Host.Diagnostics;
using PipelineEval.Host.Hosting;

var resolvedEnvPath = DotNetEnvBootstrap.LoadFromRepository(Directory.GetCurrentDirectory());
var ports = LocalPinnedPorts.FromEnvironment();

AspireEnvironmentConfigurator.ApplyPinnedPorts(ports);
AppHostStartupLogger.LogResolvedEnvAndPorts(resolvedEnvPath, ports);

StaleNodeDevPortReclaimer.TryReclaimWebPortIfStaleNode(ports);

if (!PortPreflightRunner.TryPass(ports))
    Environment.Exit(1);

var builder = DistributedApplication.CreateBuilder(args);
builder.AddPipelineEvalStack(ports);
builder.Build().Run();
