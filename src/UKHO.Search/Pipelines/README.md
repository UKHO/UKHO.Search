# UKHO.Search Pipelines – Developer How-To

This document explains how to use the in-process, channel-based pipeline framework in `UKHO.Search` to build ingestion and processing pipelines inside a service.

It is intended for developers who need to get productive quickly:

- how the pipeline model works
- how to assemble and run a pipeline
- how backpressure (and throttling) works
- how error handling works (message-scoped vs fatal)
- how to implement new node functionality safely
- what each provided node type does

> The pipeline framework is **Domain-layer** code. It is designed to be hosted by a service (e.g., a BackgroundService) but is also heavily exercised via tests.

---

## 1. Mental model

A pipeline is a graph of long-running **nodes** connected by **bounded channels**:

- Each node has a `Name`, a `StartAsync(...)`, a `Completion` task, and an optional `StopAsync(...)`.
- Nodes read from one or more `ChannelReader<T>` inputs and write to one or more `ChannelWriter<T>` outputs.
- Channels are bounded and configured with `BoundedChannelFullMode.Wait`, so slow downstream stages automatically apply **backpressure** upstream.

### 1.1 Key concepts: envelope + status

All “messages” flowing through the pipeline are typically wrapped in an `Envelope<TPayload>` (`UKHO.Search.Pipelines.Messaging`).

Key envelope fields:

- `Key` (string): ordering / partition key (e.g., document id)
- `MessageId` (Guid): stable identity for the message through the graph
- `TimestampUtc` (DateTimeOffset): created-at timestamp
- `Attempt` (int): attempt counter (used for retry)
- `Status` (`MessageStatus`): `Ok`, `Failed`, `Dropped`, `Retrying`
- `Error` (`PipelineError?`): populated when `Status != Ok`
- `Context` (`MessageContext`): mutable per-message context for breadcrumbs/timings

Rule of thumb:

- **Expected per-message failures** should be represented as `Envelope.Status != Ok` and `Envelope.Error != null`.
- **Truly unexpected failures** (bugs, broken invariants, fatal runtime errors) should throw and be handled by the node base types / supervisor.

---

## 2. Creating channels (backpressure + queue depth)

All inter-node channels should be **bounded**.

Use `BoundedChannelFactory.Create<T>(...)` (`UKHO.Search.Pipelines.Channels`) to create a bounded channel pair:

- The channel is created with `BoundedChannelFullMode.Wait` so `WriteAsync(...)` naturally blocks when the channel is full.
- The factory returns a `CountingChannel<T>` wrapper whose `Reader` implements `IQueueDepthProvider`.
  - Base node types automatically publish this queue depth via `NodeMetrics`.

Example:

```csharp
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;

var validateInput = BoundedChannelFactory.Create<Envelope<MyPayload>>(
	capacity: 1024,
	singleReader: true,
	singleWriter: true);

// validateInput.Reader / validateInput.Writer
```

### 2.1 Choosing capacity

Capacity determines both throughput and memory usage:

- Larger capacity smooths bursts but increases memory pressure.
- Smaller capacity reduces buffering and makes backpressure kick in earlier.

A practical starting point (tune with real payload sizes):

- pre-partition stages: 1,000–10,000
- per-partition lane stages: 100–1,000
- batch channels: small (10–100)

---

## 3. Assembling a pipeline

A typical keyed, ordered ingestion graph looks like:

```mermaid
flowchart LR
  SRC[Source] --> VAL[ValidateNode]
  VAL --> PART[KeyPartitionNode (N lanes)]
  PART -->|lane 0..N-1| XFORM[TransformNode]
  XFORM --> MB[MicroBatchNode]
  MB --> BULK[BulkIndexNode]

  VAL -->|failed| DLQ[DeadLetterSinkNode]
  BULK -->|failed| DLQ
```

### 3.1 Core wiring steps

1. Create a `PipelineSupervisor`.
2. Create bounded channels between each stage.
3. Construct nodes, passing:
   - `fatalErrorReporter: supervisor` to ensure fatal errors are captured and cancel the pipeline.
   - `logger: ...` (optional) for structured logs.
