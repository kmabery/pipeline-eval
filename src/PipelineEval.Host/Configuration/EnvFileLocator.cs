namespace PipelineEval.Host.Configuration;

internal static class EnvFileLocator
{
    public static string? FindDotEnvFile(string startDir)
    {
        for (var dir = new DirectoryInfo(startDir); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
