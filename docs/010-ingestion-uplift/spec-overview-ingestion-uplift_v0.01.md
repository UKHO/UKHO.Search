# Specification: Ingestion Uplift (Overview)

Version: v0.01  
Status: Draft  
Work Package: `docs/010-ingestion-uplift/`

## 1. Purpose
Update the FileShareEmulator queue message producer so it enriches emitted ingestion requests with additional batch-level and file-level metadata read from the emulator SQL database.

## 2. Scope
This work package covers:
- Extending the ingestion request payload model (as required) so the emulator can populate the new fields (applies to `AddItemRequest` and `UpdateItemRequest`).
- Updating `tools/FileShareEmulator/Services/IndexService.cs` to read the additional values from the SQL database and include them in the queued request.
- Adding/adjusting JSON serialization tests to ensure the new model types are stable and versionable.

Out of scope (initially):
- Any changes to downstream ingestion providers/consumers (how these values are used after the message is dequeued).
- Any changes to the search index schema/mapping in the target search platform.
- Any changes to security token derivation.
- Bulk backfill or reprocessing strategy for historical batches.

## 3. High-level design
### Components
- `UKHO.Search.Ingestion` (Domain)
  - Defines ingestion request contracts and serialization.
  - Validates ingestion message payloads.

- `FileShareEmulator` (Tools)
  - Builds and enqueues ingestion request payloads from the emulator SQL database.

Downstream ingestion request consumers/providers
  - Out of scope for this work package.

### Component specifications
- `docs/010-ingestion-uplift/spec-ingestion-request-file-metadata_v0.01.md`
