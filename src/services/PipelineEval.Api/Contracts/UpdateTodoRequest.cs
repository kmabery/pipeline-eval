namespace PipelineEval.Api.Contracts;

public sealed record UpdateTodoRequest(string? Title, string? Notes, bool? IsCompleted, string? CatImageObjectKey);
