using PipelineEval.Api.Authentication;
using PipelineEval.Api.Services;

namespace PipelineEval.Api.Startup;

/// <summary>
/// Thin chained-extensions wrapper over <see cref="AuthenticationExtensions"/>. Mirrors the template's
/// <c>SwaggerConfigurator</c> shape (builder + app extension methods) so Program.cs stays linear.
/// </summary>
public static class AuthenticationConfigurator
{
    public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddPipelineEvalAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddCognitoIdentityProvider(builder.Configuration);
        return builder;
    }

    public static WebApplication UseAuthenticationPipeline(this WebApplication app)
    {
        app.UsePipelineEvalAuthentication(app.Configuration);
        return app;
    }
}
