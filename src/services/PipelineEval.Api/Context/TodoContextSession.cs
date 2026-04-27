namespace PipelineEval.Api.Context;

/// <summary>
/// Per-request context attached to <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope"/>. Mirrors
/// <c>SampleContextSession</c> from the <c>nextService</c> CLI template so service code wraps each operation
/// in a scope with stable structured-log keys (<c>UserSub</c>, <c>Operation</c>, <c>TodoId</c>).
/// </summary>
public sealed class TodoContextSession
{
    public string UserSub { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string? TodoId { get; init; }

    public const string GetMessageFormat = "{UserSub}{Operation}{TodoId}";

    public object[] GetMessageArgs => [UserSub, Operation, TodoId ?? string.Empty];
}
