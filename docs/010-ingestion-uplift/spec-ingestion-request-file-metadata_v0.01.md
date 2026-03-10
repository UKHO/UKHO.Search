# Specification: Ingestion Request Batch Timestamp + File Metadata

Version: v0.01  
Status: Draft  
Work Package: `docs/010-ingestion-uplift/`

## 1. Summary
Extend `AddItemRequest` and `UpdateItemRequest` so the FileShareEmulator can include a batch timestamp and a mandatory list of file metadata entries in the message written to the queue (initially for add messages; update messages wired up later).

This enables downstream processing to reason about when a batch was created and what source files it represents.

## 2. Goals
- Capture a batch-level timestamp on ingestion requests.
- Capture batch-associated file metadata on ingestion requests.
- Keep the ingestion request JSON envelope stable and explicitly versionable.
- Ensure the new model types are JSON serializable and covered by comprehensive tests.
- Update the FileShareEmulator (`tools/FileShareEmulator/Services/IndexService.cs`) to populate the new fields from the SQL database before enqueuing.

## 3. Non-goals
- Defining the downstream storage/index mapping for file metadata.
- Changing existing ingestion property semantics.
- Introducing a new request type.

Explicitly out of scope:
- Adding file identifiers (e.g. DB `[File].[Id]`) or additional file metadata fields beyond those listed in this spec.

Decision:
- Do not add a file identifier field (e.g. `FileId`) as part of this uplift.

## 4. Background / evidence
- Ingestion requests are currently serialized using `System.Text.Json` and the project’s `IngestionJsonSerializerOptions`.
- `AddItemRequest`/`UpdateItemRequest` already enforce non-empty `SecurityTokens` and validate `Properties`.
- Ingestion request producers (e.g. `tools/FileShareEmulator`) currently populate request properties from `[BatchAttribute]` and security tokens from `BatchSecurityTokenService`.

Scope note:
- This work package focuses on enriching the message produced by FileShareEmulator; downstream consumption/usage of the new fields is out of scope.

## 5. Requirements
### 5.1 Data model: `Timestamp`
- `AddItemRequest` MUST include a `Timestamp` property of type `DateTimeOffset` sourced from `[Batch].[CreatedOn]`.
- `UpdateItemRequest` MUST include a `Timestamp` property of type `DateTimeOffset` sourced from `[Batch].[CreatedOn]`.
- `Timestamp` MUST be serialized/deserialized as an ISO-8601 `DateTimeOffset` value using the DB value as-is (no UTC normalization beyond whatever is present in the persisted value).

### 5.2 Data model: `IngestionFile`
A new model type `IngestionFile` MUST be introduced with the following properties:
- `Filename` (string)
- `Size` (long)
- `Timestamp` (`DateTimeOffset`)
- `MimeType` (string)

Semantics:
- `Timestamp` MUST be serialized/deserialized as an ISO-8601 `DateTimeOffset` value using the DB value as-is (no UTC normalization beyond whatever is present in the persisted value).
- `Size` represents the numeric length of the file (no conversion); it is sourced directly from `[File].[FileByteSize]`.

### 5.3 Data model: `IngestionFileList`
A new model type `IngestionFileList` MUST be introduced:
- Implements `IEnumerable<IngestionFile>`.
- Is JSON serializable via `System.Text.Json`.
- When used as the `Files` property value, it MUST serialize to and deserialize from a plain JSON array.

### 5.4 Request shape: `Files`
- `AddItemRequest` MUST include a `Files` property of type `IngestionFileList`.
- `UpdateItemRequest` MUST include a `Files` property of type `IngestionFileList`.
- `Files` is mandatory (must not be null) but MAY be empty.

Note:
- No other ingestion request payload types are changed by this work package.

### 5.5 File mapping from database
Each `IngestionFile` in `Files` MUST be sourced from the `[File]` table filtered by `BatchId = batchId`.

Assumptions:
- The source columns used for mapping (`FileName`, `FileByteSize`, `CreatedOn`, `MIMEType`) are expected to be non-null/populated for all included rows.
- If unexpected null/invalid values are encountered, producers SHOULD fail the request build rather than emitting a partially-populated `IngestionFile`.

Mapping:
- `IngestionFile.Filename` from `[File].[FileName]`
- `IngestionFile.Size` from `[File].[FileByteSize]`
- `IngestionFile.Timestamp` from `[File].[CreatedOn]`
- `IngestionFile.MimeType` from `[File].[MIMEType]`

Ordering:
- Producers MAY preserve the database natural row order when reading `[File]` rows.
- Consumers MUST NOT rely on a specific ordering of items within `Files`.

### 5.6 JSON contract
- The new types and properties MUST be serializable/deserializable using the existing ingestion JSON serializer options.
- The request model MUST continue to reject invalid payloads with a `JsonException` during deserialization (consistent with existing behaviour).

## 6. Validation rules
- The validation rules in this section apply equally to `AddItemRequest` and `UpdateItemRequest`.
- `Timestamp` is required (reject null/missing during deserialization).
- `Files` is required (reject null/missing during deserialization).
- `Files` may contain zero elements.
- Individual `IngestionFile` entries MUST reject missing required fields during deserialization.
- For each `IngestionFile` entry:
  - `Filename` MUST be non-empty/non-whitespace.
  - `MimeType` MUST be non-empty/non-whitespace.
  - `Size` MUST be >= 0.

## 7. Compatibility and versioning
- This work package is producer-focused (FileShareEmulator). Downstream consumption/usage is handled in a later work package.
- The emitted message MUST include `Timestamp` and `Files` as specified so that later consumer/provider changes can rely on the presence of these fields.
- Any externalized contract documentation (if present) MUST be updated alongside this change.

## 8. Acceptance criteria
- FileShareEmulator can populate `Timestamp` and `Files` for `AddItemRequest` before writing to the queue.
- `UpdateItemRequest` supports the same JSON contract for `Timestamp` and `Files` (even if FileShareEmulator does not emit update messages yet).
- Comprehensive unit tests cover JSON serialization/deserialization for:
  - `IngestionFile`
  - `IngestionFileList`
  - `AddItemRequest` and `UpdateItemRequest` including `Timestamp` and `Files`
- Tests cover both:
  - `Files` present but empty
  - `Files` missing/null is rejected

## 9. Testing strategy
- Extend existing ingestion model JSON tests (e.g. `test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs`) to:
  - Round-trip envelopes containing `AddItemRequest` and `UpdateItemRequest` with populated `Timestamp` and `Files`.
  - Validate mandatory-but-empty list behaviour.
  - Validate missing/invalid file fields are rejected.

## 10. Implementation notes (non-normative)
- The request producer(s) that currently read from `[BatchAttribute]` SHOULD additionally read:
  - `[Batch].[CreatedOn]` for `Timestamp`.
  - `[File]` rows for the batch for `Files`.

- Producers MUST populate these fields from the SQL database in the same manner as the existing request fields/attributes (i.e. sourced from authoritative DB columns, not derived/guessed at runtime).

## 11. Open questions
- TBD (captured via spec research prompt Q&A).
