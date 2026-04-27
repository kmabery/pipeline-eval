using Microsoft.Extensions.Configuration;

namespace PipelineEval.Api;

/// <summary>
/// Resolves the Postgres connection string. Supports flat keys from AWS Systems Manager
/// (e.g. ConnectionStrings__pipelineeval) as well as the ConnectionStrings section.
/// </summary>
public static class ConnectionStringHelper
{
    public static string GetPipelineEval(IConfiguration configuration)
    {
        var cs =
            configuration.GetConnectionString("pipelineeval")
            ?? configuration["ConnectionStrings:pipelineeval"]
            ?? configuration["ConnectionStrings__pipelineeval"];
        return cs ?? throw new InvalidOperationException("Connection string 'pipelineeval' not configured.");
    }
}
