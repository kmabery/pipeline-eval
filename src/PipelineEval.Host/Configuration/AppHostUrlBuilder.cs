namespace PipelineEval.Host.Configuration;

internal static class AppHostUrlBuilder
{
    public static string BuildAppHostUrls(int httpsPort, int httpPort)
    {
        var parts = new List<string>();
        if (httpsPort > 0)
            parts.Add($"https://localhost:{httpsPort}");
        if (httpPort > 0)
            parts.Add($"http://localhost:{httpPort}");
        if (parts.Count > 0)
            return string.Join(";", parts);
        return $"http://localhost:{(httpPort > 0 ? httpPort : 15053)}";
    }
}
