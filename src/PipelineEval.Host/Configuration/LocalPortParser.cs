namespace PipelineEval.Host.Configuration;

internal static class LocalPortParser
{
    public static int Parse(string environmentVariableName, int fallback) =>
        ParseFromGetter(() => Environment.GetEnvironmentVariable(environmentVariableName), fallback);

    /// <summary>Testable entry point without reading real environment variables.</summary>
    internal static int ParseFromGetter(Func<string?> getValue, int fallback)
    {
        var raw = getValue();
        return int.TryParse(raw, out var p) && p is >= 0 and < 65536 ? p : fallback;
    }
}
