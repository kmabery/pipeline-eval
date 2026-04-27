using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace PipelineEval.Api.Services;

/// <summary>
/// Development-only storage that writes under <c>LocalStorage:RootPath</c> and serves upload/download via signed URLs on this API.
/// </summary>
public sealed class LocalDirectoryCatPictureStorage : ICatPictureStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    private readonly string _rootPath;
    private readonly string _keyPrefix;
    private readonly string _publicBaseUrl;
    private readonly byte[] _signingKey;

    public LocalDirectoryCatPictureStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var root = configuration["LocalStorage:RootPath"]
            ?? throw new InvalidOperationException("LocalStorage:RootPath is required for local directory storage.");
        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(root)
                ? root
                : Path.Combine(environment.ContentRootPath, root));

        _keyPrefix = (configuration["S3:KeyPrefix"] ?? "cats").Trim().Trim('/');
        _publicBaseUrl = (configuration["LocalStorage:PublicBaseUrl"] ?? "http://127.0.0.1:5101").TrimEnd('/');
        var secret = configuration["LocalStorage:SigningKey"] ?? "local-dev-signing-key-change-me";
        _signingKey = Encoding.UTF8.GetBytes(secret);
    }

    public bool IsConfigured => true;

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
        if (!IsAllowedContentType(contentType))
            return null;

        var expires = DateTimeOffset.UtcNow.Add(lifetime);
        var payload = $"u|{objectKey}|{expires.ToUnixTimeSeconds()}|{contentType.Trim()}";
        var sig = Sign(payload);
        var q = new Dictionary<string, string?>
        {
            ["k"] = Base64UrlEncode(Encoding.UTF8.GetBytes(objectKey)),
            ["e"] = expires.ToUnixTimeSeconds().ToString(),
            ["s"] = sig,
        };
        var qs = QueryHelpers.AddQueryString($"{_publicBaseUrl}/api/dev/cat-pictures/upload", q);
        return qs;
    }

    public string? GetDownloadPresignedUrl(string objectKey, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return null;

        var expires = DateTimeOffset.UtcNow.Add(lifetime);
        var payload = $"d|{objectKey}|{expires.ToUnixTimeSeconds()}";
        var sig = Sign(payload);
        var q = new Dictionary<string, string?>
        {
            ["k"] = Base64UrlEncode(Encoding.UTF8.GetBytes(objectKey)),
            ["e"] = expires.ToUnixTimeSeconds().ToString(),
            ["s"] = sig,
        };
        return QueryHelpers.AddQueryString($"{_publicBaseUrl}/api/dev/cat-pictures/download", q);
    }

    public bool TryValidateUploadRequest(string k, string e, string s, string contentType, out string objectKey)
    {
        objectKey = string.Empty;
        if (!long.TryParse(e, out var expSec))
            return false;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expSec)
            return false;

        byte[] keyBytes;
        try
        {
            keyBytes = Base64UrlDecode(k);
        }
        catch
        {
            return false;
        }

        objectKey = Encoding.UTF8.GetString(keyBytes);
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.Contains("..", StringComparison.Ordinal))
            return false;

        var ct = contentType.Trim();
        var payload = $"u|{objectKey}|{expSec}|{ct}";
        if (!SignaturesEqual(Sign(payload), s))
            return false;

        return IsAllowedContentType(ct);
    }

    public bool TryValidateDownloadRequest(string k, string e, string s, out string objectKey)
    {
        objectKey = string.Empty;
        if (!long.TryParse(e, out var expSec))
            return false;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expSec)
            return false;

        byte[] keyBytes;
        try
        {
            keyBytes = Base64UrlDecode(k);
        }
        catch
        {
            return false;
        }

        objectKey = Encoding.UTF8.GetString(keyBytes);
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.Contains("..", StringComparison.Ordinal))
            return false;

        var payload = $"d|{objectKey}|{expSec}";
        return SignaturesEqual(Sign(payload), s);
    }

    public string GetSafeFilePath(string objectKey) => SafeJoin(_rootPath, objectKey);

    private string Sign(string payload)
    {
        using var h = new HMACSHA256(_signingKey);
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool SignaturesEqual(string expectedHex, string actual)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expectedHex),
                Convert.FromHexString(actual));
        }
        catch
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        s = (s.Length % 4) switch
        {
            2 => s + "==",
            3 => s + "=",
            _ => s,
        };
        return Convert.FromBase64String(s);
    }

    private static string SafeJoin(string root, string objectKey)
    {
        var combined = Path.GetFullPath(
            Path.Combine(root, objectKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootFull = Path.GetFullPath(root);
        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Object key escapes storage root.");
        return combined;
    }
}
