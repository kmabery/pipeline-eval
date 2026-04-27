namespace PipelineEval.Api.Startup;

public static class MapHealthEndpoints
{
    public static WebApplication MapPipelineEvalHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        return app;
    }
}
