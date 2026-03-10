# FileShareEmulator Security Uplift (005-emulator-security)

## Summary
The `FileShareEmulator` currently publishes `AddItemRequest` messages with a hard-coded `SecurityTokens` value (`["fileshare-emulator"]`). This uplift changes token generation to reflect real batch security memberships (users + groups) and standard batch creation tokens, derived from the emulator database.

All generated security tokens MUST be normalised to lower case.

This document specifies how to calculate the `SecurityTokens` list for each `AddItemRequest` created for a given `BatchId`.

## Goals
- Generate `AddItemRequest.SecurityTokens` from authoritative batch security data in SQL.
- Align emulator output with production ingestion expectations (security trimming by tokens).
- Ensure token generation is deterministic, deduplicated, and resilient to missing data.

## Non-Goals
- Introducing a new authentication/authorization system in the emulator.
- Changing database schema.
- Changing the ingestion message contract (e.g., renaming `SecurityTokens`).

## Current Behaviour
- When indexing a batch, the emulator creates an `IngestionRequest` with an `AddItemRequest`.
- `AddItemRequest.SecurityTokens` is currently a single, static token.

## Required Behaviour
For each batch being indexed (identified by `BatchId`), calculate `AddItemRequest.SecurityTokens` as follows.

### Inputs (SQL)
Given `@batchId`:

1. **Batch groups**
   - Table: `BatchReadGroup`
   - Filter: `BatchId = @batchId`
   - Column: `GroupIdentifier` (`string`)

2. **Batch users**
   - Table: `BatchReadUser`
   - Filter: `BatchId = @batchId`
   - Column: `UserIdentifier` (`string`)

3. **Business unit name**
   - Tables: `Batch`, `BusinessUnit`
   - Find `Batch.BusinessUnitId` for the batch
   - Table: `BusinessUnit`
   - Filter: `BusinessUnit.Id = Batch.BusinessUnitId AND BusinessUnit.IsActive = 1`
   - Column: `Name` (`string`)

### Token Construction Rules
Construct a list of tokens:

1. Add every non-empty `GroupIdentifier` from `BatchReadGroup`.
2. Add every non-empty `UserIdentifier` from `BatchReadUser`.
3. Add standard tokens:
   - `batchcreate`
   - `batchcreate_{bu}` where `{bu}` is the active business unit name from `BusinessUnit.Name`.

### Normalisation, Ordering, and Dedupe
- Tokens MUST be trimmed.
- Tokens MUST be lower case (use invariant casing).
- Empty/whitespace tokens MUST be ignored.
- Tokens MUST be deduplicated (case-sensitive, `StringComparer.Ordinal`).
- The final list SHOULD be stable/deterministic to aid testing and debugging.
  - Suggested ordering:
    1. `batchcreate`
    2. `batchcreate_{bu}` (if available)
    3. Group identifiers (sorted ascending, ordinal)
    4. User identifiers (sorted ascending, ordinal)

### Missing / Invalid Data Handling
- If no active business unit is found for the batch:
  - Still include `batchcreate`.
  - Do NOT include `batchcreate_{bu}`.
  - Emit a warning log including `BatchId`.
- If there are no group/user identifiers, the resulting token list still includes at least `BatchCreate` (and optionally `BatchCreate_{BU}`).

### Lower-casing Rules
All security tokens MUST be lower-cased after trimming and before de-duplication:

- `GroupIdentifier` values MUST be lower-cased.
- `UserIdentifier` values MUST be lower-cased.
- `BusinessUnit.Name` MUST be lower-cased when constructing `batchcreate_{bu}`.

## Implementation Notes (Informative)
- The token calculation is expected to be performed where `AddItemRequest` is created (currently in `IndexService.CreateRequestAsync`).
- Use the existing `SqlConnection` within the emulator process.
- Prefer single-purpose queries and keep timeouts consistent with existing command usage.

## Acceptance Criteria
- For a given `BatchId`, the emulator emits an `AddItemRequest` whose `SecurityTokens` contains:
  - All relevant group and user identifiers linked to that batch.
  - `batchcreate`.
  - `batchcreate_{bu}` when an active business unit exists.
- All tokens are lower case.
- Duplicates and whitespace tokens are not present.
- Token output is deterministic for the same database state.
- Missing/inactive business unit results in a warning and omission of the BU-specific token.

## Test Strategy
- **Unit tests** for token construction rules:
  - Deduplication
  - Ordering
  - Handling of missing BU
  - Handling of empty identifiers
- **Integration test** (preferred for emulator):
  - Seed a test database (or existing emulator DB setup) with:
    - One `Batch`
    - Multiple `BatchReadGroup` and `BatchReadUser` rows
    - Active `BusinessUnit`
  - Run a single batch index operation and assert the serialized message includes the expected `SecurityTokens`.

## Observability
- Add structured logs around token generation:
  - `BatchId`
  - Count of group tokens
  - Count of user tokens
  - Whether BU token was added
  - Warning when BU missing/inactive
