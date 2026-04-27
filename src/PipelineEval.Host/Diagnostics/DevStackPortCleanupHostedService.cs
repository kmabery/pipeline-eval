using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PipelineEval.Host.Configuration;

namespace PipelineEval.Host.Diagnostics;

/// <summary>
/// On AppHost shutdown (including Ctrl+C), ensures the Vite dev server does not keep a stale <c>node.exe</c>
/// bound to <see cref="LocalPinnedPorts.Web"/> after the orchestrator exits (common on Windows).
/// </summary>
internal sealed class DevStackPortCleanupHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly LocalPinnedPortsHolder _ports;
    private readonly ILogger<DevStackPortCleanupHostedService> _logger;
    private int _cleanupRegistered;

    public DevStackPortCleanupHostedService(
        IHostApplicationLifetime lifetime,
        LocalPinnedPortsHolder ports,
        ILogger<DevStackPortCleanupHostedService> logger)
    {
        _lifetime = lifetime;
        _ports = ports;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _cleanupRegistered, 1) != 0)
            return Task.CompletedTask;

        _lifetime.ApplicationStopping.Register(OnStopping);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnStopping()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            KillNodeListenersOnWebPort();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Dev stack port cleanup failed (non-fatal).");
        }
    }

    private void KillNodeListenersOnWebPort()
    {
        var port = _ports.Ports.Web;
        if (port <= 0)
            return;

        foreach (var pid in PortOwningProcessResolver.GetListeningProcessIds(port))
        {
            if (!StaleNodeDevPortReclaimer.TryKillProcessIfName(pid, "node", out var killed))
                continue;

            if (killed)
            {
                _logger.LogInformation(
                    "Stopped Node.js process {Pid} still listening on LOCAL_WEB_PORT ({Port}) after AppHost shutdown.",
                    pid,
                    port);
            }
        }
    }
}
