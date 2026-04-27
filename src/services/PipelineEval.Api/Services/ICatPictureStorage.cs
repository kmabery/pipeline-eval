namespace PipelineEval.Api.Services;

public interface ICatPictureStorage
{
    bool IsConfigured { get; }

    string BuildObjectKey(Guid todoId, string sanitizedFileName);

    string? GetUploadPresignedUrl(string objectKey, string contentType, TimeSpan lifetime);

    string? GetDownloadPresignedUrl(string objectKey, TimeSpan lifetime);

    bool IsAllowedContentType(string contentType);

    bool IsObjectKeyForTodo(Guid todoId, string objectKey);
}
