using PipelineEval.Api.Contracts;

namespace PipelineEval.Api.Services;

/// <summary>Discriminator for <see cref="ITodoService.CreateAsync"/>.</summary>
public enum CreateTodoOutcome
{
    Ok,
    MissingTitle,
}

public readonly record struct CreateTodoResult(CreateTodoOutcome Outcome, Guid Id);

/// <summary>Discriminator for <see cref="ITodoService.UpdateAsync"/>.</summary>
public enum UpdateTodoOutcome
{
    Ok,
    NotFound,
    InvalidObjectKey,
}

public readonly record struct UpdateTodoResult(UpdateTodoOutcome Outcome, TodoResponse? Response);

/// <summary>Discriminator for <see cref="ITodoService.CreateUploadUrlAsync"/>.</summary>
public enum CreateUploadUrlOutcome
{
    Ok,
    StorageUnavailable,
    InvalidContentType,
    NotFound,
}

public readonly record struct CreateUploadUrlResult(CreateUploadUrlOutcome Outcome, UploadUrlResponse? Response);

/// <summary>
/// Application service for the <c>/api/todos</c> endpoints. HTTP-agnostic by design (mirrors the
/// <c>nextService</c> CLI template's <c>ISampleService</c>) so the Map* extensions stay thin and
/// integration tests can exercise the service directly.
/// </summary>
public interface ITodoService
{
    bool IsStorageConfigured { get; }

    bool IsContentTypeAllowed(string contentType);

    Task<IReadOnlyList<TodoResponse>> ListAsync(string userSub, CancellationToken ct);

    Task<TodoResponse?> GetAsync(string userSub, Guid id, CancellationToken ct);

    Task<CreateTodoResult> CreateAsync(string userSub, CreateTodoRequest body, CancellationToken ct);

    Task<UpdateTodoResult> UpdateAsync(string userSub, Guid id, UpdateTodoRequest body, CancellationToken ct);

    Task<bool> DeleteAsync(string userSub, Guid id, CancellationToken ct);

    Task<CreateUploadUrlResult> CreateUploadUrlAsync(string userSub, Guid id, UploadUrlRequest body, CancellationToken ct);
}
