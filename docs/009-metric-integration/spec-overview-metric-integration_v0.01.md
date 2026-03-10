# Specification: Ingestion Pipeline Metrics → Aspire Dashboard (Overview)

Version: v0.01  
Status: Draft  
Work Package: `docs/009-metric-integration/`

## 1. Purpose
Expose ingestion pipeline metrics in the .NET Aspire dashboard by ensuring existing pipeline metrics are emitted via OpenTelemetry-compatible `Meter`/instruments and are subscribed to by Aspire/OpenTelemetry configuration.

## 2. Scope
This work package covers:
- Standardising the pipeline metrics `Meter` identity to a stable name suitable for long-term use.
- Extending pipeline node metrics dimensions to support provider-level slicing in the Aspire dashboard.
- Registering the pipeline metrics `Meter` via the existing `UKHO.Search.ServiceDefaults` OpenTelemetry configuration so all hosts referencing ServiceDefaults export the metrics.

Out of scope (initially):
- Introducing an observability platform beyond Aspire dashboard for local development.
- Designing bespoke dashboards beyond what Aspire provides.

## 3. High-level design
### Components
- `UKHO.Search` (Domain)
  - Hosts the pipeline framework and emits node-level runtime metrics via `System.Diagnostics.Metrics`.
  - Provides the metrics `Meter` and instruments used by pipeline nodes.

- `UKHO.Search.Ingestion.*` (Domain) and ingestion provider assemblies
  - Constructs provider-specific ingestion processing graphs.
  - Supplies a low-cardinality provider dimension (sourced from provider factory `Name`, e.g. `file-share`) to pipeline node metrics.

- `UKHO.Search.ServiceDefaults` (Hosts shared defaults)
  - Central place for OpenTelemetry configuration.
  - Subscribes OpenTelemetry to the pipeline metrics `Meter` so Aspire collects and displays pipeline metrics.

### Component specifications
- `docs/009-metric-integration/spec-pipelines-node-metrics_v0.01.md`
- `docs/009-metric-integration/spec-service-defaults-otel-meters_v0.01.md`
