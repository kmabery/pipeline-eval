using PipelineEval.Api.Meters;
using PipelineEval.Api.Startup;
using PipelineEval.Observability;

CoralogixEnvConfiguration.LoadDotEnvIfPresent();

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureRuntimeConfiguration()
    .ConfigureOpenTelemetry(PipelineEvalApiMeterNames.Meters)
    .ConfigureOpenApi()
    .ConfigureAuthentication()
    .InjectServiceDependencies();

var app = builder.Build();

app.ConfigureOpenApiApp();
app.UseCors();
app.UseAuthenticationPipeline();

app.MapPipelineEvalHealthEndpoints();
app.MapPipelineEvalTodoEndpoints();
app.MapPipelineEvalInviteEndpoints();
app.MapPipelineEvalDevStorageEndpoints();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Integration test entry point for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.</summary>
public partial class Program;
