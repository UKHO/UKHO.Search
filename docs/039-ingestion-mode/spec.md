# Work Package 039 – Ingestion mode configuration + FileShareImageBuilder pruning behavior

**Target path:** `docs/039-ingestion-mode/spec.md`

## 1. Overview

### 1.1 Purpose
Introduce a configurable *ingestion mode* that can be set in the AppHost configuration and propagated through Aspire to downstream components. The mode affects pruning behavior in `FileShareImageBuilder` when batch downloads fail or when files are missing.

### 1.2 Background / current issue
`FileShareImageBuilder` currently prunes the database such that batches may be removed when a download fails or when output files are missing.

A new configuration value will control whether pruning is:
- **strict**: preserve current behavior (no changes)
- **best effort**: keep pruning non-committed batches, but do **not** prune batches solely because a file has not been downloaded

### 1.3 Scope
In scope:
1. Add `ingestionMode` setting in `AppHost` `appsettings.json`, defaulting to `bestEffort`.
2. Update `AppHost` `Program.cs` to read this value (consistent with other existing config/env patterns) and pass it to both `FileShareImageBuilder` and `IngestionServiceHost` in the Aspire configuration.
3. Update `FileShareImageBuilder` to read the value of `IngestionMode` from environment variable `ingestionmode`.
4. Modify `FileShareImageBuilder` pruning logic:
   - If `ingestionmode` is `strict`: **no behavioral change**.
   - If `ingestionmode` is `bestEffort`:
     - continue pruning any batches whose `BatchStatus` is not `3` (Committed) (unchanged)
     - **do not** prune any batches solely because a file has *not* been downloaded

Out of scope:
- Changes to any other Aspire setup configuration beyond adding this single value pass-through.
- Changes to the definition of `IngestionMode` enum (assumed to already exist).

### 1.4 Definitions
- `IngestionMode` enum: located in the `UKHO.Search.Configuration` namespace.
- `BatchStatus == 3`: represents the “Committed” status.

## 2. Functional requirements

### FR1 – AppHost configuration
Add an `appsettings.json` entry:
- Key: `ingestionMode`
- Default value: `bestEffort`

### FR2 – Aspire wiring in AppHost
`AppHost` `Program.cs` must:
- read `ingestionMode` in the same manner as other environment-configured values in this host
- pass it (as environment variable) to:
  - `FileShareImageBuilder`
  - `IngestionServiceHost`

Constraints:
- Do not change any other Aspire setup behavior.
- Preserve existing naming/structure patterns.

### FR3 – FileShareImageBuilder reads `ingestionmode`
`FileShareImageBuilder` must:
- read environment variable `ingestionmode`
- parse it into the `IngestionMode` enum (case-insensitive)
- default behavior if missing/invalid: treat as `strict` (to avoid accidental behavior change)

### FR4 – Strict mode behavior
If `ingestionmode == strict`, `FileShareImageBuilder` must behave exactly as it does today.

### FR5 – Best-effort pruning behavior
If `ingestionmode == bestEffort`, `FileShareImageBuilder` must:
- still prune/remove batches where `BatchStatus != 3` (Committed), exactly as it does today
- **not** prune/remove batches for which a file has not been downloaded

This implies that the pruning condition “file not downloaded” (or equivalent) is disabled in best-effort mode, while other pruning logic remains unchanged.

## 3. Non-functional requirements

### NFR1 – Backward compatibility / safe default
If `ingestionmode` is **absent**, the system must **throw an exception and stop** (fail fast).

If `ingestionmode` is present but **cannot be parsed**, the system must **throw an exception and stop** (fail fast).

### NFR2 – Logging
`FileShareImageBuilder` should log:
- the resolved ingestion mode at startup (or before pruning)
- any parsing fallback to default (`strict`) if the env var is present but invalid

### NFR3 – Tests
Add tests to verify:
- parsing of the env var into `IngestionMode`
- strict mode preserves existing pruning behavior
- best-effort mode disables pruning for “file not downloaded” while still pruning non-committed batches

## 4. Acceptance criteria
1. `AppHost` includes `ingestionMode` in `appsettings.json` and it defaults to `bestEffort`.
2. `AppHost` passes `ingestionmode` through Aspire to both `FileShareImageBuilder` and `IngestionServiceHost`.
3. `FileShareImageBuilder` reads `ingestionmode` and resolves `IngestionMode` reliably.
4. In strict mode, functionality is unchanged.
5. In best-effort mode, database pruning:
   - still removes `BatchStatus != 3` batches
   - does not remove batches solely due to missing downloaded files

## 5. Implementation notes (non-binding)
- Environment variable name is specified as `ingestionmode` (lowercase).
- AppHost configuration key is specified as `ingestionMode` (camelCase).
- Prefer case-insensitive enum parsing, accepting common variants such as:
  - `bestEffort`, `besteffort`
  - `strict`
