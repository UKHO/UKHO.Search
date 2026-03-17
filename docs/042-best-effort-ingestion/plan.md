# Implementation Plan

**Target output path:** `./docs/042-best-effort-ingestion/plan.md`

## Project Structure / Placement
- Host configuration and DI wiring lives in `src/Hosts/*` (specifically `IngestionServiceHost` and/or `AppHost` depending on the solution’s host topology).
- FileShare ZIP download + enrichment behavior lives in `src/UKHO.Search.Ingestion.Providers.FileShare/*`.
- Unit tests live alongside the provider project’s existing test project (if present) or the established ingestion test project.

> Note: No UI changes expected; the vertical slices here are applied to the ingestion host entrypoint and the FileShare provider execution path.

## Feature Slice: Mode-aware ingestion host + BestEffort ZIP handling

- [x] Work Item 1: Read `ingestionmode` env var in `IngestionServiceHost` and expose as injectable option - Completed
  - **Purpose**: Make ingestion mode runtime-configurable via environment variable, enabling BestEffort behavior without code changes.
  - **Acceptance Criteria**:
    - `ingestionmode` environment variable is read at host startup.
    - Values `Strict` and `BestEffort` are supported case-insensitively.
    - If missing/invalid, mode defaults to `Strict` and logs a warning (or conforms to existing config validation pattern).
    - Mode value is accessible to provider components via dependency injection.
  - **Definition of Done**:
    - Host reads and validates configuration.
    - Mode flows into provider pipeline execution path.
    - Logging added for resolved mode at startup.
    - Tests added/updated to validate parsing/defaulting (unit test acceptable).
    - Can execute end-to-end via: running host with `ingestionmode=BestEffort` and observing logs during an ingestion run.
  - [x] Task 1.1: Locate the ingestion host entrypoint and configuration system - Completed
    - [x] Step 1: Identified ingestion host project as `src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj` with startup in `Program.cs`.
    - [x] Step 2: Confirmed configuration is read via explicit `Environment.GetEnvironmentVariable("ingestionmode")` (fail-fast parsing), rather than relying solely on `IConfiguration` binding.
  - [x] Task 1.2: Implement `IngestionMode` options binding - Completed
    - [x] Step 1: Added `IngestionModeOptions` wrapper record under `src/UKHO.Search/Configuration/`.
    - [x] Step 2: Bound `ingestionmode` from environment variable via `Environment.GetEnvironmentVariable("ingestionmode")`.
    - [x] Step 3: Parsing/validation uses `IngestionModeParser.Parse(...)` for case-insensitive enum parsing and fail-fast behavior on missing/invalid.
    - [x] Step 4: Missing/invalid currently throws (aligned to existing repo behavior from work package `039-ingestion-mode`).
  - [x] Task 1.3: Register and consume options - Completed
    - [x] Step 1: Registered `IngestionModeOptions` singleton instance in DI.
    - [x] Step 2: Mode is now available to downstream components via DI (`IngestionModeOptions`).
    - [x] Step 3: Added startup log entry indicating resolved ingestion mode.
  - **Files** (expected, confirm in repo):
    - `src/Hosts/*/Program.cs` or `src/Hosts/*/*Host*.cs`: Add env var read/binding + DI registration.
    - `src/Hosts/*/Options/*Ingestion*.cs`: Add/extend options type.
    - `src/UKHO.Search.Ingestion*/*`: Any ingestion pipeline entrypoint that needs mode passed through.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - PowerShell: `$env:ingestionmode="BestEffort"; dotnet run --project <ingestion-host-project>`
    - Confirm startup logs show `BestEffort`.

  - **Summary (Work Item 1)**:
    - `src/Hosts/IngestionServiceHost/Program.cs`: added startup parsing of env var `ingestionmode` via `IngestionModeParser`, registered `IngestionMode` into DI, and logged resolved mode.
    - `test/UKHO.Search.Tests/Hosts/IngestionServiceHostIngestionModeRegistrationTests.cs`: added a small DI sanity test for registering/resolving `IngestionMode`.

