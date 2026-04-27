# Log attributes and conventions

Structured logging uses **Serilog** with enrichers registered in `PipelineEval.Observability`. Align property names with this table so Coralogix queries stay stable.

## Required enricher properties (API / shared library)

| Property        | Source                          | Example        | Notes                                      |
|-----------------|---------------------------------|----------------|--------------------------------------------|
| `application`   | `Observability:ApplicationName` | `PipelineEval` | Logical product / app name.              |
| `subsystem`     | `Observability:SubSystem`        | `api`         | Deployable slice (e.g. `api`, `worker`).   |

## OpenTelemetry resource → log correlation

Resource attributes `cx.application.name` and `cx.subsystem.name` (see `traces.md`) are mirrored on the OTLP pipeline where the sink supports them; the Serilog enrichers above duplicate the same values as scalar properties for console and OTLP log records.

## Optional request / domain fields (extend per feature)

| Property        | When to set                    | Example              |
|-----------------|--------------------------------|----------------------|
| `TraceId`       | When `Activity.Current` exists | OpenTelemetry trace id |
| `SpanId`        | When `Activity.Current` exists | Span id              |

Do **not** log secrets, raw tokens, or full connection strings. Use redaction or hashes for identifiers if needed.