4. Register nodes in the supervisor via `AddNode(...)`.
   - Node names must be unique.
5. Start the supervisor (`await supervisor.StartAsync()`), then await completion.

### 3.2 Example: a minimal pipeline (in a service)

This example shows the *shape* of the wiring. In a real service you’ll also:

- inject dependencies via DI
- provide real source(s)
- implement real transforms

```csharp
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

public sealed class MyPipeline
{
	private readonly ILogger logger;

	public MyPipeline(ILogger<MyPipeline> logger)
	{
		this.logger = logger;
	}

	public async Task RunAsync(CancellationToken cancellationToken)
	{
		var supervisor = new PipelineSupervisor(cancellationToken);

		var srcToValidate = BoundedChannelFactory.Create<Envelope<int>>(capacity: 1024, singleReader: true, singleWriter: true);
		var validateToPartition = BoundedChannelFactory.Create<Envelope<int>>(capacity: 1024, singleReader: true, singleWriter: true);
		var validateToDeadLetter = BoundedChannelFactory.Create<Envelope<int>>(capacity: 256, singleReader: true, singleWriter: true);

		var partitionToTransform = BoundedChannelFactory.Create<Envelope<int>>(capacity: 256, singleReader: true, singleWriter: true);
		var transformToSink = BoundedChannelFactory.Create<Envelope<int>>(capacity: 256, singleReader: true, singleWriter: true);

		var source = new SyntheticSourceNode<int>(
			name: "source",
			output: srcToValidate.Writer,
			messageCount: 100,
			keyCardinality: 4,
			payloadFactory: i => i,
			logger: logger,
			fatalErrorReporter: supervisor);

		var validate = new ValidateNode<int>(
			name: "validate",
			input: srcToValidate.Reader,
			output: validateToPartition.Writer,
			errorOutput: validateToDeadLetter.Writer,
			forwardFailedToMainOutput: false,
			logger: logger,
			fatalErrorReporter: supervisor);

		var partition = new KeyPartitionNode<int>(
			name: "partition",
			input: validateToPartition.Reader,
			outputs: new[] { partitionToTransform.Writer },
			logger: logger,
			fatalErrorReporter: supervisor);

		var transform = new TransformNode<int, int>(
			name: "transform",
			input: partitionToTransform.Reader,
			output: transformToSink.Writer,
			transform: (value, ct) => new ValueTask<int>(value + 1),
			faultPipelineOnException: false,
			logger: logger,
			fatalErrorReporter: supervisor);

		var sink = new CollectingSinkNode<int>(
			name: "sink",
			input: transformToSink.Reader,
			perMessageDelay: TimeSpan.Zero,
			logger: logger,
			fatalErrorReporter: supervisor);

		var deadLetter = new DeadLetterSinkNode<int>(
			name: "dead-letter",
			input: validateToDeadLetter.Reader,
			filePath: "./deadletter.jsonl",
			fatalIfCannotPersist: true,
			logger: logger,
			fatalErrorReporter: supervisor);

		supervisor.AddNode(source);
		supervisor.AddNode(validate);
		supervisor.AddNode(partition);
		supervisor.AddNode(transform);
		supervisor.AddNode(sink);
		supervisor.AddNode(deadLetter);

		await supervisor.StartAsync();
		await supervisor.Completion.WaitAsync(cancellationToken);
	}
}
```

> In a real pipeline, you will typically use multiple partitions (lanes) and wire per-lane nodes after `KeyPartitionNode` to preserve strict ordering.

---

## 4. Running and stopping a pipeline

### 4.1 Starting

- Register all nodes with the `PipelineSupervisor`.
- Call `await supervisor.StartAsync()`.
- Await `supervisor.Completion`.

### 4.2 Cancellation

The supervisor creates a linked `CancellationTokenSource`. On cancellation (or a fatal error), that token is triggered and is passed to all nodes.

### 4.3 StopAsync

`PipelineSupervisor.StopAsync(ct)` is defined to:

1. cancel the pipeline, and
2. forward `StopAsync(ct)` to all nodes.

Most nodes have a safe no-op default `StopAsync`. Override it only if you have explicit resources to close or custom shutdown hooks.

---