- [x] Work Item 2: BestEffort-mode ZIP NotFound becomes a skip (FileShare provider enrichment is resilient) - Completed
  - **Purpose**: Allow ingestion to succeed when some payloads do not have a ZIP available in FileShare, while preserving Strict correctness.
  - **Acceptance Criteria**:
    - In `Strict` mode, ZIP NotFound continues to fail ingestion (no change).
    - In `BestEffort` mode, ZIP NotFound is logged and ZIP-dependent enrichment steps are skipped.
    - In `BestEffort` mode, non-NotFound failures still fail ingestion.
    - Logs include `BatchId` and clearly indicate skip vs failure.
  - **Definition of Done**:
    - Provider code updated with minimal changes.
    - Unit tests cover Strict vs BestEffort behavior.
    - An integration-style test or harness run demonstrates the end-to-end behavior if available.
    - Can execute end-to-end via: processing a payload pointing to a non-existent ZIP with `ingestionmode=BestEffort` without failing the ingestion run.
  - [x] Task 2.1: Identify the ZIP download + enrichment call chain - Completed
    - [x] Step 1: Located `FileShareZipDownloader.DownloadZipFileAsync` and calling code in `BatchContentEnricher`.
    - [x] Step 2: Confirmed failures are surfaced as exceptions (notably `InvalidOperationException` wrapping FileShare errors).
    - [x] Step 3: Confirmed all handler execution is ZIP-dependent (zip download + extraction happens before any `IBatchContentHandler` invocation).
  - [x] Task 2.2: Add mode-aware handling to ZIP downloader or caller - Completed
    - [ ] Step 1: Decide the most localized change point:
      - Option A (preferred): Handle NotFound in the caller (e.g., `BatchContentEnricher`) so the downloader remains a strict abstraction.
      - Option B: Make downloader mode-aware and return an optional result.
    - [ ] Step 2: Implement ZIP download result that can represent “missing” without exception (e.g., `ZipDownloadResult` with `Stream?` + `IsMissing`).
    - [ ] Step 3: In BestEffort, treat `NotFoundHttpError` as missing and do not throw.
    - [ ] Step 4: Ensure downstream enrichment steps are skipped when ZIP is missing.
  - [x] Task 2.3: Logging + observability - Completed
    - [x] Step 1: Added a warning log for "ZIP not found" with `BatchId` and `IngestionMode`.
    - [x] Step 2: Kept existing error logs/throw behavior for non-NotFound and strict mode.
  - [x] Task 2.4: Unit tests - Completed
    - [x] Step 1: Added strict-mode test ensuring missing ZIP still throws.
    - [x] Step 2: Added best-effort test ensuring missing ZIP is skipped without throwing.
    - [x] Step 3: Added best-effort test ensuring non-NotFound failures still throw.

  - **Summary (Work Item 2)**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BatchContentEnricher.cs`: injected `IngestionModeOptions`; in `BestEffort`, treats ZIP NotFound as a skip and returns without running ZIP-dependent handlers.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Injection/InjectionExtensions.cs`: requires `IngestionModeOptions` to be provided by the host (no provider fallback registration).
    - `test/UKHO.Search.Ingestion.Tests/Enrichment/BatchContentEnricherIngestionModeTests.cs`: added tests for Strict vs BestEffort ZIP-missing behavior.
    - `test/UKHO.Search.Ingestion.Tests/Enrichment/BatchContentEnricherTests.cs`: updated existing tests to pass `IngestionModeOptions(Strict)`.
  - **Files** (expected, confirm in repo):
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/FileShareZipDownloader.cs`: Adjust behavior or return type.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BatchContentEnricher.cs`: Add skip logic and mode propagation.
    - `tests/*FileShare*Tests/*.cs`: Add/adjust tests.
  - **Work Item Dependencies**: Depends on Work Item 1 for mode availability in provider.
  - **Run / Verification Instructions**:
    - Run unit tests: `dotnet test`
    - Run host with BestEffort and ingest a message referencing a missing ZIP; verify log message indicates skip and ingestion completes.

