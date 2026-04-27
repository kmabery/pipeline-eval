// Copyright 2022 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Derived from serilog/serilog-sinks-opentelemetry v4.2.0 OtlpEventBuilder: LogRecord path only, with
// <see cref="CoralogixOtelBodyAsMap"/> for Coralogix Frequent Search (object body with "message" key).

using System.Globalization;
using System.IO;
using System.Linq;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.OpenTelemetry;

namespace PipelineEval.Observability.CoralogixOtel;

internal static class CoralogixOtelLogPropertyExclusions
{
    internal const string SpanStartTimestampPropertyName = "SpanStartTimestamp";
    internal const string ParentSpanIdPropertyName = "ParentSpanId";
    internal const string SpanKindPropertyName = "SpanKind";
}

/// <summary>
/// Shapes <see cref="LogRecord.Body"/> as an OTLP map (kvlist) with a <c>message</c> string field, per Coralogix guidance for Frequent Search.
/// </summary>
internal static class CoralogixOtelBodyAsMap
{
    internal const string MessageKey = "message";

    internal static AnyValue MessageMap(string text) =>
        new()
        {
            KvlistValue = new KeyValueList
            {
                Values = { new KeyValue { Key = MessageKey, Value = new AnyValue { StringValue = text } } },
            },
        };
}

/// <summary>
/// Vendored from serilog/serilog-sinks-opentelemetry v4.2.0: builds OTLP <see cref="LogRecord"/> from Serilog
/// with Coralogix-compatible <see cref="CoralogixOtelBodyAsMap"/> for the body.
/// </summary>
internal static class CoralogixOtlpEventBuilder
{
    public static (LogRecord logRecord, string? scopeName) ToLogRecord(
        LogEvent logEvent,
        IFormatProvider? formatProvider,
        IncludedData includedData)
    {
        var logRecord = new LogRecord();

        ProcessProperties(logRecord.Attributes.Add, logEvent, includedData, out var scopeName);
        ProcessTimestamp(logRecord, logEvent);
        ProcessBody(logRecord, logEvent, includedData, formatProvider);
        ProcessLevel(logRecord, logEvent);
        ProcessException(logRecord.Attributes, logEvent);
        ProcessIncludedFields(logRecord, logEvent, includedData);

        return (logRecord, scopeName);
    }

    static void ProcessBody(LogRecord logRecord, LogEvent logEvent, IncludedData includedFields, IFormatProvider? formatProvider)
    {
        if (!includedFields.HasFlag(IncludedData.TemplateBody))
        {
            var writer = new StringWriter();
            logEvent.MessageTemplate.Render(
                logEvent.Properties,
                writer,
                formatProvider ?? System.Globalization.CultureInfo.InvariantCulture);
            var renderedMessage = writer.ToString();

            if (!string.IsNullOrWhiteSpace(renderedMessage))
                logRecord.Body = CoralogixOtelBodyAsMap.MessageMap(renderedMessage);
        }
        else if (!string.IsNullOrWhiteSpace(logEvent.MessageTemplate.Text))
        {
            logRecord.Body = CoralogixOtelBodyAsMap.MessageMap(logEvent.MessageTemplate.Text);
        }
    }

    public static void ProcessLevel(LogRecord logRecord, LogEvent logEvent)
    {
        var level = logEvent.Level;
        logRecord.SeverityText = level.ToString();
        logRecord.SeverityNumber = CoralogixOtelPrimitiveConversions.ToSeverityNumber(level);
    }

