# Next-Gen Arc 05 Work Packages: Ingestion Input Journal And Failure Model

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 05 introduces the ingestion input journal, also described as shadowing. The journal records provider-normalized inputs at the ingestion boundary after successful provider deserialization and before provider pipeline processing mutates the request. It creates the stable `ShadowId` correlation point needed for rule debugging, dead-letter linkage, outcomes, replay eligibility, supersession checks, and future WorkbenchHost ingestion repair workflows.

## Numbering

Arc 05 work packages use WP180-WP188.

Reserved buffer before Arc 06: WP189-WP199.

## Evidence Checked

- Queue polling, provider deserialization, queue metadata, and provider handoff are in [../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs). This is the capture point with raw queue body, provider name, queue name, message id, dequeue count, inserted/visibility metadata, typed `IngestionRequest`, and derived request id.
- Provider runtime contract is [../../src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs](../../src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs).
- Ingestion service registration, provider graph factories, and dead-letter configuration are in [../../src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs).
- Blob dead-letter persistence and payload diagnostics are in [../../src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/BlobDeadLetterSinkNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/BlobDeadLetterSinkNode.cs) and [../../src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/IngestionDeadLetterPayloadDiagnosticsFactory.cs](../../src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/IngestionDeadLetterPayloadDiagnosticsFactory.cs).
- Queue poison movement for max dequeue count is in [../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs).

## WP180: Specify Journal Semantics, Boundary, And Configuration

Scope:
- Define the ingestion input journal as an ingestion-owned capability, including capture boundary, modes, configuration keys, retention, and failure policy.

Requirements carried:
- Capture valid provider-normalized `IngestionRequest` immediately after successful deserialization and before `provider.ProcessIngestionRequestAsync(...)`.
- Deserialization failures may be captured as diagnostics but are not normal rule-debug inputs.
- Shadowing must not be tied to `IngestionMode`.
- Configuration covers enabled/disabled, raw queue body, normalized request, both, failures only, all valid inputs, table/container names, retention, and failure behavior.
- Failure behavior is deployment-configurable: best-effort diagnostic mode or required journal persistence.
- Code remains environment-neutral rather than hardcoding local/dev/live decisions.

Validation anchors:
- Options binding tests and source-node tests for disabled, best-effort, required, and deserialization-failure modes.

## WP181: Implement ShadowInput Identity And Metadata Model

Scope:
- Define immutable `ShadowInput` identity and searchable metadata.

Requirements carried:
- Assign a stable immutable `ShadowId`, distinct from queue message id and document id.
- Store provider, queue, queue message id, document/request id, request type, received timestamp, dequeue count, inserted/next-visible timestamps, payload hash, raw body pointer, normalized request pointer, optional provider context, and `ReplayOfShadowId`.
- Queue message ids change on replay and document ids recur across updates, so neither can be the journal identity.
- Producers do not generate `ShadowId`; ingestion owns it after receive/accept.

Validation anchors:
- Tests for metadata extraction and hash generation for `IndexItem`, `DeleteItem`, and `UpdateAcl`.

## WP182: Implement Table Index Plus Blob Payload Storage

Scope:
- Build Azure Table Storage metadata plus Blob Storage payload bodies behind storage abstractions.

Requirements carried:
- Blob-only is acceptable only as a spike; target design uses table metadata and blob payloads.
- Table queries must support provider, time range, document id, request type, status, context, payload hash, dead-letter id, and replay lineage.
- Blobs hold normalized request JSON and optionally raw queue JSON.
- UI/API contracts must not expose storage table keys, partition keys, or blob names.

Validation anchors:
- Storage abstraction tests and Azurite/integration tests where practical.

## WP183: Capture Journal Entries In The Queue Source Path

Scope:
- Integrate journal capture into `IngestionSourceNode` at the provider-normalized handoff boundary.

Requirements carried:
- Capture after `DeserializeIngestionRequestAsync` succeeds and before provider processing.
- Capture raw and/or normalized JSON according to configuration.
- Required capture failures block processing; best-effort failures log and continue.
- Preserve queue visibility renewal, max dequeue handling, poison movement, and provider processing semantics except where capture policy explicitly blocks.

