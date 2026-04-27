using PipelineEval.Observability;

namespace PipelineEval.Api.Startup;

/// <summary>
/// Mirrors the <c>nextService</c> CLI template's <c>ConfigureOpenTelemetry(meterNames)</c> entry point but
/// targets <see cref="PipelineEval.Observability"/> (Serilog + OpenTelemetry OTLP, with the custom
/// Coralogix Frequent Search log-body shape) instead of the <c>LBMH.Observability</c> NuGet.
/// </summary>
public static class OpenTelemetryConfigurator
{
    public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder, IEnumerable<string> meterNames)
    {
        var names = meterNames as IReadOnlyCollection<string> ?? meterNames.ToList();

        builder.AddPipelineEvalObservability(opts =>
        {
            foreach (var name in names)
                opts.AddMeter(name);
        });

        return builder;
    }
}
