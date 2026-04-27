namespace PipelineEval.Host.Configuration;

internal static class AspireEnvironmentConfigurator
{
    public static void ApplyPinnedPorts(LocalPinnedPorts ports)
    {
        var appHostUrls = AppHostUrlBuilder.BuildAppHostUrls(ports.AppHostHttps, ports.AppHostHttp);
        EnvironmentVariableDefaults.Ensure("ASPNETCORE_URLS", appHostUrls);
        EnvironmentVariableDefaults.Ensure(
            "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL",
            $"https://localhost:{ports.DashboardOtlp}");
        EnvironmentVariableDefaults.Ensure(
            "ASPIRE_DASHBOARD_MCP_ENDPOINT_URL",
            $"https://localhost:{ports.DashboardMcp}");
        EnvironmentVariableDefaults.Ensure(
            "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL",
            $"https://localhost:{ports.ResourceService}");
        AspireAspNetCoreUrlsNormalizer.ApplyToEnvironment();
    }
}