- [x] Work Item 3: Deployment/config documentation update for `ingestionmode` - Completed
  - **Purpose**: Ensure operators can enable BestEffort safely and understand behavior.
  - **Acceptance Criteria**:
    - Documentation exists describing supported values and default.
    - Example configuration snippet provided.
  - **Definition of Done**:
    - Docs updated in the work package and/or existing operational docs location.
    - Clearly states Strict unchanged and BestEffort skip semantics.
  - [x] Task 3.1: Update work package docs - Completed
    - [x] Step 1: Added a “How to configure” section (below) describing `ingestionmode`, supported values, and example run instructions.
    - [x] Step 2: Linked to the existing Aspire/AppHost work package that wires the env var (`docs/039-ingestion-mode/*`).
  - **Files**:
    - `docs/042-best-effort-ingestion/plan.md`: Add final operator notes.
  - **Work Item Dependencies**: None (can be done last).
  - **Run / Verification Instructions**:
    - N/A (documentation-only).

## How to configure `ingestionmode`

### Environment variable
- Name: `ingestionmode` (lowercase)
- Supported values (case-insensitive):
  1. `Strict`
  2. `BestEffort`

### Default behavior
- The ingestion host uses `IngestionModeParser.Parse(...)` to resolve the mode.
- If `ingestionmode` is **missing** or **invalid**, the host will **fail fast** at startup (consistent with the existing behavior introduced in `docs/039-ingestion-mode/*`).

### Example run commands
PowerShell:
- Strict:
  - `$env:ingestionmode = "Strict"; dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`
- BestEffort:
  - `$env:ingestionmode = "BestEffort"; dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`

### Aspire/AppHost wiring
- If running under Aspire, `AppHost` is expected to pass the `ingestionmode` environment variable through to `IngestionServiceHost`.
- See: `docs/039-ingestion-mode/spec.md` and `docs/039-ingestion-mode/plan.md`.

  - **Summary (Work Item 3)**:
    - `docs/042-best-effort-ingestion/plan.md`: documented how to configure `ingestionmode`, supported values, default/fail-fast behavior, and Aspire references.

---

# Architecture

**Target output path:** `./docs/042-best-effort-ingestion/architecture.md`

## Overall Technical Approach
- Add a small configuration surface area in the ingestion host to resolve an `IngestionMode` value from environment variable `ingestionmode`.
- Propagate that mode into the FileShare provider’s enrichment pipeline via DI options (preferred) so enrichment steps can be conditionally skipped.
- Keep Strict behavior as the default and unchanged.

```mermaid
flowchart LR
  env[(Environment
"ingestionmode")] --> host[IngestionServiceHost
(Startup/DI)]
  host --> options[IngestionOptions
(IngestionMode)]
  options --> provider[FileShare Provider
Enrichment Pipeline]
  provider --> zip[Zip Downloader]
  zip -->|ZIP exists| enrich[ZIP-dependent enrichment]
  zip -->|NotFound + BestEffort| skip[Skip enrichment
Log + continue]
  zip -->|Other errors| fail[Fail ingestion]
```

## Frontend
- No frontend/UI changes.

## Backend
- **Hosts**: Resolve mode and register options into DI.
- **Provider** (`UKHO.Search.Ingestion.Providers.FileShare`):
  - Detect NotFound on ZIP download.
  - In BestEffort, convert NotFound into a “missing ZIP” result and short-circuit ZIP-dependent enrichment.
  - In Strict, keep existing exception-driven failure.
- **Testing**: Provider-level unit tests validate mode behavior; integration test (if present) validates end-to-end ingestion run doesn’t fail in BestEffort for missing ZIP.
