using System.Diagnostics;

namespace PipelineEval.Host.Diagnostics;

/// <summary>
/// Resolves which process IDs are listening on a TCP port (Windows: Get-NetTCPConnection).
/// Used to reclaim stale Vite/Node listeners after AppHost shutdown or before preflight.
/// </summary>
internal static class PortOwningProcessResolver
{
    public static IReadOnlyList<int> GetListeningProcessIds(int port)
    {
        if (port <= 0 || !OperatingSystem.IsWindows())
            return [];

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    "-NoProfile -NonInteractive -Command " +
                    "\"Get-NetTCPConnection -LocalPort " + port +
                    " -State Listen -ErrorAction SilentlyContinue | " +
                    "Select-Object -ExpandProperty OwningProcess -Unique\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                return [];

            var stdout = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(10_000);

            var ids = new List<int>();
            foreach (var line in stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line.Trim(), out var id) && id > 0)
                    ids.Add(id);
            }

            return ids;
        }
        catch
        {
            return [];
        }
    }
}
