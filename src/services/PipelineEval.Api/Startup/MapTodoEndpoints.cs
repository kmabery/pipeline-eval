using PipelineEval.Api.Authentication;
using PipelineEval.Api.Contracts;
using PipelineEval.Api.Services;

namespace PipelineEval.Api.Startup;

/// <summary>
/// Mirrors the <c>nextService</c> CLI template's <c>MapSampleServiceEndpoints</c>: one public
/// <c>app.MapTodoEndpoints()</c> entry point plus private <c>RouteHandlerBuilder</c> helpers per route.
/// Handlers stay one-liners that delegate to <see cref="ITodoService"/>.
/// </summary>
public static class MapTodoEndpoints
{
    public static WebApplication MapPipelineEvalTodoEndpoints(this WebApplication app)
    {
        var requireAuth = app.Configuration.GetValue("Authentication:RequireAuthentication", true);
        var todos = app.MapGroup("/api/todos");
        if (requireAuth)
            todos.RequireAuthorization();

        MapList(todos);
        MapGetById(todos);
        MapCreate(todos);
        MapUpdate(todos);
        MapDelete(todos);
        MapUploadUrl(todos);

        return app;
    }

    private static RouteHandlerBuilder MapList(RouteGroupBuilder todos) =>
        todos.MapGet("/", async Task<IResult> (
                HttpContext http,
                IConfiguration cfg,
                ITodoService svc,
                CancellationToken ct) =>
            {
                var sub = http.GetUserSub(cfg);
                var list = await svc.ListAsync(sub, ct).ConfigureAwait(false);
                return Results.Ok(list);
            })
            .WithName("ListTodos");

    private static RouteHandlerBuilder MapGetById(RouteGroupBuilder todos) =>
        todos.MapGet("/{id:guid}", async Task<IResult> (
                HttpContext http,
                IConfiguration cfg,
                Guid id,
                ITodoService svc,
                CancellationToken ct) =>
            {
                var sub = http.GetUserSub(cfg);
                var dto = await svc.GetAsync(sub, id, ct).ConfigureAwait(false);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .WithName("GetTodo");

    private static RouteHandlerBuilder MapCreate(RouteGroupBuilder todos) =>
        todos.MapPost("/", async Task<IResult> (
                HttpContext http,
                IConfiguration cfg,
                CreateTodoRequest body,
                ITodoService svc,
                CancellationToken ct) =>
            {
                var sub = http.GetUserSub(cfg);
                var result = await svc.CreateAsync(sub, body, ct).ConfigureAwait(false);
                return result.Outcome switch
                {
                    CreateTodoOutcome.MissingTitle => Results.BadRequest(new { error = "Title is required." }),
                    _ => Results.Created($"/api/todos/{result.Id}", new { Id = result.Id }),
                };
            })
            .WithName("CreateTodo");

    private static RouteHandlerBuilder MapUpdate(RouteGroupBuilder todos) =>
        todos.MapPatch("/{id:guid}", async Task<IResult> (
                HttpContext http,
                IConfiguration cfg,
                Guid id,
                UpdateTodoRequest body,
                ITodoService svc,
                CancellationToken ct) =>
            {
                var sub = http.GetUserSub(cfg);
                var result = await svc.UpdateAsync(sub, id, body, ct).ConfigureAwait(false);
                return result.Outcome switch
                {
                    UpdateTodoOutcome.NotFound => Results.NotFound(),
                    UpdateTodoOutcome.InvalidObjectKey => Results.BadRequest(new { error = "Invalid cat image key for this todo." }),
                    _ => Results.Ok(result.Response),
                };
            })
            .WithName("UpdateTodo");

    private static RouteHandlerBuilder MapDelete(RouteGroupBuilder todos) =>
        todos.MapDelete("/{id:guid}", async Task<IResult> (
                HttpContext http,
                IConfiguration cfg,
                Guid id,
                ITodoService svc,
                CancellationToken ct) =>
            {
                var sub = http.GetUserSub(cfg);
                var deleted = await svc.DeleteAsync(sub, id, ct).ConfigureAwait(false);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteTodo");

    private static RouteHandlerBuilder MapUploadUrl(RouteGroupBuilder todos) =>
        todos.MapPost("/{id:guid}/upload-url", async Task<IResult> (
                HttpContext http,
                IConfiguration cfg,
                Guid id,
                UploadUrlRequest body,
                ITodoService svc,
                CancellationToken ct) =>
            {
                var sub = http.GetUserSub(cfg);
                var result = await svc.CreateUploadUrlAsync(sub, id, body, ct).ConfigureAwait(false);
                return result.Outcome switch
                {
                    CreateUploadUrlOutcome.StorageUnavailable => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
                    CreateUploadUrlOutcome.InvalidContentType => Results.BadRequest(new
                    {
                        error = "Content type must be image/jpeg, image/png, image/webp, or image/gif.",
                    }),
                    CreateUploadUrlOutcome.NotFound => Results.NotFound(),
                    _ => Results.Ok(result.Response),
                };
            })
            .WithName("CreateTodoUploadUrl");
}
