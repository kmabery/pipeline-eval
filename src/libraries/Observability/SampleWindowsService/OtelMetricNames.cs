namespace PipelineEval.SampleWindowsService;

/// <summary>Metric names aligned with <c>src/libraries/sample-md/metrics.md</c>.</summary>
internal static class OtelMetricNames
{
    public const string MeterName = "PipelineEval.SampleWindowsService";

    public const string TickCountName = "pipelineeval.sample_windows.ticks";

    public const string SubsystemValue = "sample-windows-service";
}
