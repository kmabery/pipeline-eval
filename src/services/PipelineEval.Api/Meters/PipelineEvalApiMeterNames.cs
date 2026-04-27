namespace PipelineEval.Api.Meters;

/// <summary>
/// Application meter and counter names. <see cref="MeterName"/> is the <c>System.Diagnostics.Metrics.Meter</c>
/// name registered with OpenTelemetry via <c>AddOpenTelemetry().WithMetrics(m =&gt; m.AddMeter(name))</c>;
/// the per-counter constants are passed to <c>Meter.CreateCounter&lt;long&gt;(name)</c>. Mirrors
/// <c>SampleServiceMeterNames</c> from the <c>nextService</c> CLI template.
/// </summary>
public static class PipelineEvalApiMeterNames
{
    public const string MeterName = "PipelineEval.Api";

    public const string TodoCreated = "todo.created";
    public const string TodoUpdated = "todo.updated";
    public const string TodoDeleted = "todo.deleted";
    public const string TodoUploadUrlIssued = "todo.upload_url_issued";
    public const string InviteCreated = "invite.created";

    public static IReadOnlyList<string> Meters { get; } = [MeterName];
}
