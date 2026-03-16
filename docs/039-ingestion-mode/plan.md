# Implementation Plan

**Target path:** `docs/039-ingestion-mode/plan.md`

## Project structure / touchpoints
This change adds an `ingestionMode` setting in the Aspire AppHost and wires it to two apps, then updates `FileShareImageBuilder` behavior based on the `ingestionmode` environment variable.

Primary code locations (expected):
- `src/Hosts/AppHost/appsettings.json`: add `ingestionMode`.
- `src/Hosts/AppHost/Program.cs`: read `ingestionMode` and pass as env var `ingestionmode` to:
  - `FileShareImageBuilder`
  - `IngestionServiceHost`
- `tools/FileShareImageBuilder/*`: read env var `ingestionmode` and parse `IngestionMode`.
- `tools/FileShareImageBuilder/*`: adjust pruning logic per mode.

Primary test locations (expected):
- `test/*` and/or `tools/*/*.Tests` depending on existing coverage for `FileShareImageBuilder`.

Constraints from spec:
- Fail fast if `ingestionmode` missing or unparsable.
- Do not change anything else about Aspire setup configuration.
- Strict mode must preserve current pruning behavior.
- Best-effort mode must keep pruning `BatchStatus != 3` (Committed) but must not prune batches solely due to missing downloaded files.

## Feature Slice: Configured ingestion mode is propagated via Aspire and enforced by FileShareImageBuilder

- [x] Work Item 1: Add `ingestionMode` to AppHost configuration and propagate to services - Completed
  - **Purpose**: Provide a single configuration source (`appsettings.json`) that controls ingestion mode and is injected into dependent processes via environment variables.
  - **Acceptance Criteria**:
    - `src/Hosts/AppHost/appsettings.json` contains `ingestionMode` set to `bestEffort`.
    - `src/Hosts/AppHost/Program.cs` reads the value and passes env var `ingestionmode` to both `FileShareImageBuilder` and `IngestionServiceHost`.
    - No other Aspire wiring is changed.
  - **Definition of Done**:
    - AppHost builds.
    - Configuration value is present and passed through in Aspire configuration.
    - Documentation (`docs/039-ingestion-mode/spec.md`) remains the source of truth.
    - Can execute end-to-end via: running AppHost (Aspire) and observing env var in those processes.
  - [x] Task 1.1: Add `ingestionMode` to AppHost `appsettings.json` - Completed
    - [x] Step 1: Located `src/Hosts/AppHost/appsettings.json` parameters block.
    - [x] Step 2: Added `"ingestionMode": "bestEffort"` under `Parameters`.
  - [x] Task 1.2: Wire `ingestionMode` through Aspire in `Program.cs` - Completed
    - [x] Step 1: Located Aspire resource definitions in `src/Hosts/AppHost/AppHost.cs`.
    - [x] Step 2: Added `builder.AddParameter("ingestionMode")` consistent with other parameters.
    - [x] Step 3: Passed env var `ingestionmode` to both `IngestionServiceHost` (Services runmode) and `FileShareImageBuilder` (Export runmode).
    - [x] Step 4: Verified build succeeds; no other Aspire configuration was changed.
  - **Files**:
    - `src/Hosts/AppHost/appsettings.json`: add `ingestionMode`.
    - `src/Hosts/AppHost/Program.cs`: read and pass env var `ingestionmode`.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - Build: `dotnet build`.
    - Run AppHost: `dotnet run --project src/Hosts/AppHost/AppHost.csproj`.
  - **User Instructions**: None.

  - **Summary (Work Item 1)**:
    - `src/Hosts/AppHost/appsettings.json`: added `Parameters.ingestionMode` defaulting to `bestEffort`.
    - `src/Hosts/AppHost/AppHost.cs`: read `ingestionMode` as a parameter and injected it as environment variable `ingestionmode` into both `IngestionServiceHost` and `FileShareImageBuilder` resources.

