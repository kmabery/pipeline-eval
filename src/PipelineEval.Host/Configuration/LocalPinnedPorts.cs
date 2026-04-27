namespace PipelineEval.Host.Configuration;

internal readonly record struct LocalPinnedPorts(
    int Localstack,
    int Api,
    int Web,
    int AppHostHttps,
    int AppHostHttp,
    int DashboardOtlp,
    int DashboardMcp,
    int ResourceService)
{
    public static LocalPinnedPorts FromEnvironment() =>
        new(
            LocalPortParser.Parse("LOCAL_LOCALSTACK_PORT", 4566),
            LocalPortParser.Parse("LOCAL_API_PORT", 5101),
            LocalPortParser.Parse("LOCAL_WEB_PORT", 5173),
            LocalPortParser.Parse("LOCAL_APPHOST_HTTPS_PORT", 17000),
            LocalPortParser.Parse("LOCAL_APPHOST_HTTP_PORT", 15053),
            LocalPortParser.Parse("LOCAL_DASHBOARD_OTLP_PORT", 21224),
            LocalPortParser.Parse("LOCAL_DASHBOARD_MCP_PORT", 23009),
            LocalPortParser.Parse("LOCAL_RESOURCE_SERVICE_PORT", 22234));

    public IEnumerable<(string Name, int Port)> EnumerateForPreflight()
    {
        yield return ("LOCAL_API_PORT", Api);
        yield return ("LOCAL_WEB_PORT", Web);
        yield return ("LOCAL_LOCALSTACK_PORT", Localstack);
        yield return ("LOCAL_APPHOST_HTTPS_PORT", AppHostHttps);
        yield return ("LOCAL_APPHOST_HTTP_PORT", AppHostHttp);
        yield return ("LOCAL_DASHBOARD_OTLP_PORT", DashboardOtlp);
        yield return ("LOCAL_DASHBOARD_MCP_PORT", DashboardMcp);
        yield return ("LOCAL_RESOURCE_SERVICE_PORT", ResourceService);
    }
}
