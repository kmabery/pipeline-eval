using PipelineEval.Api.Authentication;
using PipelineEval.Api.Contracts;
using PipelineEval.Api.Services;

namespace PipelineEval.Api.Startup;

/// <summary>
/// Maps <c>POST /api/auth/invite</c>. When auth is required and Cognito is configured, the endpoint is
/// the real admin-only invite flow backed by <see cref="IInviteService"/>; in local dev with auth
/// disabled it falls back to the stub the burger-menu uses. Mirrors the template's Map* shape.
/// </summary>
public static class MapInviteEndpoints
{
    public static WebApplication MapPipelineEvalInviteEndpoints(this WebApplication app)
    {
        var requireAuth = app.Configuration.GetValue("Authentication:RequireAuthentication", true);
        if (requireAuth && !string.IsNullOrWhiteSpace(app.Configuration["Cognito:UserPoolId"]))
        {
            MapInvite(app);
        }
        else if (!requireAuth && app.Environment.IsDevelopment())
        {
            MapInviteLocalStub(app);
        }

        return app;
    }

    private static RouteHandlerBuilder MapInvite(WebApplication app) =>
        app.MapPost(
                "/api/auth/invite",
                async Task<IResult> (
                    InviteRequest body,
                    IInviteService invites,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Email))
                        return Results.BadRequest(new { error = "Email is required." });
                    try
                    {
                        await invites.InviteUserByEmailAsync(body.Email, ct).ConfigureAwait(false);
                        return Results.Ok(new { message = "Invitation email sent (if the user did not already exist)." });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.Conflict(new { error = ex.Message });
                    }
                })
            .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
            .WithName("InviteUser");

    private static RouteHandlerBuilder MapInviteLocalStub(WebApplication app) =>
        app.MapPost(
                "/api/auth/invite",
                (InviteRequest body, ILogger<Program> log) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Email))
                        return Results.BadRequest(new { error = "Email is required." });
                    log.LogInformation("Local invite stub: pretending to invite {Email}.", body.Email);
                    return Results.Ok(new { message = $"Local dev: invitation stub accepted for {body.Email}." });
                })
            .WithName("InviteUserLocalStub");
}
