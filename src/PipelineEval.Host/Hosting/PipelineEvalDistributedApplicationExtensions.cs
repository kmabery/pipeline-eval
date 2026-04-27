using Microsoft.Extensions.DependencyInjection;
using Projects;
using PipelineEval.Host;
using PipelineEval.Host.Configuration;
using PipelineEval.Host.Diagnostics;

namespace PipelineEval.Host.Hosting;

internal static class PipelineEvalDistributedApplicationExtensions
{
    public static void AddPipelineEvalStack(this IDistributedApplicationBuilder builder, LocalPinnedPorts ports)
    {
        builder.Services.AddSingleton(new LocalPinnedPortsHolder(ports));
        builder.Services.AddHostedService<DevStackPortCleanupHostedService>();
        builder.Services.AddHostedService<ApiEarlyExitWatcher>();
        var postgres = builder.AddPostgres("postgres").AddDatabase("pipelineeval");
        var localstack = builder.AddLocalStackContainer(ports);
        builder.AddPipelineEvalApi(ports, postgres, localstack);
        builder.AddPipelineEvalWebApp(ports);
    }

    private static IResourceBuilder<ContainerResource> AddLocalStackContainer(
        this IDistributedApplicationBuilder builder,
        LocalPinnedPorts ports) =>
        builder.AddContainer("localstack", "localstack/localstack", "4")
            .WithEnvironment("SERVICES", "s3,cognito-idp")
            .WithEnvironment("SKIP_SSL_CERT_DOWNLOAD", "1")
            .WithHttpEndpoint(port: ports.Localstack, targetPort: 4566, name: "s3");

    private static void AddPipelineEvalApi(
        this IDistributedApplicationBuilder builder,
        LocalPinnedPorts ports,
        IResourceBuilder<PostgresDatabaseResource> postgres,
        IResourceBuilder<ContainerResource> localstack)
    {
        var s3ServiceUrl = $"http://localhost:{ports.Localstack}";
        builder.AddProject<Projects.PipelineEval_Api>("pipeline-eval-api")
            .WithReference(postgres)
            .WaitFor(postgres)
            .WaitFor(localstack)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
            .WithEnvironment("S3__ServiceUrl", s3ServiceUrl)
            .WithEnvironment("S3__BucketName", "pipelineeval-local")
            .WithEnvironment("S3__Region", "us-east-1")
            .WithHttpEndpoint(port: ports.Api, name: "api");
    }

    private static void AddPipelineEvalWebApp(this IDistributedApplicationBuilder builder, LocalPinnedPorts ports) =>
        builder.AddJavaScriptApp("pipeline-eval-web", "../front-end/PipelineEval.Web")
            .WithHttpEndpoint(
                targetPort: ports.Web,
                port: ports.Web,
                name: "web",
                env: null,
                isProxied: false)
            .WithExternalHttpEndpoints();
}
