using System.Net;
using System.Net.Sockets;

namespace PipelineEval.Host.Diagnostics;

internal static class PortAvailabilityChecker
{
    public static bool IsPortInUse(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }
}
