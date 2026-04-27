using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PipelineEval.SampleWindowsService;

internal sealed class SampleWorker : BackgroundService
{
    private readonly ILogger<SampleWorker> _logger;
    private static readonly Meter Meter = new(OtelMetricNames.MeterName, "1.0.0");
    private static readonly Counter<long> Ticks = Meter.CreateCounter<long>(OtelMetricNames.TickCountName);

    public SampleWorker(ILogger<SampleWorker> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Ticks.Add(1, new KeyValuePair<string, object?>("subsystem", OtelMetricNames.SubsystemValue));
            _logger.LogInformation("Sample Windows service tick");
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken).ConfigureAwait(false);
        }
    }
}
