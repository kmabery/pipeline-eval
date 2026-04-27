using PipelineEval.Observability;
using PipelineEval.SampleWindowsService;

CoralogixEnvConfiguration.LoadDotEnvIfPresent();
var builder = Host.CreateApplicationBuilder(args);
CoralogixEnvConfiguration.ApplyCoralogixEnvironment(builder.Configuration);
builder.AddPipelineEvalObservability();
builder.Services.AddHostedService<SampleWorker>();
builder.Services.AddWindowsService(o => o.ServiceName = "PipelineEval.SampleWindowsService");

await builder.Build().RunAsync().ConfigureAwait(false);
