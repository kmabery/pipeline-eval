# Traces (spans) attributes and conventions

Tracing uses **OpenTelemetry** with ASP.NET Core and HTTP client instrumentation when OTLP export is enabled (`Observability:ApiKey` + `UseLocal: false`).

## Resource attributes (Coralogix-oriented)

These keys are set on the resource in `PipelineEval.Observability` and should stay consistent across services.

| Attribute key           | Config source                    | Example        |
|-------------------------|----------------------------------|----------------|
| `service.name`          | `Observability:ServiceName`      | `PipelineEval.Api` |
| `cx.application.name`   | `Observability:ApplicationName`  | `PipelineEval`  |
| `cx.subsystem.name`     | `Observability:SubSystem`        | `api`          |

## Span tags (samples)

| Tag key       | When                    | Example        |
|---------------|-------------------------|----------------|
| `sample.ui`   | Optional UI experiments | `winforms`     |

Use **semantic conventions** from OpenTelemetry for HTTP (`http.request.method`, `url.scheme`, etc.) where the instrumentation provides them.

## Activity sources

When adding custom `ActivitySource` / manual spans, use names prefixed with `PipelineEval.` (for example `PipelineEval.SampleWinForms`) so they are easy to filter in the backend.
