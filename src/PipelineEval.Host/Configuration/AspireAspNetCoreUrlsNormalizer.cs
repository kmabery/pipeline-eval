namespace PipelineEval.Host.Configuration;

internal static class AspireAspNetCoreUrlsNormalizer
{
    public static void ApplyToEnvironment()
    {
        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        var (newUrls, allowUnsecured) = Compute(urls);
        if (newUrls != null)
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", newUrls);
        if (allowUnsecured)
            EnvironmentVariableDefaults.Ensure("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
    }

    /// <summary>
    /// Pure logic for tests: reorder https before http; flag HTTP-only for Aspire.
    /// </summary>
    internal static (string? NewUrls, bool AllowUnsecuredTransport) Compute(string? urls)
    {
        if (string.IsNullOrWhiteSpace(urls))
            return (null, false);

        var parts = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var httpsParts = FilterHttps(parts);
        var httpParts = FilterHttp(parts);
        var otherParts = FilterOther(parts);

        if (httpsParts.Count > 0 && httpParts.Count > 0)
        {
            var reordered = httpsParts.Concat(httpParts).Concat(otherParts).ToList();
            if (!parts.SequenceEqual(reordered))
                return (string.Join(";", reordered), false);
            return (null, false);
        }

        if (httpParts.Count > 0 && httpsParts.Count == 0)
            return (null, true);

        return (null, false);
    }

    private static List<string> FilterHttps(List<string> parts) =>
        parts.Where(p => p.StartsWith("https://", StringComparison.OrdinalIgnoreCase)).ToList();

    private static List<string> FilterHttp(List<string> parts) =>
        parts.Where(p => p.StartsWith("http://", StringComparison.OrdinalIgnoreCase)).ToList();

    private static IEnumerable<string> FilterOther(List<string> parts) =>
        parts.Where(p =>
            !p.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !p.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
}
