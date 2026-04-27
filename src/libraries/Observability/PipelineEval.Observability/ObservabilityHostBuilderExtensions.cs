using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.OpenTelemetry;
using PipelineEval.Observability.CoralogixOtel;

namespace PipelineEval.Observability;

/// <summary>
/// Registers Serilog and OpenTelemetry (OTLP). Routes traces, metrics, and logs to either the local
/// Aspire dashboard (when <c>Observability:UseLocal=true</c> and <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set)
/// or to a Coralogix-compatible OTLP ingress with bearer auth (when <c>UseLocal=false</c> and
/// <c>Observability:ApiKey</c> is set).
/// </summary>
public static class ObservabilityHostBuilderExtensions
{
    /// <summary>Standard OpenTelemetry env var Aspire injects into AppHost-managed projects.</summary>
    private const string OtelExporterOtlpEndpointEnv = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>Adds Serilog + OpenTelemetry for ASP.NET Core (includes ASP.NET instrumentation).</summary>
    public static WebApplicationBuilder AddPipelineEvalObservability(this WebApplicationBuilder builder) =>
        AddPipelineEvalObservabilityCore(builder, includeAspNetCoreInstrumentation: true, configure: null);

    /// <summary>
    /// Adds Serilog + OpenTelemetry for ASP.NET Core. The <paramref name="configure"/> callback runs against the
    /// bootstrap <see cref="ObservabilityOptions"/> (after binding from configuration) and is also registered with
    /// <see cref="OptionsServiceCollectionExtensions.Configure{TOptions}(IServiceCollection, Action{TOptions})"/>
    /// so consumers reading <c>IOptions&lt;ObservabilityOptions&gt;</c> see the same value.
    /// </summary>
    public static WebApplicationBuilder AddPipelineEvalObservability(this WebApplicationBuilder builder, Action<ObservabilityOptions>? configure) =>
        AddPipelineEvalObservabilityCore(builder, includeAspNetCoreInstrumentation: true, configure: configure);

    /// <summary>Adds Serilog + OpenTelemetry for generic hosts (worker / Windows Service; no ASP.NET instrumentation).</summary>
    public static HostApplicationBuilder AddPipelineEvalObservability(this HostApplicationBuilder builder) =>
        AddPipelineEvalObservabilityCore(builder, includeAspNetCoreInstrumentation: false, configure: null);

    /// <summary>Adds Serilog + OpenTelemetry for generic hosts with a bootstrap <see cref="ObservabilityOptions"/> mutator.</summary>
    public static HostApplicationBuilder AddPipelineEvalObservability(this HostApplicationBuilder builder, Action<ObservabilityOptions>? configure) =>
        AddPipelineEvalObservabilityCore(builder, includeAspNetCoreInstrumentation: false, configure: configure);