Validation anchors:
- Source-node tests proving capture order, policy behavior, deserialization diagnostics, and poison flow preservation.

## WP184: Carry ShadowId Through Pipeline Context And Dead Letters

Scope:
- Add `ShadowId` to envelope/context so later diagnostics, outcomes, dead letters, and replay attempts link back to the accepted input.

Requirements carried:
- Request-level and index-operation dead letters include `ShadowId` when available.
- Existing breadcrumbs, node names, queue metadata, error details, and payload diagnostics remain intact.

Validation anchors:
- Pipeline tests showing `ShadowId` on validation, canonical/enrichment, indexing, and dead-letter paths.

## WP185: Define Outcomes And Failure Ownership Taxonomy

Scope:
- Model processing outcomes separately from immutable shadow inputs.

Requirements carried:
- Distinguish provider handoff failure, ingress gate failure, post-ingress ingestion-owned failure, and ambiguous provider-dependent enrichment failure.
- Model `ShadowInput`, `IngestionAttempt` or `IngestionOutcome`, and `DeadLetterRecord` separately.
- Journal covers successes, failures, and replays, not only dead letters.
- Dead letters are failure outcomes, not the primary replay source.

Validation anchors:
- Outcome tests for accepted, validation failed, dead-lettered, indexed, replay requested, replayed, and superseded states.

## WP186: Implement Dead-Letter Linkage And Failure Detail Data

Scope:
- Link dead-letter records, poison diagnostics, payload diagnostics, outcomes, and shadow inputs.

Requirements carried:
- Failure detail data includes failed node, error category/code/message, breadcrumbs, timestamps, provider, document id, request type, queue metadata, dead-letter reference, payload pointers, and replay eligibility inputs.
- Poison queue movement for max dequeue count is classified separately from post-ingress dead-letter outcomes.

Validation anchors:
- Dead-letter blob schema tests, fallback serialization tests, and missing-shadow compatibility tests.

## WP187: Implement Supersession And Replay Eligibility Foundations

Scope:
- Track latest known accepted and latest successful inputs per provider/document/request stream.

Requirements carried:
- Replay is unsafe by default when the input is no longer the latest known provider event for the document.
- Supersession applies to `IndexItem`, `DeleteItem`, and `UpdateAcl`.
- Eligibility considers provider, document id, request type, source timestamp, received timestamp, payload hash, provider version/ETag where available, last successful `ShadowId`, indexed state marker, and `ReplayOfShadowId`.
- Diagnostic replay remains allowed for superseded inputs; guarded repair replay requires freshness checks.

Validation anchors:
- Tests for stale upsert/delete/ACL replay blocks and diagnostic replay allowed on superseded inputs.

## WP188: Document Journal API Handoff And Token Semantics

Scope:
- Document journal semantics and the API shapes Arc 06/Arc 08 will consume.

Requirements carried:
- APIs list shadow inputs, fetch metadata and payloads, show outcomes/dead letters, show supersession/eligibility, and support later diagnostic/guarded replay.
- Current first version captures security tokens as received; moving token derivation into ingestion/provider normalization is a later contract change.
- Journal-backed inputs replace File Share SQL reconstruction for accepted-input tooling.

Validation anchors:
- Documentation review against implemented model and tests.

## Arc Requirement Cross-Check

- Journal capture after provider deserialization and before provider processing: WP180, WP183.
- `ShadowId`, metadata, raw/normalized payload pointers, payload hash, context, and replay lineage: WP181-WP182.
- Configuration, capture modes, retention, environment neutrality, best-effort/required failure behavior: WP180, WP183.
- Table index plus blob payload storage: WP182.
- `ShadowId` through pipeline context and dead letters: WP184, WP186.
- Failure ownership taxonomy and outcomes: WP185.
- Supersession and replay safety across all request types: WP187.
- Diagnostic versus guarded repair replay foundations: WP187-WP188.
- Producers do not generate `ShadowId`; token derivation remains upstream initially: WP181, WP188.

## Handoff To Arc 06

Arc 06 exposes provider tooling, ingestion-rule, journal discovery, failure, diagnostic replay, and guarded repair APIs over these foundations.