## 5. Backpressure and throttling

### 5.1 Backpressure (how it works)

Backpressure is implemented by **bounded channels** with `BoundedChannelFullMode.Wait`:

- Every node writes via `ChannelWriter.WriteAsync(...)`.
- If the downstream channel is full, `WriteAsync(...)` awaits until space is available.
- This naturally slows upstream nodes (and, by extension, sources).

In node implementations, this means:

- avoid using `TryWrite` unless you are explicitly implementing “best effort” semantics
- expect `WriteAsync` to block under load; this is normal and desirable

### 5.2 Fan-out backpressure (Broadcast)

`BroadcastNode<TIn>` can enforce two modes:

- `AllMustReceive`: backpressure is the **maximum** of downstream pressure (broadcast slows if any output is slow).
- `BestEffort`: required outputs are strict, optional outputs are attempted using `TryWrite` and are skipped if backpressured.

### 5.3 Throttling patterns

“Throttling” typically means intentionally reducing throughput.

You can throttle in three common ways:

1. **Reduce channel capacities**
   - upstream blocks sooner and you reduce buffering.

2. **Reduce concurrency by design**
   - keep per-lane processing sequential (default) and increase/decrease number of partitions.

3. **Delay in sinks (test / simulation)**
   - `CollectingSinkNode<T>` supports a `perMessageDelay` which is useful for backpressure tests.

If you need explicit throttling for a dependency (e.g., limiting calls to an external API), implement it inside your node with a `SemaphoreSlim` or a token bucket approach, but keep the envelope semantics the same.

---

## 6. Error handling model

The framework distinguishes between:

### 6.1 Message-scoped errors (expected)

These errors are represented on the `Envelope`:

- `envelope.MarkFailed(error)` → terminal failure
- `envelope.MarkDropped(reason, nodeName)` → intentionally filtered
- `envelope.MarkRetrying(error)` → transient failure that will be retried

Message-scoped errors do **not** crash the node and do **not** stop the pipeline.

**Recommended pattern**: provide a dedicated error output channel and route failed envelopes to it (e.g., from `ValidateNode`, `RetryingTransformNode`, `BulkIndexNode`).

### 6.2 Fatal node/pipeline errors (unexpected)

Fatal errors are unexpected exceptions thrown by node worker loops.

For base-node implementations (`NodeBase`, `SinkNodeBase`, `MultiInputNodeBase`, `SourceNodeBase`):

- exceptions are caught at the top of the loop
- the node reports fatal to the supervisor (`IPipelineFatalErrorReporter`)
- output channels are completed with the exception (fault propagation)
- the supervisor cancels the pipeline (fail-fast)

This results in deterministic shutdown rather than partial progress.

---

## 7. How to implement a new node

### 7.1 Choose the correct base type

Prefer using base types unless you have a strong reason not to:

- `SourceNodeBase<TOut>`: no input, one output
- `NodeBase<TIn, TOut>`: one input, one output
- `SinkNodeBase<TIn>`: one input, no output
- `MultiInputNodeBase<T1, T2, TOut>`: two inputs, one output (merge/fan-in)

The base types provide:

- the read loop (`WaitToReadAsync` + `TryRead`)
- completion propagation
- fatal exception capture + propagation
- metrics scaffolding via `NodeMetrics`
- optional queue depth wiring (when using `BoundedChannelFactory`)

### 7.2 Node implementation rules

1. **Never throw for expected per-message failures**
   - mark `Envelope.Status` and populate `Envelope.Error`.

2. **Preserve envelope identity**
   - `MessageId`, `Key`, `TimestampUtc`, `Attempt` should remain meaningful across stages.

3. **Breadcrumbs and timing**
   - use `item.Context.AddBreadcrumb(Name)` and `item.Context.MarkTimeUtc(...)` where helpful.

4. **Respect backpressure**
   - always use `await WriteAsync(...)` (or `ChannelWriter.WriteAsync(...)`) unless you are explicitly implementing optional/best-effort behavior.

5. **Completion propagation**
   - let the base class complete outputs when input completes.

6. **Cancellation**
   - respond to cancellation promptly.
   - for buffering nodes, consider `CancellationMode.Drain` semantics (see `MicroBatchNode`).

