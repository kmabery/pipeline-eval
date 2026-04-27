using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace PipelineEval.Api.Services;

/// <summary>
/// Ensures bucket and browser CORS exist when using an S3-compatible endpoint (e.g. LocalStack).
/// </summary>
public sealed class S3LocalDevInitializer : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<S3LocalDevInitializer> _logger;

    public S3LocalDevInitializer(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<S3LocalDevInitializer> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceUrl = _configuration["S3:ServiceUrl"];
        var bucket = _configuration["S3:BucketName"];

        if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(bucket))
            return;

        if (!_environment.IsDevelopment())
            return;

        try
        {
            using var s3 = S3ClientFactory.Create(_configuration);
            await EnsureBucketAsync(s3, bucket, cancellationToken).ConfigureAwait(false);
            await EnsureCorsAsync(s3, bucket, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "S3 local bootstrap failed (bucket {Bucket}). Uploads may fail until S3 is reachable.",
                bucket);
        }
    }

    private static async Task EnsureBucketAsync(IAmazonS3 s3, string bucket, CancellationToken ct)
    {
        try
        {
            await s3.HeadBucketAsync(new HeadBucketRequest { BucketName = bucket }, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == HttpStatusCode.NotFound
            || string.Equals(ex.ErrorCode, "NoSuchBucket", StringComparison.OrdinalIgnoreCase))
        {
            await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, ct).ConfigureAwait(false);
        }
    }

    private static Task EnsureCorsAsync(IAmazonS3 s3, string bucket, CancellationToken ct)
    {
        var cors = new CORSConfiguration
        {
            Rules =
            [
                new CORSRule
                {
                    AllowedMethods = ["GET", "PUT", "HEAD", "POST", "DELETE"],
                    AllowedOrigins = ["*"],
                    AllowedHeaders = ["*"],
                    ExposeHeaders = ["ETag", "x-amz-request-id"],
                    MaxAgeSeconds = 3000,
                },
            ],
        };

        return s3.PutCORSConfigurationAsync(
            new PutCORSConfigurationRequest { BucketName = bucket, Configuration = cors },
            ct);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
