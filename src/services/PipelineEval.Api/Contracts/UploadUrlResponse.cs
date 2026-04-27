namespace PipelineEval.Api.Contracts;

public sealed record UploadUrlResponse(string UploadUrl, string ObjectKey, int ExpiresInSeconds);
