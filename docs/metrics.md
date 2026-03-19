# Metrics in the Aspire Dashboard

This document describes the **application-level metrics** emitted by UKHO.Search and how to view them in the **.NET Aspire dashboard**.

> Scope: this focuses on the **custom UKHO.Search meters/instruments** intended for ingestion pipeline visibility.
> The services also enable ASP.NET Core, `HttpClient`, and .NET runtime instrumentation via OpenTelemetry; those meters are visible in Aspire too, but are not exhaustively listed here.

## Viewing metrics locally (Aspire)

1. Start the AppHost:
   - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
2. Open the Aspire dashboard.
3. Select the service/resource you want to inspect.
4. Go to **Metrics**.

## How metrics are exported

All hosts that call `AddServiceDefaults()` automatically configure OpenTelemetry metrics.

The ingestion pipeline meter is explicitly subscribed in `src/Hosts/UKHO.Search.ServiceDefaults/BuilderExtensions.cs` via:

- `metrics.AddMeter("UKHO.Search.Ingestion.Pipeline")`

Without this subscription, the custom pipeline metrics below will not appear in Aspire.

---

## Meter: `UKHO.Search.Ingestion.Pipeline`

**Purpose**: end-to-end operational visibility of the ingestion pipeline (pipeline node throughput, failures, and latency), with dimensions to slice by **provider** and **node**.

### Dimensions (tags)

All instruments emitted from this meter use the following tags:

| Tag | Type | Cardinality | Description |
|---|---:|---:|---|
| `node` | string | Medium (bounded by node names) | The pipeline node name (for example `ingestion-enrich-0`). |
| `provider` | string | Low | The ingestion provider name (for example `file-share`). Present only when the pipeline is built with a provider name (provider-specific graphs). |

Notes:
- `provider` is sourced from `IIngestionDataProviderFactory.Name` (for example, file-share provider uses `file-share`).
- The same `node` name can exist in multiple provider pipelines within the same process; `(provider, node)` uniquely identifies a node instance for observable gauges.

### Instruments

| Instrument | Type | Unit | Tags | What it measures |
|---|---|---|---|---|
| `ukho.pipeline.node.in` | Counter (long) | `{item}` | `node`, `provider?` | Count of items **accepted for processing** by a node (increments once per item read from input). |
| `ukho.pipeline.node.out` | Counter (long) | `{item}` | `node`, `provider?` | Count of items **emitted** by a node (increments once per item written to the main output). |
| `ukho.pipeline.node.failed` | Counter (long) | `{item}` | `node`, `provider?` | Count of outputs where the envelope status is `Failed`. This is recorded when a node writes an envelope whose status indicates failure. |
| `ukho.pipeline.node.dropped` | Counter (long) | `{item}` | `node`, `provider?` | Count of outputs where the envelope status is `Dropped`. This is recorded when a node writes an envelope whose status indicates drop. |
| `ukho.pipeline.node.duration_ms` | Histogram (double) | `ms` | `node`, `provider?` | Per-item processing duration for a node (wall-clock elapsed time around the node handler for each item). |
| `ukho.pipeline.node.inflight` | Observable gauge (long) | `{item}` | `node`, `provider?` | Current number of in-flight items for a node.
A node increments this when it begins processing an item and decrements when it finishes (even on failure).
Useful for spotting stuck/slow nodes or backpressure. |
| `ukho.pipeline.node.queue_depth` | Observable gauge (long) | `{item}` | `node`, `provider?` | Current **input queue depth estimate** for a node.
For nodes whose input channel supports queue depth (implements `IQueueDepthProvider`), this is the live queue depth.
Some nodes also provide a meaningful depth (for example micro-batching uses the internal buffer length).
If no depth provider exists, this emits `0`. |

### Practical usage in Aspire

Common ways to slice/group:

- **By provider**: compare ingestion throughput/latency across providers (for example `file-share` vs future providers).
- **By node**: identify bottlenecks (high `duration_ms`, high `inflight`, or growing `queue_depth`).
- **By (provider, node)**: isolate the same node role running in multiple provider pipelines.

### Interpretation tips

- A sustained increase in `queue_depth` combined with elevated `duration_ms` usually indicates a slow node downstream.
- `failed`/`dropped` counters should typically be near zero in steady-state; spikes often correlate with downstream availability or validation/enrichment issues.
- `inflight` staying high for a single node can indicate an item is stuck (or the node is saturated).

### Dead-letter payloads and metric impact

Dead-letter persistence now captures richer payload diagnostics, including runtime payload details and, for ingestion index-operation dead-letters, the Elasticsearch-facing GeoJSON payload shape used for `geoPolygons`.

Metric impact:

- There is currently **no dedicated dead-letter meter or instrument** for persisted dead-letter payload contents.
- Dead-letter payload fields such as `payloadDiagnostics`, payload snapshots, runtime payload type, and snapshot error details are **not emitted as metric tags**.
- The richer dead-letter JSON therefore does **not increase metrics cardinality** in Aspire.
- Existing pipeline metrics remain the primary operational signal:
  - failures continue to show up through `ukho.pipeline.node.failed`
  - drops continue to show up through `ukho.pipeline.node.dropped`
  - slow or blocked persistence paths would still surface indirectly through `duration_ms`, `inflight`, or `queue_depth`

Practical guidance:

- Use Aspire metrics to detect **that** failures are occurring and where in the pipeline they are occurring.
- Use the dead-letter artifact itself to inspect **why** a specific item failed, including the final serialized payload shape sent toward Elasticsearch.

---

## Notes on built-in instrumentation meters

In addition to the custom meter above, hosts enable:

- ASP.NET Core instrumentation (`AddAspNetCoreInstrumentation()`)
- `HttpClient` instrumentation (`AddHttpClientInstrumentation()`)
- .NET runtime instrumentation (`AddRuntimeInstrumentation()`)

These contribute additional meters and instruments visible in Aspire (for example request duration, HTTP client timings, GC/runtime counters).
For authoritative details on those meters/instruments, refer to the OpenTelemetry .NET instrumentation documentation for each component.
