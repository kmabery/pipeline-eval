using PipelineEval.Api.Services;

namespace PipelineEval.Api.Startup;

/// <summary>
/// Local-development-only endpoints that the front-end Vite dev server hits when
/// <c>LocalStorage:RootPath</c> is set, so cat-picture uploads/downloads work without real S3.
/// Active only when the host is in the <c>Development</c> environment.
/// </summary>
public static class MapDevStorageEndpoints
{
    public static WebApplication MapPipelineEvalDevStorageEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        MapUpload(app);
        MapDownload(app);
        return app;
    }

    private static RouteHandlerBuilder MapUpload(WebApplication app) =>
        app.MapPut(
                "/api/dev/cat-pictures/upload",
                async Task<IResult> (HttpRequest req, ICatPictureStorage storage, CancellationToken ct) =>
                {
                    if (storage is not LocalDirectoryCatPictureStorage local)
                        return Results.NotFound();

                    var k = req.Query["k"].ToString();
                    var e = req.Query["e"].ToString();
                    var s = req.Query["s"].ToString();
                    var contentType = req.ContentType ?? string.Empty;
                    if (!local.TryValidateUploadRequest(k, e, s, contentType, out var objectKey))
                        return Results.Unauthorized();

                    var path = local.GetSafeFilePath(objectKey);
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await req.Body.CopyToAsync(fs, ct).ConfigureAwait(false);
                    }

                    return Results.Ok();
                })
            .DisableAntiforgery()
            .WithName("DevCatPictureUpload");

    private static RouteHandlerBuilder MapDownload(WebApplication app) =>
        app.MapGet(
                "/api/dev/cat-pictures/download",
                (HttpRequest req, ICatPictureStorage storage) =>
                {
                    if (storage is not LocalDirectoryCatPictureStorage local)
                        return Results.NotFound();

                    var k = req.Query["k"].ToString();
                    var e = req.Query["e"].ToString();
                    var s = req.Query["s"].ToString();
                    if (!local.TryValidateDownloadRequest(k, e, s, out var objectKey))
                        return Results.Unauthorized();

                    var path = local.GetSafeFilePath(objectKey);
                    if (!System.IO.File.Exists(path))
                        return Results.NotFound();

                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    var mime = ext switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".webp" => "image/webp",
                        ".gif" => "image/gif",
                        _ => "application/octet-stream",
                    };

                    return Results.File(path, contentType: mime);
                })
            .WithName("DevCatPictureDownload");
}
