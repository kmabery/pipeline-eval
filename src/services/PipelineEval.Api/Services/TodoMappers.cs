using PipelineEval.Api.Contracts;
using PipelineEval.Api.Data;

namespace PipelineEval.Api.Services;

internal static class TodoMappers
{
    public static TodoResponse ToDto(TodoItem t, ICatPictureStorage storage, TimeSpan downloadLifetime) =>
        new(
            t.Id,
            t.Title,
            t.Notes,
            t.IsCompleted,
            t.CatImageObjectKey,
            t.CatImageObjectKey is { } key ? storage.GetDownloadPresignedUrl(key, downloadLifetime) : null,
            t.CreatedAtUtc);

    public static string SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "image.jpg";
        var f = Path.GetFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars())
            f = f.Replace(c, '_');
        return f.Length > 200 ? f[..200] : f;
    }
}
