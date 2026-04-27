using Amazon.Extensions.Configuration.SystemsManager;
using PipelineEval.Observability;

namespace PipelineEval.Api.Startup;

/// <summary>
/// Pre-bind step that mirrors the <c>nextService</c> CLI template's chained-extensions style. Layers in:
/// (a) the AWS Systems Manager Parameter Store provider when <c>Aws:SsmParameterPrefix</c> /
/// <c>AWS_SSM_PARAMETER_PREFIX</c> is set, and (b) the Coralogix-friendly env var mapping from
/// <see cref="CoralogixEnvConfiguration"/>. Runs before <c>ConfigureOpenTelemetry</c> so observability
/// settings (API key, endpoint) populated from SSM are visible to the OTel bootstrap.
/// </summary>
public static class ConfigurationConfigurator
{
    public static WebApplicationBuilder ConfigureRuntimeConfiguration(this WebApplicationBuilder builder)
    {
        var ssmPrefix =
            builder.Configuration["Aws:SsmParameterPrefix"]
            ?? Environment.GetEnvironmentVariable("AWS_SSM_PARAMETER_PREFIX");
        if (!string.IsNullOrWhiteSpace(ssmPrefix))
        {
            builder.Configuration.AddSystemsManager(
                configureSource =>
                {
                    configureSource.Path = ssmPrefix.TrimEnd('/');
                    configureSource.Optional = true;
                    configureSource.ReloadAfter = TimeSpan.FromMinutes(5);
                });
        }

        CoralogixEnvConfiguration.ApplyCoralogixEnvironment(builder.Configuration);
        return builder;
    }
}
