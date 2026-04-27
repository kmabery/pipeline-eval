// Copyright 2022 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Vendored from serilog/serilog-sinks-opentelemetry v4.2.0 OpenTelemetryLogsSink, using
// <see cref="CoralogixOtlpEventBuilder"/> for log record mapping.

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace PipelineEval.Observability.CoralogixOtel;

/// <summary>
/// Serilog sink: Serilog <see cref="LogEvent"/> to OTLP logs, with Coralogix map-shaped <c>body.message</c>.
/// </summary>
public sealed class CoralogixOpenTelemetryLogsSink : IBatchedLogEventSink, ILogEventSink, IDisposable
{
    readonly IFormatProvider? _formatProvider;
    readonly ResourceLogs _resourceLogsTemplate;
    readonly CoralogixOtelGrpcLogExporter _exporter;
    readonly IncludedData _includedData;

    public CoralogixOpenTelemetryLogsSink(
        CoralogixOtelGrpcLogExporter exporter,
        IFormatProvider? formatProvider,
        IReadOnlyDictionary<string, object> resourceAttributes,
        IncludedData includedData)
    {
        _exporter = exporter;
        _formatProvider = formatProvider;
        _includedData = includedData;
        _resourceLogsTemplate = CoralogixOtelRequestTemplateFactory.CreateResourceLogs(resourceAttributes);
    }

    public void Dispose() => _exporter.Dispose();

    public Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        var resourceLogs = _resourceLogsTemplate.Clone();

        ScopeLogs? logsAnonymousScope = null;
        Dictionary<string, ScopeLogs>? logsNamedScopes = null;

        foreach (var logEvent in batch)
        {
            var (logRecord, scopeName) = CoralogixOtlpEventBuilder.ToLogRecord(logEvent, _formatProvider, _includedData);
            if (scopeName == null)
            {
                if (logsAnonymousScope == null)
                {
                    logsAnonymousScope = CoralogixOtelRequestTemplateFactory.CreateScopeLogs(null);
                    resourceLogs.ScopeLogs.Add(logsAnonymousScope);
                }

                logsAnonymousScope.LogRecords.Add(logRecord);
            }
            else
            {
                logsNamedScopes ??= new Dictionary<string, ScopeLogs>();
                if (!logsNamedScopes.TryGetValue(scopeName, out var namedScope))
                {
                    namedScope = CoralogixOtelRequestTemplateFactory.CreateScopeLogs(scopeName);
                    logsNamedScopes.Add(scopeName, namedScope);
                    resourceLogs.ScopeLogs.Add(namedScope);
                }

                namedScope.LogRecords.Add(logRecord);
            }
        }

        var logsRequest = new ExportLogsServiceRequest();
        logsRequest.ResourceLogs.Add(resourceLogs);
        return _exporter.ExportAsync(logsRequest);
    }

    public void Emit(LogEvent logEvent)
    {
        var (logRecord, scopeName) = CoralogixOtlpEventBuilder.ToLogRecord(logEvent, _formatProvider, _includedData);
        var scopeLogs = CoralogixOtelRequestTemplateFactory.CreateScopeLogs(scopeName);
        scopeLogs.LogRecords.Add(logRecord);
        var resourceLogs = _resourceLogsTemplate.Clone();
        resourceLogs.ScopeLogs.Add(scopeLogs);
        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(resourceLogs);
        _exporter.Export(request);
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;
}
