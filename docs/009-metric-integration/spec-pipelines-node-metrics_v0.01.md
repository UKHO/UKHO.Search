# Specification: Pipeline Node Metrics (Aspire/OpenTelemetry Integration)

Version: v0.01  
Status: Draft  
Work Package: `docs/009-metric-integration/`

## 1. Summary
This document specifies the pipeline node metrics contract required to ensure ingestion pipeline metrics are visible in the Aspire dashboard.

It covers:
- The stable `Meter` identity used for pipeline metrics.
- The set of metric instruments emitted by pipeline nodes.
- Dimension/tag conventions (low-cardinality) to support slicing by node and provider.

## 2. Goals
- Preserve the existing pipeline node metric instruments (counts, duration, gauges).
- Ensure pipeline node metrics are emitted under a stable `Meter` name that can be subscribed to by OpenTelemetry.
- Support slicing metrics by:
  - node name
  - ingestion provider name (low-cardinality)
- Avoid high-cardinality tags.

## 3. Non-goals
- Defining any new business-domain ingestion metrics beyond the existing pipeline node metrics.
- Adding per-message/request identifiers as metric dimensions.
- Replacing or re-platforming the pipeline execution model.

## 4. Background / evidence
- Pipeline nodes currently record metrics using `System.Diagnostics.Metrics` via `UKHO.Search.Pipelines.Metrics.NodeMetrics`.
- Aspire/OpenTelemetry will only export custom `Meter` metrics when the `Meter` is subscribed via OpenTelemetry configuration (see companion ServiceDefaults spec).

## 5. Requirements
### 5.1 Stable meter name
- The pipeline node metrics `Meter` name MUST be renamed from the current placeholder value to a stable name.
- Target meter name: `UKHO.Search.Ingestion.Pipeline`.

Rationale:
- The current name contains `Playground`, which is not suitable for long-term production identity.
- Aspire/OpenTelemetry subscriptions are based on meter name; stable names simplify configuration and discovery.

### 5.2 Instruments (no rename)
The following instruments MUST continue to be emitted (names unchanged):
- Counter: `ukho.pipeline.node.in`
- Counter: `ukho.pipeline.node.out`
- Counter: `ukho.pipeline.node.failed`
- Counter: `ukho.pipeline.node.dropped`
- Histogram: `ukho.pipeline.node.duration_ms`
- ObservableGauge: `ukho.pipeline.node.inflight`
- ObservableGauge: `ukho.pipeline.node.queue_depth`

### 5.3 Tagging (dimensions)
All pipeline node metrics MUST include the following low-cardinality tags:
- `node`: the pipeline node name (string)

In addition, pipeline node metrics emitted by ingestion provider processing graphs MUST include:
- `provider`: ingestion provider name (string), sourced from `IIngestionDataProviderFactory.Name`.
  - Example: `file-share`

Notes:
- Provider names MUST be drawn from a controlled, low-cardinality set (the set of registered ingestion providers).
- Provider tag values MUST NOT include per-request or per-item details.

### 5.4 Provider tag propagation model
- The provider tag MUST be determined at graph construction time (not per message) and applied consistently to all node metrics within that provider’s processing graph.

Implementation notes (normative intent, not code-level prescription):
- Pipeline nodes are currently instantiated with a node name only.
- To support provider tagging, pipeline node metrics creation MUST accept a provider name (optional for non-ingestion uses) so the tag can be applied to all metric instruments.

### 5.5 Collision avoidance
- The design MUST support multiple provider pipelines running in-process without tag collisions.
- Observable gauge providers (in-flight and queue depth) MUST be able to return measurements uniquely attributable to a `(provider, node)` pair.

## 6. Acceptance criteria
- When running the application under Aspire, pipeline node metrics are visible in the Aspire dashboard Metrics UI.
- Metrics can be sliced/grouped by:
  - `node`
  - `provider`
- Multiple providers (when present) do not overwrite each other’s gauges and counters.

## 7. Testing strategy
- Unit tests covering:
  - Provider tag presence for ingestion provider graphs.
  - Gauge observation correctness when two pipelines share node names but have different `provider` values.
  - No regressions in emitted instrument names.

## 8. References
- NimblePros: "OTEL - An Introduction To OpenTelemetry" (custom metrics via `Meter`)
- NimblePros: "Enhancing Telemetry in Aspire" (register custom meters in shared ServiceDefaults)
