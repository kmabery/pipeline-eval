namespace PipelineEval.Api.Data;

public class TodoItem
{
    public Guid Id { get; set; }

    /// <summary>Cognito <c>sub</c>, or the anonymous local subject when authentication is disabled.</summary>
    public string UserSub { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
    public string? CatImageObjectKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
