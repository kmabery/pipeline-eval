using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PipelineEval.Host;

/// <summary>
/// Watches the API resource and dumps its log tail to stderr if it reaches a terminal state
/// without ever reaching "Running". Turns silent "Done" in the Aspire dashboard into an
/// actionable failure message in the AppHost terminal.
/// </summary>
internal sealed class ApiEarlyExitWatcher : BackgroundService
{
    private const string ResourceName = "pipeline-eval-api";
    private const int TailLineCount = 50;

    private static readonly HashSet<string> TerminalStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "Finished", "Exited", "FailedToStart", "Terminated", "Stopped",
    };

    private static readonly HashSet<string> HealthyStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "Running", "Ready",
    };

    private readonly ResourceNotificationService _notifications;
    private readonly ResourceLoggerService _loggers;
    private readonly ILogger<ApiEarlyExitWatcher> _logger;

    public ApiEarlyExitWatcher(
        ResourceNotificationService notifications,
        ResourceLoggerService loggers,
        ILogger<ApiEarlyExitWatcher> logger)
    {
        _notifications = notifications;
        _loggers = loggers;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tail = new Queue<string>(TailLineCount);
        var logTask = Task.Run(() => TailLogsAsync(tail, stoppingToken), stoppingToken);

        try
        {
            await WatchForEarlyApiExitAsync(tail, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ApiEarlyExitWatcher terminated unexpectedly.");
        }

        await AwaitLogTaskIgnoringCancelAsync(logTask).ConfigureAwait(false);
    }

    private async Task WatchForEarlyApiExitAsync(Queue<string> tail, CancellationToken ct)
    {
        var everRunning = false;
        var dumped = false;

        await foreach (var evt in _notifications.WatchAsync(ct).ConfigureAwait(false))
        {
            if (!string.Equals(evt.Resource.Name, ResourceName, StringComparison.OrdinalIgnoreCase))
                continue;

            var state = evt.Snapshot.State?.Text ?? string.Empty;
            if (HealthyStates.Contains(state))
                everRunning = true;

            if (ShouldDumpEarlyExit(dumped, everRunning, state))
            {
                dumped = true;
                await WriteEarlyExitDumpAsync(state, tail, ct).ConfigureAwait(false);
            }
        }
    }

    private static bool ShouldDumpEarlyExit(bool dumped, bool everRunning, string state) =>
        !dumped && !everRunning && TerminalStates.Contains(state);

    private static async Task WriteEarlyExitDumpAsync(
        string state,
        Queue<string> tail,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Console.Error.WriteLineAsync(
                $"[AppHost] API resource '{ResourceName}' exited early (state={state}). Last {TailLineCount} log lines:")
            .ConfigureAwait(false);

        foreach (var line in tail)
            await Console.Error.WriteLineAsync("  | " + line).ConfigureAwait(false);

        await Console.Error.WriteLineAsync(
                "[AppHost] Fix the error above, then re-run the AppHost. Common causes: port conflict, " +
                "missing Docker (Postgres), or missing Cognito config when RequireAuthentication=true.")
            .ConfigureAwait(false);
    }

    private static async Task AwaitLogTaskIgnoringCancelAsync(Task logTask)
    {
        try
        {
            await logTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private async Task TailLogsAsync(Queue<string> tail, CancellationToken ct)
    {
        try
        {
            await foreach (var batch in _loggers.WatchAsync(ResourceName).WithCancellation(ct).ConfigureAwait(false))
            {
                foreach (var line in batch)
                    EnqueueTailLine(tail, line.Content ?? string.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private static void EnqueueTailLine(Queue<string> tail, string content)
    {
        lock (tail)
        {
            if (tail.Count == TailLineCount)
                tail.Dequeue();
            tail.Enqueue(content);
        }
    }
}
