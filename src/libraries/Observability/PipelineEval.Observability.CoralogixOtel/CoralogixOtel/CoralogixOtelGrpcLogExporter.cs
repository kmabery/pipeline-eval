// Copyright 2022 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Vendored from serilog/serilog-sinks-opentelemetry v4.2.0 GrpcExporter: logs only, using
// gRPC code generated from opentelemetry-proto (public types).

using Grpc.Core;
using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace PipelineEval.Observability.CoralogixOtel;

/// <summary>
/// Sends <see cref="ExportLogsServiceRequest"/> to an OTLP gRPC logs endpoint.
/// </summary>
public sealed class CoralogixOtelGrpcLogExporter : IDisposable
{
    readonly GrpcChannel? _channel;
    readonly LogsService.LogsServiceClient? _client;
    readonly Metadata _headers;

    public CoralogixOtelGrpcLogExporter(
        string logsEndpoint,
        IReadOnlyDictionary<string, string> headers,
        HttpMessageHandler? httpMessageHandler = null)
    {
        var options = new GrpcChannelOptions();
        if (httpMessageHandler != null)
        {
            options.HttpClient = new HttpClient(httpMessageHandler);
            options.DisposeHttpClient = true;
        }

        _channel = GrpcChannel.ForAddress(logsEndpoint, options);
        _client = new LogsService.LogsServiceClient(_channel);

        _headers = new Metadata();
        foreach (var header in headers)
            _headers.Add(header.Key, header.Value);
    }

    public void Dispose() => _channel?.Dispose();

    public void Export(ExportLogsServiceRequest request) =>
        _client?.Export(request, _headers);

    public Task ExportAsync(ExportLogsServiceRequest request) =>
        _client?.ExportAsync(request, _headers).ResponseAsync ?? Task.CompletedTask;
}