- [x] Work Item 2: Enforce ingestion mode env var parsing in FileShareImageBuilder (fail fast) - Completed
  - **Purpose**: Ensure runtime behavior is explicitly controlled and safe by requiring `ingestionmode` to be present and valid.
  - **Acceptance Criteria**:
    - If `ingestionmode` env var is missing, FileShareImageBuilder throws and stops.
    - If `ingestionmode` is present but invalid, FileShareImageBuilder throws and stops.
    - If valid, logging records the selected mode.
  - **Definition of Done**:
    - Unit tests exist for parsing and fail-fast behaviors.
    - FileShareImageBuilder reads the env var once and uses a strongly typed `IngestionMode`.
    - Can execute end-to-end via: running FileShareImageBuilder with/without env var.
  - [x] Task 2.1: Locate `IngestionMode` enum and define parsing rules - Completed
    - [x] Step 1: Identified `UKHO.Search.Configuration.IngestionMode` in `src/UKHO.Search/Configuration/IngestionMode.cs` (`Strict`, `BestEffort`).
    - [x] Step 2: Implemented case-insensitive parsing aligned to enum names.
  - [x] Task 2.2: Implement fail-fast env var retrieval - Completed
    - [x] Step 1: Added `IngestionModeParser.Parse(string?)`.
    - [x] Step 2: Missing/blank value throws `InvalidOperationException`.
    - [x] Step 3: Unparsable value throws `InvalidOperationException`.
    - [x] Step 4: FileShareImageBuilder logs the resolved mode at startup.
  - [x] Task 2.3: Add tests - Completed
    - [x] Step 1: Added tests for missing/blank values.
    - [x] Step 2: Added tests for invalid values.
    - [x] Step 3: Added tests for valid values (strict/bestEffort, case-insensitive).
  - **Files**:
    - `tools/FileShareImageBuilder/*`: add env var parsing and logging.
    - `test/*` (appropriate test project): add tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 1 (to ensure env var is being supplied via AppHost), but can be developed in parallel.
  - **Run / Verification Instructions**:
    - `dotnet test`
    - Run FileShareImageBuilder with env vars set/unset.
  - **User Instructions**: None.

  - **Summary (Work Item 2)**:
    - `src/UKHO.Search/Configuration/IngestionModeParser.cs`: added fail-fast parser for `ingestionmode`.
    - `tools/FileShareImageBuilder/Program.cs`: now reads env var `ingestionmode`, validates via `IngestionModeParser`, and logs selected mode.
    - `test/UKHO.Search.Tests/Configuration/IngestionModeParsingTests.cs`: added unit tests covering missing/invalid/valid values.

- [x] Work Item 3: Implement pruning behavior changes for `bestEffort` mode - Completed
  - **Purpose**: Adjust pruning so best-effort ingestion does not remove batches simply because their files were not downloaded, while keeping the existing pruning of non-committed batches.
  - **Acceptance Criteria**:
    - `strict` mode: pruning behavior is unchanged.
    - `bestEffort` mode:
      - still removes batches with `BatchStatus != 3`.
      - does not remove batches solely because a file was not downloaded.
  - **Definition of Done**:
    - Unit/integration tests cover strict vs bestEffort paths.
    - No regression in existing behavior for strict.
    - Can execute end-to-end via: run FileShareImageBuilder and observe DB state.
  - [x] Task 3.1: Identify current prune logic and the condition for “file has not been downloaded” - Completed
    - [x] Step 1: Located pruning in `tools/FileShareImageBuilder/DataCleaner.cs`.
    - [x] Step 2: Confirmed “downloaded” is inferred via presence of `*.zip` files under `<dataImagePath>/bin/content/**` (used to build `downloadedBatchIds`).
    - [x] Step 3: Confirmed committed batches are those with `BatchStatus == 3`.
  - [x] Task 3.2: Preserve non-committed pruning for both modes - Completed
    - [x] Step 1: Kept `DeleteNonCommittedBatchesAsync` execution unchanged and always enabled.
  - [x] Task 3.3: Gate missing-file pruning behind mode - Completed
    - [x] Step 1: Strict mode retains existing committed-but-not-downloaded pruning.
    - [x] Step 2: BestEffort mode skips committed-but-not-downloaded pruning.
    - [x] Step 3: Added mode-specific console output to make the active policy clear.
  - [x] Task 3.4: Add/extend tests - Completed
    - [x] Step 1: Added strict regression test validating committed batches are removed when no downloads exist.
    - [x] Step 2: Added best-effort test validating committed batches are NOT removed when no downloads exist.
    - [x] Step 3: Added test validating non-committed batches are removed.
  - **Files**:
    - `tools/FileShareImageBuilder/*`: pruning logic changes.
    - `test/*`: pruning behavior tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 2 (needs resolved ingestion mode to branch behavior).
  - **Run / Verification Instructions**:
    - `dotnet test`
    - Run FileShareImageBuilder with `ingestionmode=strict` and `ingestionmode=bestEffort`.
  - **User Instructions**: None.

  - **Summary (Work Item 3)**:
    - `tools/FileShareImageBuilder/DataCleaner.cs`: now branches on `ingestionmode` (via `IngestionModeParser`) to skip pruning of committed-but-not-downloaded batches in `BestEffort` mode; strict behavior unchanged.
    - `test/UKHO.Search.Tests/FileShareImageBuilder/DataCleanerIngestionModeTests.cs`: added tests covering strict vs bestEffort and non-committed pruning.

## Summary / key considerations
- Keep the Aspire changes minimal: add one new config value and pass it as `ingestionmode` to exactly two resources.
- FileShareImageBuilder must fail fast for missing/unparsable `ingestionmode` per updated spec.
- Implement the pruning change as a narrow conditional: only disable “missing downloaded file” pruning in best-effort mode; keep the `BatchStatus != 3` pruning unchanged.
- Add tests for parsing and pruning behavior to avoid regressions.
