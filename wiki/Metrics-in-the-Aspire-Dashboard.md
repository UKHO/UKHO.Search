# Metrics in the Aspire dashboard

This page describes the application-level metrics emitted by `UKHO.Search` and how to view them in the .NET Aspire dashboard.

Scope:

- this page focuses on the custom `UKHO.Search` meters and instruments intended for ingestion pipeline visibility
- services also enable ASP.NET Core, `HttpClient`, and .NET runtime instrumentation through OpenTelemetry
- those built-in meters are visible in Aspire too, but are not exhaustively listed here

## Viewing metrics locally

1. Start `AppHost`:
   - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
2. Open the Aspire dashboard.
3. Select the service or resource you want to inspect.
4. Open the **Metrics** tab.

For Elasticsearch inspection beyond metrics, you can also open Kibana from the Aspire dashboard.

- use `kibana_admin` as the username
- use the `elastic-password` parameter value from the Aspire dashboard **Parameters** tab as the password

## How metrics are exported

All hosts that call `AddServiceDefaults()` automatically configure OpenTelemetry metrics.

The ingestion pipeline meter is explicitly subscribed in:

- `src/Hosts/UKHO.Search.ServiceDefaults/BuilderExtensions.cs`

through:

- `metrics.AddMeter("UKHO.Search.Ingestion.Pipeline")`

Without that subscription, the custom pipeline metrics below will not appear in Aspire.

## Meter: `UKHO.Search.Ingestion.Pipeline`

Purpose:

- end-to-end operational visibility of the ingestion pipeline
- node throughput, failures, drops, queueing, and latency
- dimensions that let you slice by provider and node

### Dimensions

All instruments emitted from this meter use the following tags.

| Tag | Type | Cardinality | Description |
|---|---:|---:|---|
| `node` | string | Medium | The pipeline node name, for example `ingestion-enrich-0`. |
| `provider` | string | Low | The ingestion provider name, for example `file-share`. Present only when the pipeline is built with a provider name. |

Notes:

- `provider` is sourced from `IIngestionDataProviderFactory.Name`
- the same `node` name can exist in multiple provider pipelines within the same process
- the pair `(provider, node)` uniquely identifies a node instance for observable gauges

### Instruments

| Instrument | Type | Unit | Tags | What it measures |
|---|---|---|---|---|
| `ukho.pipeline.node.in` | Counter (`long`) | `{item}` | `node`, `provider?` | Count of items accepted for processing by a node. Increments once per item read from input. |
| `ukho.pipeline.node.out` | Counter (`long`) | `{item}` | `node`, `provider?` | Count of items emitted by a node. Increments once per item written to the main output. |
| `ukho.pipeline.node.failed` | Counter (`long`) | `{item}` | `node`, `provider?` | Count of outputs where the envelope status is `Failed`. Recorded when a node writes an envelope whose status indicates failure. |
| `ukho.pipeline.node.dropped` | Counter (`long`) | `{item}` | `node`, `provider?` | Count of outputs where the envelope status is `Dropped`. Recorded when a node writes an envelope whose status indicates drop. |
| `ukho.pipeline.node.duration_ms` | Histogram (`double`) | `ms` | `node`, `provider?` | Per-item processing duration for a node, measured as wall-clock elapsed time around the node handler for each item. |
| `ukho.pipeline.node.inflight` | Observable gauge (`long`) | `{item}` | `node`, `provider?` | Current number of in-flight items for a node. Incremented when processing starts and decremented when processing finishes, including failure paths. Useful for spotting stuck or saturated nodes. |
| `ukho.pipeline.node.queue_depth` | Observable gauge (`long`) | `{item}` | `node`, `provider?` | Current input queue depth estimate for a node. If the input channel supports queue depth through `IQueueDepthProvider`, this is the live depth. Some nodes also provide a meaningful depth directly, for example micro-batching uses internal buffer length. If no depth provider exists, this emits `0`. |

## Practical usage in Aspire

Common ways to group or slice the metrics:

- by provider: compare throughput and latency across providers, such as `file-share` versus future providers
- by node: identify bottlenecks through high `duration_ms`, high `inflight`, or growing `queue_depth`
- by `(provider, node)`: isolate the same logical node role running in multiple provider pipelines

## Interpretation tips

- a sustained increase in `queue_depth` combined with elevated `duration_ms` usually indicates a slow node downstream
- `failed` and `dropped` should usually stay near zero in steady state; spikes often correlate with downstream availability problems or validation and enrichment issues
- `inflight` staying high for a single node can indicate a stuck item or a saturated node

## Dead-letter payloads and metric impact

Dead-letter persistence captures richer payload diagnostics, including runtime payload details and, for ingestion index-operation dead-letters, the Elasticsearch-facing GeoJSON payload shape used for `geoPolygons`.

Metric impact:

- there is currently no dedicated dead-letter meter or instrument for persisted dead-letter payload contents
- dead-letter payload fields such as `payloadDiagnostics`, payload snapshots, runtime payload type, and snapshot error details are not emitted as metric tags
- the richer dead-letter JSON therefore does not increase metrics cardinality in Aspire
- existing pipeline metrics remain the primary operational signal:
  - failures continue to appear through `ukho.pipeline.node.failed`
  - drops continue to appear through `ukho.pipeline.node.dropped`
  - slow or blocked persistence paths still surface indirectly through `duration_ms`, `inflight`, or `queue_depth`

Practical guidance:

- use Aspire metrics to detect that failures are occurring and where in the pipeline they are occurring
- use the dead-letter artifact itself to inspect why a specific item failed, including the final serialized payload shape sent toward Elasticsearch

## Built-in instrumentation meters

In addition to the custom meter above, hosts also enable:

- ASP.NET Core instrumentation through `AddAspNetCoreInstrumentation()`
- `HttpClient` instrumentation through `AddHttpClientInstrumentation()`
- .NET runtime instrumentation through `AddRuntimeInstrumentation()`

These contribute additional meters and instruments visible in Aspire, such as request duration, HTTP client timings, and GC or runtime counters.

For authoritative details on those meters and instruments, use the OpenTelemetry .NET instrumentation documentation for each component.

## Related pages

- [Project setup](Project-Setup)
- [Setup walkthrough](Setup-Walkthrough)
- [Setup troubleshooting](Setup-Troubleshooting)
- [Ingestion pipeline](Ingestion-Pipeline)
- [Documentation source map](Documentation-Source-Map)
