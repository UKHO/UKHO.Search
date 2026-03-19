# Work Package 049 — Dead Letter Diagnostic Payload Enhancement

**Target output path:** `docs/049-deadletter-enhancement/spec.md`

**Version:** `v0.01`

## 1. Overview

### 1.1 Purpose
Enhance ingestion dead-letter persistence so dead-letter records include enough diagnostic payload detail to explain indexing and transformation failures without requiring a debugger or reproduction run.

The immediate driver is index dead-letter analysis for `UpsertOperation` failures, where the current dead-letter JSON only shows `documentId` and omits the `CanonicalDocument` that Elasticsearch attempted to index.

### 1.2 Scope
This work defines a generic dead-letter enrichment capability for persisted dead-letter records across ingestion flows.

In scope:
- enrich persisted dead-letter records with runtime payload detail sufficient for diagnosis
- support polymorphic payload capture so derived payload properties are preserved
- apply the enhancement consistently to dead-letter persistence implementations used by ingestion
- allow a generic diagnostic snapshot model rather than special-casing only `CanonicalDocument`
- improve failure diagnostics for Elasticsearch indexing errors by making the indexed document content visible in the dead-letter output

Out of scope:
- changing retry, routing, or dead-lettering decisions in the ingestion pipeline
- fixing the `geoPolygons` parsing issue itself
- preserving the current dead-letter JSON contract for compatibility
- introducing production-grade data redaction policies beyond what is already present in the current dev-oriented ingestion codebase

### 1.3 Stakeholders
- developers diagnosing ingestion failures
- developers working on file-share enrichment and indexing
- developers inspecting blob/file dead-letter records during emulator and local runs

### 1.4 Definitions
- **Dead-letter record**: the persisted diagnostic record written when an ingestion message cannot proceed successfully
- **Runtime payload type**: the concrete CLR type held in the payload at runtime, not only the declared generic/base type
- **Diagnostic snapshot**: a persisted JSON representation of the payload or related context intended for debugging
- **Index dead-letter**: a dead-letter created after an `IndexOperation` fails during bulk indexing or related terminal index processing

## 2. System context

### 2.1 Current state
The ingestion pipeline persists dead-letter records for request and index failures.

For index dead-letters, the persisted envelope payload is typed as `IndexOperation`. When the runtime payload is actually `UpsertOperation`, the persisted JSON currently loses derived members such as `Document`, leaving only base-type data such as `documentId`.

This makes investigation of Elasticsearch failures difficult because the dead-letter record does not show the exact `CanonicalDocument` that was submitted.

### 2.2 Proposed state
Dead-letter persistence will capture diagnostic payload content generically using the runtime payload shape and an explicit diagnostic snapshot field. Dead-letter records will become self-contained diagnostic artifacts that show:
- the persisted envelope metadata
- the runtime payload type
- the runtime payload content or a derived diagnostic snapshot
- the existing pipeline error information
- any fallback serialization error details if diagnostic capture partially fails

For `UpsertOperation`, this will allow the dead-letter JSON to show the full `CanonicalDocument` that failed indexing.

### 2.3 Assumptions
- dead-letter payload size growth is acceptable for current dev-focused workflows
- the team is willing to adopt a revised dead-letter JSON schema and does not require backward compatibility
- fresh index recreation is acceptable for related diagnostic work in this environment
- both file-based and blob-based dead-letter sinks should expose the same diagnostic behavior

### 2.4 Constraints
- the solution must be generic and must not be implemented as an Elasticsearch-only or `CanonicalDocument`-only special case
- dead-letter persistence must remain resilient; diagnostic serialization must not prevent dead-letter creation
- existing ingestion processing architecture and dependency direction must be preserved

## 3. Component / service design (high level)

### 3.1 Components affected
Expected components affected:
- dead-letter record model(s) used for persistence
- dead-letter persistence sinks for file-based and blob-based storage
- ingestion pipeline payload serialization path for dead-letter records
- any shared dead-letter metadata/snapshot abstractions

Likely code areas:
- `src/UKHO.Search/Pipelines/DeadLetter/*`
- `src/UKHO.Search/Pipelines/Nodes/DeadLetterSinkNode.cs`
- `src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/BlobDeadLetterSinkNode.cs`
- related index-operation payload types in `src/UKHO.Search.Ingestion/Pipeline/Operations/*`

### 3.2 Data flows
1. A request or index operation fails and is routed to a dead-letter sink.
2. The sink builds a dead-letter record.
3. The sink captures standard metadata and error information.
4. The sink captures payload diagnostics using the runtime payload type and generic snapshot rules.
5. The sink persists the dead-letter record.
6. If payload diagnostic serialization fails, the sink persists a fallback record that still includes minimal metadata plus the serialization failure details.

### 3.3 Key decisions
- The dead-letter schema may be revised freely for this work item.
- The enhancement must be generic across payload types.
- The persisted dead-letter record must expose the runtime payload type.
- The persisted dead-letter record must include enough payload detail to diagnose `UpsertOperation` failures without separate reproduction.
- The dead-letter sink must prefer successful persistence of a partially detailed record over dropping the dead-letter entirely.
- A dedicated diagnostic snapshot field is preferred over relying only on base-type envelope serialization.