    private static TBuilder AddPipelineEvalObservabilityCore<TBuilder>(TBuilder builder, bool includeAspNetCoreInstrumentation, Action<ObservabilityOptions>? configure)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection(ObservabilityOptions.SectionName));
        if (configure is not null)
            builder.Services.Configure(configure);

        // SSM/Parameter-Store fallback. Amazon.Extensions.Configuration.SystemsManager only translates
        // '/' -> ':' (it leaves '__' literal). Mirror the workaround in ConnectionStringHelper so an
        // SSM parameter named "Observability__ApiKey" still binds to ObservabilityOptions.ApiKey.
        builder.Services.PostConfigure<ObservabilityOptions>(opts => ApplyDoubleUnderscoreFallback(opts, builder.Configuration));

        builder.Services.AddSerilog(
            (services, loggerConfiguration) =>
            {
                var configuration = services.GetRequiredService<IConfiguration>();
                var monitor = services.GetService<IOptionsMonitor<ObservabilityOptions>>();
                var opts = monitor?.CurrentValue
                    ?? configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
                    ?? new ObservabilityOptions();
                ApplyDoubleUnderscoreFallback(opts, configuration);

                loggerConfiguration
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty(OtelLogProperties.Application, opts.ApplicationName)
                    .Enrich.WithProperty(OtelLogProperties.Subsystem, opts.SubSystem);

                loggerConfiguration.WriteTo.Console();

                if (TryGetLogOtlpTarget(opts, out var endpoint, out var headers))
                {
                    if (ShouldUseCoralogixMapLogBody(configuration, opts))
                        WriteCoralogixMapBodyOpenTelemetrySink(loggerConfiguration, opts, endpoint, headers);
                    else
                    {
                        loggerConfiguration.WriteTo.OpenTelemetry(
                            configure: o =>
                            {
                                o.Endpoint = endpoint;
                                o.Protocol = OtlpProtocol.Grpc;
                                if (headers is { Count: > 0 })
                                    o.Headers = headers;
                                o.ResourceAttributes = new Dictionary<string, object>
                                {
                                    ["service.name"] = opts.ServiceName,
                                    [OtelResourceAttributes.ApplicationName] = opts.ApplicationName,
                                    [OtelResourceAttributes.SubsystemName] = opts.SubSystem,
                                };
                            });
                    }
                }
            });

        var bootstrapOpts = builder.Configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>() ?? new ObservabilityOptions();
        ApplyDoubleUnderscoreFallback(bootstrapOpts, builder.Configuration);
        configure?.Invoke(bootstrapOpts);

        if (ShouldRegisterOtel(bootstrapOpts))
        {
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(rb => ConfigureResource(rb, bootstrapOpts))
                .WithTracing(t =>
                {
                    if (includeAspNetCoreInstrumentation)
                        t.AddAspNetCoreInstrumentation();
                    t.AddHttpClientInstrumentation()
                        .AddOtlpExporter(e => ConfigureOtlpExporter(e, bootstrapOpts));
                })
                .WithMetrics(m =>
                {
                    if (includeAspNetCoreInstrumentation)
                        m.AddAspNetCoreInstrumentation();
                    m.AddRuntimeInstrumentation();
                    foreach (var meterName in bootstrapOpts.Meters)
                        m.AddMeter(meterName);
                    m.AddOtlpExporter(e => ConfigureOtlpExporter(e, bootstrapOpts));
                });
        }

        return builder;
    }

    /// <summary>
    /// When <c>opts.ApiKey</c> / endpoint / app / subsystem / service-name is empty, fall back to the
    /// double-underscore literal keys that the AWS Systems Manager configuration provider emits
    /// (e.g. <c>Observability__ApiKey</c>). Mirrors <see cref="PipelineEval.Api.ConnectionStringHelper"/>.
    /// </summary>
    private static void ApplyDoubleUnderscoreFallback(ObservabilityOptions opts, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
            opts.ApiKey = configuration["Observability__ApiKey"] ?? opts.ApiKey;
        if (string.IsNullOrWhiteSpace(opts.OtlpEndpoint))
            opts.OtlpEndpoint = configuration["Observability__OtlpEndpoint"] ?? opts.OtlpEndpoint;
        if (string.IsNullOrWhiteSpace(opts.ApplicationName))
            opts.ApplicationName = configuration["Observability__ApplicationName"] ?? opts.ApplicationName;
        if (string.IsNullOrWhiteSpace(opts.SubSystem))
            opts.SubSystem = configuration["Observability__SubSystem"] ?? opts.SubSystem;
        if (string.IsNullOrWhiteSpace(opts.ServiceName))
            opts.ServiceName = configuration["Observability__ServiceName"] ?? opts.ServiceName;

        var useLocalRaw = configuration["Observability__UseLocal"];
        if (!string.IsNullOrWhiteSpace(useLocalRaw)
            && bool.TryParse(useLocalRaw, out var useLocalParsed)
            // Only override when the bound value is the default and there is no explicit
            // section value (otherwise we'd flip an intentionally-set Observability:UseLocal).
            && string.IsNullOrWhiteSpace(configuration["Observability:UseLocal"]))
        {
            opts.UseLocal = useLocalParsed;
        }
    }

    /// <summary>
    /// Decides whether to register OpenTelemetry tracing/metrics. Two valid destinations:
    /// (a) Coralogix when <c>UseLocal=false</c> and an <c>ApiKey</c> exists; or
    /// (b) the local Aspire dashboard when <c>UseLocal=true</c> and Aspire injected
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> into the process.
    /// </summary>
    private static bool ShouldRegisterOtel(ObservabilityOptions opts)
    {
        if (!opts.UseLocal && !string.IsNullOrWhiteSpace(opts.ApiKey))
            return true;
        if (opts.UseLocal && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OtelExporterOtlpEndpointEnv)))
            return true;
        return false;
    }

    private static void ConfigureOtlpExporter(OpenTelemetry.Exporter.OtlpExporterOptions e, ObservabilityOptions opts)
    {
        if (opts.UseLocal)
        {
            // Let the OTel SDK read OTEL_EXPORTER_OTLP_ENDPOINT / _PROTOCOL / _HEADERS from the env
            // vars Aspire AppHost injects into the project. Do not override here.
            return;
        }
        e.Endpoint = new Uri(opts.OtlpEndpoint);
        e.Headers = MergeOtlpHeaders(opts);
    }

    /// <summary>
    /// Resolve the Serilog OTLP-sink target. Returns false (skip) when no destination is configured.
    /// </summary>
    private static bool TryGetLogOtlpTarget(
        ObservabilityOptions opts,
        out string endpoint,
        out IDictionary<string, string>? headers)
    {
        endpoint = string.Empty;
        headers = null;

        if (!opts.UseLocal && !string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            endpoint = opts.OtlpEndpoint;
            headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {opts.ApiKey}",
            };
            return true;
        }

        if (opts.UseLocal)
        {
            var aspire = Environment.GetEnvironmentVariable(OtelExporterOtlpEndpointEnv);
            if (!string.IsNullOrWhiteSpace(aspire))
            {
                endpoint = aspire;
                return true;
            }
        }

        return false;
    }

    private static void ConfigureResource(ResourceBuilder rb, ObservabilityOptions opts)
    {
        rb.AddService(
            serviceName: opts.ServiceName,
            serviceVersion: typeof(ObservabilityHostBuilderExtensions).Assembly.GetName().Version?.ToString());

        rb.AddAttributes(new Dictionary<string, object>
        {
            [OtelResourceAttributes.ApplicationName] = opts.ApplicationName,
            [OtelResourceAttributes.SubsystemName] = opts.SubSystem,
        });
    }

    private static string MergeOtlpHeaders(ObservabilityOptions opts)
    {
        var pairs = new List<string> { $"Authorization=Bearer {opts.ApiKey}" };
        if (!string.IsNullOrWhiteSpace(opts.OtlpHeaders))
        {
            foreach (var part in opts.OtlpHeaders.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                pairs.Add(part);
        }

        return string.Join(",", pairs);
    }

    /// <summary>
    /// Coralogix Frequent Search: OTLP <c>body</c> as a map with a <c>message</c> key (see <c>CORALOGIX_MAP_LOG_BODY</c> / <c>Observability:MapLogBody</c>).
    /// </summary>
    private static bool ShouldUseCoralogixMapLogBody(IConfiguration configuration, ObservabilityOptions opts)
    {
        foreach (var key in new[] { "Observability:MapLogBody", "Observability__MapLogBody" })
        {
            var raw = configuration[key];
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (bool.TryParse(raw, out var b)) return b;
            if (string.Equals(raw.Trim(), "1", StringComparison.Ordinal)) return true;
            if (string.Equals(raw.Trim(), "0", StringComparison.Ordinal)) return false;
        }

        var fromEnv = Environment.GetEnvironmentVariable(CoralogixEnvConfiguration.EnvMapLogBody);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            if (bool.TryParse(fromEnv, out var eb)) return eb;
            if (string.Equals(fromEnv.Trim(), "1", StringComparison.Ordinal)) return true;
            if (string.Equals(fromEnv.Trim(), "0", StringComparison.Ordinal)) return false;
        }

        if (opts.UseLocal) return false;
        return opts.UseCoralogixFrequentSearchBodyShape;
    }

    private static void WriteCoralogixMapBodyOpenTelemetrySink(
        LoggerConfiguration loggerConfiguration,
        ObservabilityOptions opts,
        string endpoint,
        IDictionary<string, string>? headers)
    {
        var h = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers is { Count: > 0 })
        {
            foreach (var p in headers)
                h[p.Key] = p.Value;
        }

        const IncludedData coralogixIncludedData =
            IncludedData.MessageTemplateTextAttribute |
            IncludedData.TraceIdField |
            IncludedData.SpanIdField;

        var ver = typeof(ObservabilityOptions).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        var resourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = opts.ServiceName,
            [OtelResourceAttributes.ApplicationName] = opts.ApplicationName,
            [OtelResourceAttributes.SubsystemName] = opts.SubSystem,
            ["telemetry.sdk.name"] = "opentelemetry",
            ["telemetry.sdk.language"] = "csharp",
            ["telemetry.sdk.version"] = ver,
        };

        var httpHandler = new SocketsHttpHandler { ActivityHeadersPropagator = null };
        var exporter = new CoralogixOtelGrpcLogExporter(endpoint, h, httpHandler);
        var logSink = new CoralogixOpenTelemetryLogsSink(exporter, null, resourceAttributes, coralogixIncludedData);
        loggerConfiguration.WriteTo.Sink(
            logSink,
            new BatchingOptions
            {
                EagerlyEmitFirstEvent = true,
                BatchSizeLimit = 1000,
                BufferingTimeLimit = TimeSpan.FromSeconds(2),
                QueueLimit = 100000,
            });
    }
}
