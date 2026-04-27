using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using PipelineEval.Api.Context;
using PipelineEval.Api.Contracts;
using PipelineEval.Api.Data;
using PipelineEval.Api.Meters;

namespace PipelineEval.Api.Services;

/// <summary>
/// Owns the existing <c>/api/todos</c> CRUD + upload-url logic that previously lived as inline lambdas in
/// <c>Program.cs</c>. Counters are created via <see cref="IMeterFactory"/> against the
/// <see cref="PipelineEvalApiMeterNames.MeterName"/> meter (registered by
/// <c>Startup/OpenTelemetryConfigurator.cs</c>). Each operation runs inside a
/// <see cref="ILogger.BeginScope"/> populated from <see cref="TodoContextSession"/>.
/// </summary>
public sealed class TodoService : ITodoService, IDisposable
{
    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DownloadLifetime = TimeSpan.FromHours(1);

    private readonly AppDbContext _db;
    private readonly ICatPictureStorage _storage;
    private readonly ILogger<TodoService> _logger;
    private readonly Meter _meter;
    private readonly Counter<long> _todoCreated;
    private readonly Counter<long> _todoUpdated;
    private readonly Counter<long> _todoDeleted;
    private readonly Counter<long> _todoUploadUrlIssued;

    public TodoService(
        AppDbContext db,
        ICatPictureStorage storage,
        ILogger<TodoService> logger,
        IMeterFactory meterFactory)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
        _meter = meterFactory.Create(PipelineEvalApiMeterNames.MeterName);
        _todoCreated = _meter.CreateCounter<long>(PipelineEvalApiMeterNames.TodoCreated);
        _todoUpdated = _meter.CreateCounter<long>(PipelineEvalApiMeterNames.TodoUpdated);
        _todoDeleted = _meter.CreateCounter<long>(PipelineEvalApiMeterNames.TodoDeleted);
        _todoUploadUrlIssued = _meter.CreateCounter<long>(PipelineEvalApiMeterNames.TodoUploadUrlIssued);
    }

    public bool IsStorageConfigured => _storage.IsConfigured;

    public bool IsContentTypeAllowed(string contentType) => _storage.IsAllowedContentType(contentType);

    public async Task<IReadOnlyList<TodoResponse>> ListAsync(string userSub, CancellationToken ct)
    {
        var ctx = new TodoContextSession { UserSub = userSub, Operation = "List" };
        using (_logger.BeginScope(TodoContextSession.GetMessageFormat, ctx.GetMessageArgs))
        {
            var list = await _db.Todos.AsNoTracking()
                .Where(t => t.UserSub == userSub)
                .OrderBy(t => t.CreatedAtUtc)
                .ToListAsync(ct).ConfigureAwait(false);
            return list.Select(t => TodoMappers.ToDto(t, _storage, DownloadLifetime)).ToList();
        }
    }

    public async Task<TodoResponse?> GetAsync(string userSub, Guid id, CancellationToken ct)
    {
        var ctx = new TodoContextSession { UserSub = userSub, Operation = "Get", TodoId = id.ToString() };
        using (_logger.BeginScope(TodoContextSession.GetMessageFormat, ctx.GetMessageArgs))
        {
            var t = await _db.Todos.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserSub == userSub, ct).ConfigureAwait(false);
            return t is null ? null : TodoMappers.ToDto(t, _storage, DownloadLifetime);
        }
    }

    public async Task<CreateTodoResult> CreateAsync(string userSub, CreateTodoRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Title))
            return new CreateTodoResult(CreateTodoOutcome.MissingTitle, Guid.Empty);

        var entity = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserSub = userSub,
            Title = body.Title.Trim(),
            Notes = string.IsNullOrWhiteSpace(body.Notes) ? null : body.Notes.Trim(),
            IsCompleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var ctx = new TodoContextSession { UserSub = userSub, Operation = "Create", TodoId = entity.Id.ToString() };
        using (_logger.BeginScope(TodoContextSession.GetMessageFormat, ctx.GetMessageArgs))
        {
            _db.Todos.Add(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _todoCreated.Add(1);
            _logger.LogInformation("Created todo.");
            return new CreateTodoResult(CreateTodoOutcome.Ok, entity.Id);
        }
    }

    public async Task<UpdateTodoResult> UpdateAsync(string userSub, Guid id, UpdateTodoRequest body, CancellationToken ct)
    {
        var ctx = new TodoContextSession { UserSub = userSub, Operation = "Update", TodoId = id.ToString() };
        using (_logger.BeginScope(TodoContextSession.GetMessageFormat, ctx.GetMessageArgs))
        {
            var entity = await _db.Todos
                .FirstOrDefaultAsync(x => x.Id == id && x.UserSub == userSub, ct).ConfigureAwait(false);
            if (entity is null)
                return new UpdateTodoResult(UpdateTodoOutcome.NotFound, null);

            if (body.Title is { } title)
                entity.Title = title.Trim();
            if (body.Notes is not null)
                entity.Notes = string.IsNullOrWhiteSpace(body.Notes) ? null : body.Notes.Trim();
            if (body.IsCompleted is { } done)
                entity.IsCompleted = done;
            if (body.CatImageObjectKey is not null)
            {
                if (string.IsNullOrWhiteSpace(body.CatImageObjectKey))
                {
                    entity.CatImageObjectKey = null;
                }
                else if (!_storage.IsObjectKeyForTodo(id, body.CatImageObjectKey))
                {
                    return new UpdateTodoResult(UpdateTodoOutcome.InvalidObjectKey, null);
                }
                else
                {
                    entity.CatImageObjectKey = body.CatImageObjectKey.Trim();
                }
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _todoUpdated.Add(1);
            _logger.LogInformation("Updated todo.");
            return new UpdateTodoResult(UpdateTodoOutcome.Ok, TodoMappers.ToDto(entity, _storage, DownloadLifetime));
        }
    }

    public async Task<bool> DeleteAsync(string userSub, Guid id, CancellationToken ct)
    {
        var ctx = new TodoContextSession { UserSub = userSub, Operation = "Delete", TodoId = id.ToString() };
        using (_logger.BeginScope(TodoContextSession.GetMessageFormat, ctx.GetMessageArgs))
        {
            var entity = await _db.Todos
                .FirstOrDefaultAsync(x => x.Id == id && x.UserSub == userSub, ct).ConfigureAwait(false);
            if (entity is null)
                return false;
            _db.Todos.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _todoDeleted.Add(1);
            _logger.LogInformation("Deleted todo.");
            return true;
        }
    }

    public async Task<CreateUploadUrlResult> CreateUploadUrlAsync(string userSub, Guid id, UploadUrlRequest body, CancellationToken ct)
    {
        if (!_storage.IsConfigured)
            return new CreateUploadUrlResult(CreateUploadUrlOutcome.StorageUnavailable, null);

        if (!_storage.IsAllowedContentType(body.ContentType ?? string.Empty))
            return new CreateUploadUrlResult(CreateUploadUrlOutcome.InvalidContentType, null);

        var ctx = new TodoContextSession { UserSub = userSub, Operation = "CreateUploadUrl", TodoId = id.ToString() };
        using (_logger.BeginScope(TodoContextSession.GetMessageFormat, ctx.GetMessageArgs))
        {
            var exists = await _db.Todos.AsNoTracking()
                .AnyAsync(x => x.Id == id && x.UserSub == userSub, ct).ConfigureAwait(false);
            if (!exists)
                return new CreateUploadUrlResult(CreateUploadUrlOutcome.NotFound, null);

            var fileName = TodoMappers.SanitizeFileName(body.FileName);
            var objectKey = _storage.BuildObjectKey(id, fileName);
            var url = _storage.GetUploadPresignedUrl(objectKey, body.ContentType!.Trim(), UploadLifetime);
            if (url is null)
                return new CreateUploadUrlResult(CreateUploadUrlOutcome.StorageUnavailable, null);

            _todoUploadUrlIssued.Add(1);
            _logger.LogInformation("Issued upload URL.");
            return new CreateUploadUrlResult(
                CreateUploadUrlOutcome.Ok,
                new UploadUrlResponse(url, objectKey, (int)UploadLifetime.TotalSeconds));
        }
    }

    public void Dispose() => _meter.Dispose();
}
