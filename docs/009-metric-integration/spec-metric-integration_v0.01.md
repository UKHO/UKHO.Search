# Specification: Ingestion Pipeline Metrics → Aspire Dashboard

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

## 4. Pipeline node metrics contract
### 4.1 Summary
Defines the pipeline node metrics contract required to ensure ingestion pipeline metrics are visible in the Aspire dashboard.

### 4.2 Goals
- Preserve the existing pipeline node metric instruments (counts, duration, gauges).
- Ensure pipeline node metrics are emitted under a stable `Meter` name that can be subscribed to by OpenTelemetry.
- Support slicing metrics by:
  - node name
  - ingestion provider name (low-cardinality)
- Avoid high-cardinality tags.

### 4.3 Non-goals
- Defining any new business-domain ingestion metrics beyond the existing pipeline node metrics.
- Adding per-message/request identifiers as metric dimensions.
- Replacing or re-platforming the pipeline execution model.

### 4.4 Stable meter name
- The pipeline node metrics `Meter` name MUST be renamed from the current placeholder value to a stable name.
- Target meter name: `UKHO.Search.Ingestion.Pipeline`.

Rationale:
- The current name contains `Playground`, which is not suitable for long-term production identity.
- Aspire/OpenTelemetry subscriptions are based on meter name; stable names simplify configuration and discovery.

### 4.5 Instruments (no rename)
The following instruments MUST continue to be emitted (names unchanged):
- Counter: `ukho.pipeline.node.in`
- Counter: `ukho.pipeline.node.out`
- Counter: `ukho.pipeline.node.failed`
- Counter: `ukho.pipeline.node.dropped`
- Histogram: `ukho.pipeline.node.duration_ms`
- ObservableGauge: `ukho.pipeline.node.inflight`
- ObservableGauge: `ukho.pipeline.node.queue_depth`

### 4.6 Tagging (dimensions)
All pipeline node metrics MUST include the following low-cardinality tags:
- `node`: the pipeline node name (string)

In addition, pipeline node metrics emitted by ingestion provider processing graphs MUST include:
- `provider`: ingestion provider name (string), sourced from `IIngestionDataProviderFactory.Name`.
  - Example: `file-share`

Notes:
- Provider names MUST be drawn from a controlled, low-cardinality set (the set of registered ingestion providers).
- Provider tag values MUST NOT include per-request or per-item details.

### 4.7 Provider tag propagation model
- The provider tag MUST be determined at graph construction time (not per message) and applied consistently to all node metrics within that provider’s processing graph.

### 4.8 Collision avoidance
- The design MUST support multiple provider pipelines running in-process without tag collisions.
- Observable gauge providers (in-flight and queue depth) MUST be able to return measurements uniquely attributable to a `(provider, node)` pair.

## 5. ServiceDefaults (OpenTelemetry / Aspire wiring)
### 5.1 Summary
Defines how `UKHO.Search.ServiceDefaults` must be configured so OpenTelemetry subscribes to the ingestion pipeline metrics `Meter` and exports those metrics to the Aspire dashboard.

### 5.2 Goals
- Ensure ingestion pipeline metrics emitted via `System.Diagnostics.Metrics` are collected by OpenTelemetry.
- Ensure metrics are visible in the Aspire dashboard Metrics UI during local development.
- Centralise meter subscription in `UKHO.Search.ServiceDefaults` so all hosts that reference ServiceDefaults automatically export pipeline metrics.

### 5.3 Non-goals
- Adding a non-Aspire observability platform/exporter.
- Duplicating OpenTelemetry wiring in each host.

### 5.4 Requirements
#### 5.4.1 Subscribe OpenTelemetry to the pipeline meter
- `UKHO.Search.ServiceDefaults` MUST subscribe OpenTelemetry to the pipeline node metrics `Meter` by adding the meter name to the OpenTelemetry metrics pipeline.
- Target meter name: `UKHO.Search.Ingestion.Pipeline`.

Rationale:
- OpenTelemetry does not automatically export all `Meter` instruments; it exports meters explicitly added (plus those from built-in instrumentations).

#### 5.4.2 Implementation location
- The meter subscription MUST be applied within `UKHO.Search.ServiceDefaults` telemetry configuration.
- The subscription MUST be part of the default `AddServiceDefaults()` path so all hosts using ServiceDefaults automatically pick it up.

#### 5.4.3 Compatibility with existing exporters
- The change MUST be compatible with existing exporter behaviour:
  - Local Aspire dashboard collection (default Aspire dev experience)
  - OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured (already supported by ServiceDefaults)

#### 5.4.4 No host duplication
- Hosts (e.g., `IngestionServiceHost`, `QueryServiceHost`, etc.) MUST NOT need per-host meter configuration changes beyond referencing ServiceDefaults.

## 6. Acceptance criteria
- Running the solution under Aspire:
  - The Aspire dashboard shows pipeline node metrics in the Metrics section.
  - Metrics appear under the expected service(s) and can be filtered by `node` and `provider` tags.
- Multiple providers (when present) do not overwrite each other’s gauges and counters.

## 7. Operational considerations
- The Aspire dashboard is intended primarily for local development; production deployments should use a secured observability platform.
- If production export is enabled via OTLP, confirm that the chosen backend supports the expected metrics volume and cardinality (provider/node tags are expected low-cardinality).

## 8. Testing strategy
- Unit tests covering:
  - Provider tag presence for ingestion provider graphs.
  - Gauge observation correctness when two pipelines share node names but have different `provider` values.
  - No regressions in emitted instrument names.

## 9. References
- NimblePros: "OTEL - An Introduction To OpenTelemetry" (custom metrics via `Meter`)
- NimblePros: "Enhancing Telemetry in Aspire" (register custom meters in shared ServiceDefaults)
