namespace PipelineEval.Host.Configuration;

/// <summary>
/// Reference-type wrapper so <see cref="LocalPinnedPorts"/> can be registered in DI (structs are not valid for generic singleton registration).
/// </summary>
internal sealed class LocalPinnedPortsHolder(LocalPinnedPorts ports)
{
    public LocalPinnedPorts Ports { get; } = ports;
}
