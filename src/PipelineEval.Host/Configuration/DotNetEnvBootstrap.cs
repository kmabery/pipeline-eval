using DotNetEnv;

namespace PipelineEval.Host.Configuration;

internal static class DotNetEnvBootstrap
{
    /// <summary>
    /// Loads repo .env via TraversePath (clobberExistingVars: true). Returns first .env path found.
    /// </summary>
    public static string? LoadFromRepository(string currentDirectory)
    {
        try
        {
            Env.TraversePath().Load();
        }
        catch
        {
            // Missing or invalid .env should not crash startup.
        }

        return EnvFileLocator.FindDotEnvFile(currentDirectory);
    }
}
