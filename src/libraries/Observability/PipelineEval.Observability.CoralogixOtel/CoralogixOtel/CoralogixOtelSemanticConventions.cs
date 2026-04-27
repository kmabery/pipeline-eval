// Aligned with serilog/serilog-sinks-opentelemetry SemanticConventions (exception + message template attrs).

namespace PipelineEval.Observability.CoralogixOtel;

public static class CoralogixOtelSemanticConventions
{
    public const string AttributeExceptionType = "exception.type";
    public const string AttributeExceptionMessage = "exception.message";
    public const string AttributeExceptionStacktrace = "exception.stacktrace";
    public const string AttributeMessageTemplateText = "message_template.text";
    public const string AttributeMessageTemplateMD5Hash = "message_template.hash.md5";
    public const string AttributeMessageTemplateRenderings = "message_template.renderings";
}
