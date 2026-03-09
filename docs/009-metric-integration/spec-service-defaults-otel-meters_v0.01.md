# Specification: Register Pipeline Meters in Aspire OpenTelemetry (ServiceDefaults)

Version: v0.01  
Status: Draft  
Work Package: `docs/009-metric-integration/`

## 1. Summary
This document specifies how `UKHO.Search.ServiceDefaults` must be configured so OpenTelemetry subscribes to the ingestion pipeline metrics `Meter` and exports those metrics to the Aspire dashboard.

## 2. Goals
- Ensure ingestion pipeline metrics emitted via `System.Diagnostics.Metrics` are collected by OpenTelemetry.
- Ensure metrics are visible in the Aspire dashboard Metrics UI during local development.
- Centralise meter subscription in `UKHO.Search.ServiceDefaults` so all hosts that reference ServiceDefaults automatically export pipeline metrics.

## 3. Non-goals
- Adding a non-Aspire observability platform/exporter.
- Duplicating OpenTelemetry wiring in each host.

## 4. Requirements
### 4.1 Subscribe OpenTelemetry to the pipeline meter
- `UKHO.Search.ServiceDefaults` MUST subscribe OpenTelemetry to the pipeline node metrics `Meter` by adding the meter name to the OpenTelemetry metrics pipeline.
- Target meter name: `UKHO.Search.Ingestion.Pipeline`.

Rationale:
- OpenTelemetry does not automatically export all `Meter` instruments; it exports meters explicitly added (plus those from built-in instrumentations).

### 4.2 Implementation location
- The meter subscription MUST be applied within `UKHO.Search.ServiceDefaults` telemetry configuration.
- The subscription MUST be part of the default `AddServiceDefaults()` path so all hosts using ServiceDefaults automatically pick it up.

### 4.3 Compatibility with existing exporters
- The change MUST be compatible with existing exporter behaviour:
  - Local Aspire dashboard collection (default Aspire dev experience)
  - OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured (already supported by ServiceDefaults)

### 4.4 No host duplication
- Hosts (e.g., `IngestionServiceHost`, `QueryServiceHost`, etc.) MUST NOT need per-host meter configuration changes beyond referencing ServiceDefaults.

## 5. Acceptance criteria
- Running the solution under Aspire:
  - The Aspire dashboard shows pipeline node metrics in the Metrics section.
  - Metrics appear under the expected service(s) and can be filtered by `node` and `provider` tags.

## 6. Operational considerations
- The Aspire dashboard is intended primarily for local development; production deployments should use a secured observability platform.
- If production export is enabled via OTLP, confirm that the chosen backend supports the expected metrics volume and cardinality (provider/node tags are expected low-cardinality).

## 7. References
- NimblePros: "Enhancing Telemetry in Aspire" (adding custom metrics via ServiceDefaults and `AddMeter(...)`)
- NimblePros: "OTEL - An Introduction To OpenTelemetry" (custom metrics via `Meter` and counters/histograms)
