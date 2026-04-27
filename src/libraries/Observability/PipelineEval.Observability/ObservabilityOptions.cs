namespace PipelineEval.Observability;

/// <summary>
/// Binds configuration section <c>Observability</c>. See appsettings and <c>.env</c> mapping in <see cref="CoralogixEnvConfiguration"/>.
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>Coralogix / OTLP API key (never commit real values).</summary>
    public string ApiKey { get; set; } = "";

    public string ApplicationName { get; set; } = "spruce-next";

    public string SubSystem { get; set; } = "PipelineEval";

    /// <summary>OpenTelemetry service.name resource attribute.</summary>
    public string ServiceName { get; set; } = "PipelineEval.Api";

    /// <summary>When true, skip OTLP exporters and use console-friendly Serilog only.</summary>
    public bool UseLocal { get; set; } = true;

    /// <summary>OTLP gRPC endpoint (Coralogix regional ingress). Default matches US2 (<c>*.app.cx498.coralogix.com</c> tenants).</summary>
    public string OtlpEndpoint { get; set; } = "https://ingress.us2.coralogix.com:443";

    /// <summary>Optional extra OTLP headers (semicolon-separated key=value), merged with Authorization for the API key.</summary>
    public string? OtlpHeaders { get; set; }

    /// <summary>
    /// When <c>Observability:MapLogBody</c> / env overrides are not set, use OTLP <c>logRecord.body</c> as a key-value
    /// map with a <c>message</c> field (Coralogix Frequent Search) for non-local exports.
    /// </summary>
    public bool UseCoralogixFrequentSearchBodyShape { get; set; } = true;

    private readonly List<string> _meters = new();

    /// <summary>
    /// Application-level <c>System.Diagnostics.Metrics</c> meter names registered for OTLP export. Mirrors the
    /// <c>nextService</c> template's <c>observabilityOptions.AddMeter(name)</c> API so service projects can pass
    /// <c>SomeServiceMeterNames.Meters</c> through their <c>OpenTelemetryConfigurator</c>.
    /// </summary>
    public IReadOnlyList<string> Meters => _meters;

    /// <summary>
    /// Adds an application meter name. Trims whitespace, ignores null/empty, and dedupes case-insensitively.
    /// </summary>
    public ObservabilityOptions AddMeter(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return this;

        var trimmed = name.Trim();
        foreach (var existing in _meters)
        {
            if (string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase))
                return this;
        }

        _meters.Add(trimmed);
        return this;
    }
}
