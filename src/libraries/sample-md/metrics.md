# Metrics attributes and conventions

Metrics are emitted via **OpenTelemetry** (`Meter` / instruments) and OTLP when `Observability:UseLocal` is `false` and `Observability:ApiKey` is set.

## Meter names

| Meter name                         | Owner project              | Description                    |
|------------------------------------|----------------------------|--------------------------------|
| `PipelineEval.SampleWindowsService` | `SampleWindowsService`     | Sample counter for local demos |

Add new meters under `PipelineEval.*` — **do not** use a vendor prefix in meter or instrument names.

## Instruments (examples)

| Instrument name                     | Type    | Unit | Labels / attributes        |
|-------------------------------------|---------|------|----------------------------|
| `pipelineeval.sample_windows.ticks`  | Counter | `1`  | `subsystem` = `sample-windows-service` |

## Label keys

| Key         | Meaning                          | Example values              |
|-------------|----------------------------------|-----------------------------|
| `subsystem` | Mirrors `Observability:SubSystem` | `api`, `sample-windows-service` |

Keep label cardinality low; avoid unbounded user ids as label values.