    public static void ProcessProperties(
        Action<KeyValue> addAttribute, LogEvent logEvent, IncludedData includedData, out string? scopeName)
    {
        scopeName = null;
        foreach (var property in logEvent.Properties)
        {
            if (property is
                {
                    Key: Constants.SourceContextPropertyName, Value: ScalarValue { Value: string sourceContext }
                })
            {
                scopeName = sourceContext;
                if ((includedData & IncludedData.SourceContextAttribute) != IncludedData.SourceContextAttribute)
                {
                    continue;
                }
            }

            if (property is
                {
                    Key: CoralogixOtelLogPropertyExclusions.SpanStartTimestampPropertyName
                        or CoralogixOtelLogPropertyExclusions.ParentSpanIdPropertyName
                        or CoralogixOtelLogPropertyExclusions.SpanKindPropertyName
                })
            {
                continue;
            }

            var v = CoralogixOtelPrimitiveConversions.ToOpenTelemetryAnyValue(property.Value, includedData);
            addAttribute(CoralogixOtelPrimitiveConversions.NewAttribute(property.Key, v));
        }
    }

    public static void ProcessTimestamp(LogRecord logRecord, LogEvent logEvent)
    {
        logRecord.TimeUnixNano = CoralogixOtelPrimitiveConversions.ToUnixNano(logEvent.Timestamp);
        logRecord.ObservedTimeUnixNano = logRecord.TimeUnixNano;
    }

    public static void ProcessException(RepeatedField<KeyValue> attrs, LogEvent logEvent)
    {
        var ex = logEvent.Exception;
        if (ex == null) return;
        attrs.Add(
                CoralogixOtelPrimitiveConversions.NewStringAttribute(
                CoralogixOtelSemanticConventions.AttributeExceptionType, ex.GetType().ToString()));

        if (ex.Message != "")
        {
            attrs.Add(
                    CoralogixOtelPrimitiveConversions.NewStringAttribute(
                    CoralogixOtelSemanticConventions.AttributeExceptionMessage, ex.Message));
        }

        if (ex.ToString() != "")
        {
            attrs.Add(
                    CoralogixOtelPrimitiveConversions.NewStringAttribute(
                    CoralogixOtelSemanticConventions.AttributeExceptionStacktrace, ex.ToString()));
        }
    }

    static void ProcessIncludedFields(LogRecord logRecord, LogEvent logEvent, IncludedData includedFields)
    {
        if ((includedFields & IncludedData.TraceIdField) != IncludedData.None && logEvent.TraceId is { } traceId)
        {
            logRecord.TraceId = CoralogixOtelPrimitiveConversions.ToOpenTelemetryTraceId(traceId.ToHexString());
        }

        if ((includedFields & IncludedData.SpanIdField) != IncludedData.None && logEvent.SpanId is { } spanId)
        {
            logRecord.SpanId = CoralogixOtelPrimitiveConversions.ToOpenTelemetrySpanId(spanId.ToHexString());
        }

        if ((includedFields & IncludedData.MessageTemplateTextAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(
                    CoralogixOtelPrimitiveConversions.NewAttribute(
                    CoralogixOtelSemanticConventions.AttributeMessageTemplateText, new() { StringValue = logEvent.MessageTemplate.Text }));
        }

        if ((includedFields & IncludedData.MessageTemplateMD5HashAttribute) != IncludedData.None)
        {
            logRecord.Attributes.Add(
                    CoralogixOtelPrimitiveConversions.NewAttribute(
                    CoralogixOtelSemanticConventions.AttributeMessageTemplateMD5Hash, new() { StringValue = CoralogixOtelPrimitiveConversions.Md5Hash(logEvent.MessageTemplate.Text) }));
        }

        if ((includedFields & IncludedData.MessageTemplateRenderingsAttribute) != IncludedData.None)
        {
            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

            if (tokensWithFormat.Any())
            {
                var renderings = new ArrayValue();
                foreach (var propertyToken in tokensWithFormat)
                {
                    var space = new StringWriter();
                    propertyToken.Render(logEvent.Properties, space, CultureInfo.InvariantCulture);
                    renderings.Values.Add(new AnyValue { StringValue = space.ToString() });
                }

                logRecord.Attributes.Add(
                        CoralogixOtelPrimitiveConversions.NewAttribute(
                        CoralogixOtelSemanticConventions.AttributeMessageTemplateRenderings,
                        new AnyValue { ArrayValue = renderings }));
            }
        }
    }
}
