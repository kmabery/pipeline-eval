using PipelineEval.Host.Configuration;

namespace PipelineEval.Host.Diagnostics;

internal static class PortPreflightRunner
{
    /// <returns>True if all ports are free; false if any conflict (errors written to stderr).</returns>
    public static bool TryPass(LocalPinnedPorts ports)
    {
        var conflicts = ports
            .EnumerateForPreflight()
            .Where(p => p.Port > 0)
            .Where(p => PortAvailabilityChecker.IsPortInUse(p.Port))
            .ToList();

        if (conflicts.Count == 0)
            return true;

        Console.Error.WriteLine("[AppHost] Port preflight failed. The following ports are already in use:");
        foreach (var c in conflicts)
            WriteConflictLine(c.Name, c.Port);
        return false;
    }

    private static void WriteConflictLine(string name, int port)
    {
        Console.Error.WriteLine(
            $"  - {port} ({name}). Change {name} in .env or free the port (Windows: " +
            $"'Get-NetTCPConnection -LocalPort {port} | Select-Object OwningProcess').");
    }
}
