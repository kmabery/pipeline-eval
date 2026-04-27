namespace PipelineEval.Api.Startup;

/// <summary>
/// We use <c>Microsoft.AspNetCore.OpenApi</c> (<c>AddOpenApi</c> / <c>MapOpenApi</c>) rather than the
/// <c>nextService</c> CLI template's Swashbuckle setup, but the chained-extensions shape is the same.
/// </summary>
public static class OpenApiConfigurator
{
    public static WebApplicationBuilder ConfigureOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
        return builder;
    }

    public static WebApplication ConfigureOpenApiApp(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();
        return app;
    }
}
