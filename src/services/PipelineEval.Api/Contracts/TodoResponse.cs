namespace PipelineEval.Api.Contracts;

public sealed record TodoResponse(
    Guid Id,
    string Title,
    string? Notes,
    bool IsCompleted,
    string? CatImageObjectKey,
    string? CatImageUrl,
    DateTimeOffset CreatedAtUtc);
