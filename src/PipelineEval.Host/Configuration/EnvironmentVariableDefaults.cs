namespace PipelineEval.Host.Configuration;

internal static class EnvironmentVariableDefaults
{
    public static void Ensure(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, value);
    }
}
