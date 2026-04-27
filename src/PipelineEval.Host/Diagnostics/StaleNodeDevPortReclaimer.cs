using System.Diagnostics;
using PipelineEval.Host.Configuration;

namespace PipelineEval.Host.Diagnostics;

/// <summary>
/// If the pinned web port is still held by a stale <c>node.exe</c> (typical when Vite outlives AppHost on Windows),
/// stop that process so the next <c>dotnet run</c> can bind the port.
/// </summary>
internal static class StaleNodeDevPortReclaimer
{
    private const string NodeProcessName = "node";

    /// <summary>
    /// Attempts to free <see cref="LocalPinnedPorts.Web"/> when it is in use only by Node.
    /// </summary>
    public static void TryReclaimWebPortIfStaleNode(LocalPinnedPorts ports)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var webPort = ports.Web;
        if (webPort <= 0 || !PortAvailabilityChecker.IsPortInUse(webPort))
            return;

        foreach (var pid in PortOwningProcessResolver.GetListeningProcessIds(webPort))
        {
            if (!TryKillProcessIfName(pid, NodeProcessName, out var killed))
                continue;

            if (killed)
            {
                Console.WriteLine(
                    $"[AppHost] Stopped stale Node.js process (PID {pid}) that was still listening on " +
                    $"LOCAL_WEB_PORT ({webPort}). This can happen if Vite did not exit when the AppHost stopped.");
            }

            Thread.Sleep(200);
            if (!PortAvailabilityChecker.IsPortInUse(webPort))
                return;
        }
    }

    internal static bool TryKillProcessIfName(int pid, string expectedProcessName, out bool killed)
    {
        killed = false;
        try
        {
            using var p = Process.GetProcessById(pid);
            if (!string.Equals(p.ProcessName, expectedProcessName, StringComparison.OrdinalIgnoreCase))
                return false;

            p.Kill(entireProcessTree: true);
            p.WaitForExit(10_000);
            killed = true;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