## 4. Functional requirements

### 4.1 Generic dead-letter payload diagnostics
- Persisted dead-letter records shall include the concrete runtime payload type name for the envelope payload.
- Persisted dead-letter records shall include a diagnostic payload snapshot captured from the runtime payload.
- The diagnostic payload snapshot mechanism shall be generic and reusable for different payload types.
- The generic mechanism shall not depend on `IndexOperation` being the only payload family.
- The generic mechanism shall preserve derived payload members when the runtime payload is a subtype of the declared payload type.

### 4.2 Index-operation diagnostics
- When the failed payload is an `UpsertOperation`, the persisted dead-letter record shall include the `CanonicalDocument` content associated with that operation.
- The persisted dead-letter record for an `UpsertOperation` shall include enough detail to inspect fields such as `geoPolygons` without re-running ingestion.
- The persisted dead-letter record shall continue to include the existing pipeline error details returned by bulk indexing.
- The persisted dead-letter record should make it clear which operation type failed, such as upsert, delete, or ACL update.

### 4.3 Consistency across sinks
- File-based and blob-based dead-letter sinks shall persist the same logical diagnostic information.
- The dead-letter schema for request and index dead-letters shall follow the same structural pattern, even if the payload snapshots differ by payload type.
- If the system supports multiple dead-letter persistence implementations, they shall use the same runtime payload diagnostic approach.

### 4.4 Failure tolerance
- If runtime payload snapshot generation fails, dead-letter persistence shall still succeed with a fallback record.
- The fallback record shall include the original pipeline error, the payload type, and the snapshot serialization failure details.
- A failure to serialize the diagnostic snapshot shall not suppress dead-letter persistence of the failed message.

### 4.5 Schema usability
- The revised dead-letter JSON shall be readable and useful when inspected directly in raw JSON form.
- The JSON shall make it obvious where to find:
  - envelope metadata
  - runtime payload type
  - payload diagnostic snapshot
  - pipeline error details
  - snapshot serialization fallback details if present

## 5. Non-functional requirements
- The enhancement should add minimal complexity to the ingestion pipeline outside dead-letter persistence.
- The design should favor debuggability over compact payload size for this dev-oriented work.
- The dead-letter persistence path should remain robust under partial serialization failures.
- The design should be extensible so additional payload-specific snapshots can be added later without redesigning the schema.

## 6. Data model
The dead-letter record model shall be revised to support diagnostic payload persistence.

The revised model should include, at minimum:
- existing dead-letter metadata
- existing envelope metadata
- existing pipeline error details
- `payloadType` or equivalent runtime type field
- `payloadSnapshot` or equivalent diagnostic payload field
- optional `snapshotError` or equivalent fallback diagnostic field

The schema may change existing field layout if needed because backward compatibility is not required for this work item.

## 7. Interfaces & integration
- No external API changes are required.
- Blob and file dead-letter persistence implementations must align on the same record shape.
- Any helper abstraction introduced for payload snapshots should be reusable from multiple dead-letter sinks.

## 8. Observability (logging/metrics/tracing)
- Dead-letter persistence should log when snapshot generation fails and a fallback record is used.
- Logging should continue to identify node name, message id, key, and error code.
- Additional logging may identify the runtime payload type captured in the dead-letter record.

## 9. Security & compliance
- This work is intended for current dev-focused workflows and may increase the amount of payload data written to dead-letter storage.
- The team should assume dead-letter outputs may now contain full indexed document content and should treat the storage location accordingly.
- No special backward-compatibility or contract-preservation measures are required.

## 10. Testing strategy
- Add tests proving dead-letter persistence includes derived payload members when the declared payload type is a base type.
- Add tests proving an `UpsertOperation` dead-letter contains the `CanonicalDocument` diagnostic content.
- Add tests proving file-based and blob-based dead-letter sinks emit the same logical diagnostic structure.
- Add tests proving fallback persistence still succeeds when snapshot serialization fails.
- Add tests proving request dead-letters still persist successfully with the revised schema.

## 11. Rollout / migration
- No backward compatibility is required for the dead-letter JSON schema.
- No migration of existing dead-letter artifacts is required.
- The revised schema may be adopted directly for current dev use.
- Fresh index recreation is acceptable and does not constrain this work item.

## 12. Open questions
1. Resolved: implement the enhancement in a generic way rather than as a `CanonicalDocument`-only special case.
2. Resolved: backward compatibility is not required for the dead-letter JSON schema.
3. Resolved: the immediate diagnostic goal is to make the failed indexed document visible from the dead-letter artifact itself.
4. Resolved: fresh index recreation is acceptable for this work item.
5. Open: should the revised dead-letter schema store both a polymorphic `payload` and a separate `payloadSnapshot`, or only one canonical diagnostic representation?
