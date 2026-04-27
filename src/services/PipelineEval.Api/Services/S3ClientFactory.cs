using Amazon;
using Amazon.S3;

namespace PipelineEval.Api.Services;

internal static class S3ClientFactory
{
    public static IAmazonS3 Create(IConfiguration configuration)
    {
        var regionName = configuration["S3:Region"] ?? "us-east-1";
        var serviceUrl = configuration["S3:ServiceUrl"];

        if (string.IsNullOrWhiteSpace(serviceUrl))
            return new AmazonS3Client(RegionEndpoint.GetBySystemName(regionName));

        var trimmed = serviceUrl.TrimEnd('/');
        var cfg = new AmazonS3Config
        {
            ServiceURL = trimmed,
            ForcePathStyle = true,
            AuthenticationRegion = regionName,
            // Without UseHttp, presigned URLs default to https:// even when ServiceURL is http,
            // which breaks browser PUTs to LocalStack (cert authority invalid).
            UseHttp = trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
        };

        var accessKey = configuration["S3:AccessKey"];
        var secretKey = configuration["S3:SecretKey"];
        var key = string.IsNullOrWhiteSpace(accessKey) ? "test" : accessKey;
        var secret = string.IsNullOrWhiteSpace(secretKey) ? "test" : secretKey;

        return new AmazonS3Client(key, secret, cfg);
    }
}
