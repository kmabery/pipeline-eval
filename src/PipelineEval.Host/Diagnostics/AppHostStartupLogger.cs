using PipelineEval.Host.Configuration;

namespace PipelineEval.Host.Diagnostics;

internal static class AppHostStartupLogger
{
    public static void LogResolvedEnvAndPorts(string? resolvedEnvPath, LocalPinnedPorts ports)
    {
        Console.WriteLine($"[AppHost] .env: {resolvedEnvPath ?? "(none)"}");
        Console.WriteLine(
            "[AppHost] ports -> "
            + $"api={ports.Api} web={ports.Web} localstack={ports.Localstack} "
            + $"apphost(https/http)={ports.AppHostHttps}/{ports.AppHostHttp} "
            + $"dashboard(otlp/mcp/resource)={ports.DashboardOtlp}/{ports.DashboardMcp}/{ports.ResourceService}");
    }
}