### 7.3 Skeleton: a simple transform-style node

```csharp
using System.Threading.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;

public sealed class MyNode : NodeBase<Envelope<int>, Envelope<int>>
{
	public MyNode(string name, ChannelReader<Envelope<int>> input, ChannelWriter<Envelope<int>> output)
		: base(name, input, output)
	{
	}

	protected override async ValueTask HandleItemAsync(Envelope<int> item, CancellationToken cancellationToken)
	{
		item.Context.AddBreadcrumb(Name);

		if (item.Status != MessageStatus.Ok)
		{
			await WriteAsync(item, cancellationToken);
			return;
		}

		// Implement per-message error handling by marking the envelope instead of throwing.
		var updated = item.MapPayload(item.Payload + 1);
		await WriteAsync(updated, cancellationToken);
	}
}
```

---

## 8. Node catalog (what each node does)

This section describes the node types currently implemented in `UKHO.Search.Pipelines.Nodes`.

### 8.1 Base types

#### `SourceNodeBase<TOut>`

- Produces items into an output channel.
- Reports fatal errors via supervisor.
- Completes output when production ends.

#### `NodeBase<TIn, TOut>`

- Reads from one input channel and writes to one output channel.
- Provides `WriteAsync(...)` helper that records metrics.
- Handles completion propagation and fatal exception propagation.

#### `SinkNodeBase<TIn>`

- Reads from one input channel.
- No output.
- Provides metrics and fatal error handling.

#### `MultiInputNodeBase<T1, T2, TOut>`

- Reads fairly from two input channels.
- Provides `WriteAsync(...)` helper and consistent completion/fault propagation.
- Publishes queue depth as the sum of both inputs (when available).

### 8.2 Source nodes

#### `SyntheticSourceNode<TPayload>`

Purpose:

- Generates synthetic test data (envelopes) for pipeline tests and local experimentation.

Key behaviors:

- Creates `Envelope<T>` with stable key distribution (`keyCardinality`).
- Writes respecting backpressure.

### 8.3 Validation and transformation

#### `ValidateNode<TPayload>`

Purpose:

- Validates envelope shape (currently: key must be non-empty) and marks failures as message-scoped errors.

Key behaviors:

- If `Key` is empty/whitespace and status is `Ok`, marks as `Failed` with `PipelineErrorCategory.Validation`.
- If an `errorOutput` is configured and status is `Failed`, writes to `errorOutput`.
- If `forwardFailedToMainOutput` is `true`, failed messages also flow to main output; otherwise they do not.

#### `TransformNode<TIn, TOut>`

Purpose:

- Maps `Envelope<TIn>` → `Envelope<TOut>` using a provided transform function.

Key behaviors:

- Passes through non-`Ok` envelopes (maps payload to default and preserves error/status).
- On transform exception:
  - if `faultPipelineOnException` is `true`, rethrows (fatal)
  - otherwise marks message as `Failed` (`TRANSFORM_ERROR`) and forwards

#### `RetryingTransformNode<TIn, TOut>`

Purpose:

- Like `TransformNode`, but supports strict in-order retry.

Key behaviors:

- Executes transform inside a loop.
- On exception:
  - creates a `PipelineError` and consults `IRetryPolicy.ShouldRetry(envelope, error)`
  - if retryable, marks `Retrying`, increments `Attempt`, delays via `retryPolicy.GetDelay(attempt)`, and retries **without reading the next item**
  - if not retryable, marks `Failed` and optionally writes to `errorOutput`

This design preserves strict per-lane ordering.

### 8.4 Ordering and lane partitioning

#### `KeyPartitionNode<TPayload>`

Purpose:

- Partitions envelopes to N output lanes based on a stable hash of `Envelope.Key`.

Key behaviors:

- Uses a stable FNV-1a hash over UTF-8 bytes.
- Writes each envelope to exactly one output writer: `outputs[partition]`.

Ordering:

- If all messages for a given key always go through the same partitioner configuration, FIFO order is preserved within that lane.

### 8.5 Batching and bulk

#### `MicroBatchNode<TPayload>`

Purpose:

