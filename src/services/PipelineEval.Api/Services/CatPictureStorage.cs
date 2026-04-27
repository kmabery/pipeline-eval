using Amazon.S3;
using Amazon.S3.Model;

namespace PipelineEval.Api.Services;

public sealed class CatPictureStorage : ICatPictureStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly string _keyPrefix;
    private readonly bool _forceHttpScheme;

    public CatPictureStorage(IConfiguration configuration)
    {
        _s3 = S3ClientFactory.Create(configuration);
        _bucketName = configuration["S3:BucketName"] ?? string.Empty;
        _keyPrefix = (configuration["S3:KeyPrefix"] ?? "cats").Trim().Trim('/');
        // When talking to LocalStack (http://localhost:4566 etc.) the AWS SDK still
        // emits https:// in presigned URLs, which browsers reject with
        // net::ERR_CERT_AUTHORITY_INVALID. Force the scheme back to http to match.
        var serviceUrl = configuration["S3:ServiceUrl"];
        _forceHttpScheme = !string.IsNullOrWhiteSpace(serviceUrl)
            && serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_bucketName);

    public bool IsAllowedContentType(string contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Trim());

    public bool IsObjectKeyForTodo(Guid todoId, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.Contains("..", StringComparison.Ordinal))
            return false;
        return objectKey.StartsWith($"{_keyPrefix}/{todoId:N}/", StringComparison.Ordinal);
    }

    public string BuildObjectKey(Guid todoId, string sanitizedFileName)
    {
        var safe = string.IsNullOrWhiteSpace(sanitizedFileName) ? "image.jpg" : sanitizedFileName;
        return $"{_keyPrefix}/{todoId:N}/{Guid.NewGuid():N}-{safe}";
    }

    public string? GetUploadPresignedUrl(string objectKey, string contentType, TimeSpan lifetime)
    {
        if (!IsConfigured)
            return null;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(lifetime),
            ContentType = contentType,
        };
        return MaybeForceHttp(_s3.GetPreSignedURL(request));
    }

    public string? GetDownloadPresignedUrl(string objectKey, TimeSpan lifetime)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(objectKey))
            return null;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime),
        };
        return MaybeForceHttp(_s3.GetPreSignedURL(request));
    }

    private string? MaybeForceHttp(string? url)
    {
        if (!_forceHttpScheme || string.IsNullOrEmpty(url))
            return url;
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "http://" + url.Substring("https://".Length)
            : url;
    }
}
