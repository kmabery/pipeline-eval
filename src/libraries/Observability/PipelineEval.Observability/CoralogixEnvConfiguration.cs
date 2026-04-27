using System.Collections;
using Microsoft.Extensions.Configuration;

namespace PipelineEval.Observability;

/// <summary>
/// Loads <c>.env</c> from the current working directory (if present) and maps
/// <c>CORALOGIX_*</c> variables into the <see cref="ObservabilityOptions.SectionName"/> configuration keys.
/// </summary>
public static class CoralogixEnvConfiguration
{
    public const string EnvApiKey = "CORALOGIX_API_KEY";
    public const string EnvApplication = "CORALOGIX_APPLICATION";
    public const string EnvSubsystem = "CORALOGIX_SUBSYSTEM";
    /// <summary>Regional OTLP gRPC URL (e.g. US2 <c>https://ingress.us2.coralogix.com:443</c> for <c>*.app.cx498.coralogix.com</c>).</summary>
    public const string EnvOtlpEndpoint = "CORALOGIX_OTLP_ENDPOINT";
    /// <summary>Flips <c>Observability:UseLocal</c>. Accepts <c>true/false</c> or <c>1/0</c>. When false, the library exports OTLP to <see cref="EnvOtlpEndpoint"/> with bearer auth from <see cref="EnvApiKey"/>.</summary>
    public const string EnvUseLocal = "CORALOGIX_USE_LOCAL";
    /// <summary>Overrides <c>Observability:ServiceName</c> (OpenTelemetry <c>service.name</c> resource attribute).</summary>
    public const string EnvServiceName = "CORALOGIX_SERVICE_NAME";
    /// <summary>When set to true/false or 1/0, maps to <c>Observability:MapLogBody</c> to force Coralogix map body on or off (overrides <see cref="ObservabilityOptions.UseCoralogixFrequentSearchBodyShape"/> heuristics).</summary>
    public const string EnvMapLogBody = "CORALOGIX_MAP_LOG_BODY";

    /// <summary>Loads <c>.env</c> into the process environment when the file exists.</summary>
    public static void LoadDotEnvIfPresent()
    {
        try
        {
            DotNetEnv.Env.TraversePath().Load();
        }
        catch
        {
            // Missing or invalid .env should not crash startup; rely on appsettings / env vars already set.
        }
    }

    /// <summary>
    /// Copies CORALOGIX_* from the environment into <c>Observability:*</c> when set (env wins over appsettings for those keys).
    /// Call after <see cref="Microsoft.Extensions.Hosting.HostApplicationBuilder.Configuration"/> is available.
    /// </summary>
    public static void ApplyCoralogixEnvironment(IConfigurationBuilder configurationBuilder)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var apiKey = Environment.GetEnvironmentVariable(EnvApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey))
            data[$"{ObservabilityOptions.SectionName}:ApiKey"] = apiKey;

        var app = Environment.GetEnvironmentVariable(EnvApplication);
        if (!string.IsNullOrWhiteSpace(app))
            data[$"{ObservabilityOptions.SectionName}:ApplicationName"] = app;

        var sub = Environment.GetEnvironmentVariable(EnvSubsystem);
        if (!string.IsNullOrWhiteSpace(sub))
            data[$"{ObservabilityOptions.SectionName}:SubSystem"] = sub;

        var otlp = Environment.GetEnvironmentVariable(EnvOtlpEndpoint);
        if (!string.IsNullOrWhiteSpace(otlp))
            data[$"{ObservabilityOptions.SectionName}:OtlpEndpoint"] = otlp;

        var useLocal = Environment.GetEnvironmentVariable(EnvUseLocal);
        if (TryParseBool(useLocal, out var useLocalParsed))
            data[$"{ObservabilityOptions.SectionName}:UseLocal"] = useLocalParsed ? "true" : "false";

        var serviceName = Environment.GetEnvironmentVariable(EnvServiceName);
        if (!string.IsNullOrWhiteSpace(serviceName))
            data[$"{ObservabilityOptions.SectionName}:ServiceName"] = serviceName;

        var mapLog = Environment.GetEnvironmentVariable(EnvMapLogBody);
        if (TryParseBool(mapLog, out var mapLogParsed))
            data[$"{ObservabilityOptions.SectionName}:MapLogBody"] = mapLogParsed ? "true" : "false";

        if (data.Count == 0)
            return;

        configurationBuilder.AddInMemoryCollection(data);
    }

    private static bool TryParseBool(string? raw, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var trimmed = raw.Trim();
        if (bool.TryParse(trimmed, out value))
            return true;
        if (trimmed == "1") { value = true; return true; }
        if (trimmed == "0") { value = false; return true; }
        return false;
    }
}