- Buffers per-lane envelopes into `BatchEnvelope<TPayload>` for efficiency.

Triggers:

- `maxItems` (required)
- `maxDelay` (required)
- `maxBytes` (optional; requires `estimateSizeBytes`)

Key behaviors:

- Flushes when any trigger is reached or when input completes.
- Tracks batch aggregate context (`ItemCount`, estimated bytes, min/max timestamps).
- Supports `CancellationMode.Drain`: when cancelled, it flushes what is buffered.

#### `BulkIndexNode<TDocument>`

Purpose:

- Consumes `BatchEnvelope<TDocument>` and writes to a bulk indexing client abstraction (`IBulkIndexClient<TDocument>`).

Key behaviors:

- Classifies per-item results into:
  - success (forward to `successOutput`)
  - transient failure (forward to `retryOutput` if configured)
  - permanent failure (forward to `errorOutput` if configured)

Notes:

- This node intentionally depends on an abstraction so it can be tested without Elasticsearch.

### 8.6 Branching and fan-in/fan-out

#### `BroadcastNode<TIn>`

Purpose:

- Fan-out to multiple outputs.

Modes:

- `AllMustReceive`: strict backpressure across all outputs.
- `BestEffort`: required outputs strict; optional outputs are attempted and skipped if backpressured.

Completion:

- Propagates completion to all outputs.

#### `MergeNode<TIn>`

Purpose:

- Fairly merges two inputs into a single output.

Notes:

- Merge does **not** guarantee global ordering.
- Use `KeyPartitionNode` downstream if ordering needs to be re-established.

#### `RouteNode<TIn>`

Purpose:

- Routes envelopes to output writers based on a route key.

Key behaviors:

- Uses caller-supplied `getRoute(envelope)` and a `routes` dictionary.
- If the route key is missing:
  - marks the envelope as `Failed` (`ROUTE_NOT_FOUND`)
  - writes to `errorOutput` when configured
  - otherwise faults the node (fatal), because the failure can’t be observed externally

### 8.7 Sinks

#### `CollectingSinkNode<TPayload>`

Purpose:

- In-memory sink used for tests and demos.

Key behaviors:

- Optionally delays per message (`perMessageDelay`) to simulate slow downstream.

#### `DeadLetterSinkNode<TPayload>`

Purpose:

- Persists failed/dropped envelopes as JSONL records.

Record content:

- envelope + error
- node name
- timestamps
- optional raw snapshot (via `snapshotter` callback)
- metadata (app version / commit id / host name)

Concurrency:

- In-process writes are serialized.
- Cross-process safety is enforced by opening the file with exclusive writer access (`FileShare.Read`) and retrying.

---

## 9. Metrics and observability

Each node constructs `NodeMetrics` (`UKHO.Search.Pipelines.Metrics`).

Emitted instruments include:

- `ukho.pipeline.node.in` / `ukho.pipeline.node.out` (counters)
- `ukho.pipeline.node.failed` / `ukho.pipeline.node.dropped` (counters)
- `ukho.pipeline.node.duration_ms` (histogram)
- `ukho.pipeline.node.inflight` (observable gauge)
- `ukho.pipeline.node.queue_depth` (observable gauge)

Queue depth is available when you create channels via `BoundedChannelFactory` (counting reader/writer wrappers).

---

## 10. Practical tips

- Prefer per-lane sequential processing for strict ordering.
- Treat channel capacity as a first-class tuning knob.
- For expected per-message errors: mark the envelope and route to an error output.
- For truly fatal conditions: throw and let the supervisor cancel the pipeline.
- Keep node names unique (supervisor enforces this).
- Use `ILogger` to include structured fields like `NodeName`, `Key`, `MessageId`, `Attempt`.

---

## 11. Where to look next

- `Pipelines/Messaging/Envelope<T>` and `Pipelines/Errors/PipelineError` for the data/error model.
- `Pipelines/Nodes/*` for node implementations.
- `Pipelines/Supervision/PipelineSupervisor` for lifecycle and fail-fast behavior.
- `test/UKHO.Search.Tests/Pipelines/*` for end-to-end examples of backpressure, ordering, retry, batching, and dead-letter behavior.
