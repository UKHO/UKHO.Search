# Implementation Plan

Work Package: `docs/009-metric-integration/`  
Based on: `docs/009-metric-integration/spec-metric-integration_v0.01.md`

## Telemetry wiring: make existing pipeline metrics visible in Aspire (vertical slice)
- [x] Work Item 1: Subscribe OpenTelemetry to the *current* pipeline meter and validate metrics appear in Aspire - Completed
  - **Purpose**: Achieve a runnable, demonstrable baseline where pipeline metrics show up in the Aspire dashboard, proving the end-to-end wiring (pipeline `Meter` → OpenTelemetry → Aspire dashboard).
  - **Acceptance Criteria**:
    - Pipeline node metrics appear in the Aspire dashboard Metrics UI when running under `AppHost`.
    - No application startup errors/warnings introduced by the change.
  - **Definition of Done**:
    - ServiceDefaults subscribes to the pipeline meter via `.AddMeter(...)`.
    - Local verification completed under Aspire dashboard.
    - Tests (if any exist for ServiceDefaults wiring) still pass.
    - Documentation updated (plan only; spec remains the source of truth).
  - [x] Task 1: Update ServiceDefaults to subscribe to the pipeline meter - Completed
    - [x] Step 1: Update `src/Hosts/UKHO.Search.ServiceDefaults/BuilderExtensions.cs` metrics configuration to call `.AddMeter(...)`. - Added `.AddMeter("UKHO.Search.Pipelines.Playground")` to metrics pipeline.
    - [x] Step 2: Use the *current* meter name (`UKHO.Search.Pipelines.Playground`) for this first slice. - Implemented.
    - [x] Step 3: Ensure the change is applied through the existing `AddServiceDefaults()` path. - Implemented via `ConfigureOpenTelemetry()` which is invoked from `AddServiceDefaults()`.
  - [x] Task 2: Verify in Aspire - Completed
    - [x] Step 1: Run `dotnet run --project src/Hosts/AppHost/AppHost.csproj`. - Verified.
    - [x] Step 2: Open the Aspire dashboard. - Verified.
    - [x] Step 3: Navigate to Metrics and confirm pipeline instruments (e.g., `ukho.pipeline.node.in`, `ukho.pipeline.node.duration_ms`) are present. - Confirmed.
  - **Files**:
    - `src/Hosts/UKHO.Search.ServiceDefaults/BuilderExtensions.cs`: add `.AddMeter(...)` to the `.WithMetrics(...)` pipeline.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
    - Aspire Dashboard → Metrics

## Meter identity stabilization (breaking metric identity; still runnable)
- [x] Work Item 2: Rename the pipeline meter to `UKHO.Search.Ingestion.Pipeline` and update ServiceDefaults subscription - Completed
  - **Purpose**: Implement the spec’s stable meter name so long-term configuration and dashboard usage is consistent.
  - **Acceptance Criteria**:
    - Pipeline node metrics continue to appear in Aspire after the meter rename.
    - No remaining references to `UKHO.Search.Pipelines.Playground` are required for metrics visibility.
  - **Definition of Done**:
    - `NodeMetrics.MeterName` (or equivalent) updated to `UKHO.Search.Ingestion.Pipeline`.
    - ServiceDefaults `.AddMeter(...)` updated to the new name.
    - End-to-end run confirmed in Aspire.
    - Unit tests pass.
  - [x] Task 1: Update pipeline meter name - Completed
    - [x] Step 1: Update `src/UKHO.Search/Pipelines/Metrics/NodeMetrics.cs` to set the meter name constant to `UKHO.Search.Ingestion.Pipeline`. - Updated `NodeMetrics.MeterName`.
    - [x] Step 2: Ensure no other metrics instruments are renamed (instrument names remain `ukho.pipeline.*`). - No instrument name changes.
  - [x] Task 2: Update ServiceDefaults subscription - Completed
    - [x] Step 1: Replace the old meter name in `BuilderExtensions.cs` with `UKHO.Search.Ingestion.Pipeline`. - Updated `.AddMeter(...)` subscription.
  - [ ] Task 3: Verify in Aspire
    - [x] Step 1: Run `dotnet run --project src/Hosts/AppHost/AppHost.csproj`. - Verified.
    - [x] Step 2: Confirm the metrics are still present. - Verified.
    - Summary: Build + dashboard verification completed after the meter rename.
  - **Files**:
    - `src/UKHO.Search/Pipelines/Metrics/NodeMetrics.cs`: rename `MeterName`.
    - `src/Hosts/UKHO.Search.ServiceDefaults/BuilderExtensions.cs`: update `.AddMeter(...)` to new name.
  - **Work Item Dependencies**:
    - Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
    - Aspire Dashboard → Metrics

## Provider dimension support (enables slicing by provider)
- [x] Work Item 3: Add `provider` tag to pipeline node metrics (low cardinality) and plumb provider name from ingestion graphs - Completed
  - **Purpose**: Make ingestion metrics usable at scale by allowing slicing by provider (e.g., `file-share`) without increasing cardinality beyond controlled provider names.
  - **Acceptance Criteria**:
    - Pipeline node metrics include `provider=<providerName>` as a metric dimension for provider pipelines.
    - Aspire metrics can be grouped/filtered by `provider` and `node`.
    - Multiple provider pipelines can run in-process without gauge/counter collisions.
  - **Definition of Done**:
    - `NodeMetrics` records `provider` tag for counters/histograms and includes it in observable gauges.
    - Pipeline node types can be constructed with an optional provider name.
    - Ingestion provider graph builders supply provider name from `IIngestionDataProviderFactory.Name`.
    - Unit tests cover tag presence and gauge collision avoidance.
  - [x] Task 1: Extend `NodeMetrics` to support provider tagging - Completed
    - [x] Step 1: Update `NodeMetrics` to accept an optional `providerName` and include it as a tag alongside `node`. - Added provider-aware constructor and tag set (`provider`, `node`) for all instruments.
    - [x] Step 2: Update gauge measurement providers so they can emit measurements uniquely per `(provider, node)`. - Observable gauge providers are now keyed by `(provider, node)`.
    - [x] Step 3: Ensure tag allocation is consistent (avoid allocating new `KeyValuePair[]` per call where possible). - Tags are cached per `NodeMetrics` instance and reused.
  - [x] Task 2: Extend pipeline node construction to pass provider name - Completed
    - [x] Step 1: Update `NodeBase<TIn, TOut>` to accept an optional provider name and pass it into `NodeMetrics`. - Added optional `providerName` plumbing through node base types.
    - [x] Step 2: Update pipeline node types in `src/UKHO.Search/Pipelines/Nodes/` so ingestion graphs can pass provider name when instantiating nodes. - Added optional `providerName` to key node constructors used by ingestion graphs.
  - [x] Task 3: Update ingestion provider graph builders to pass provider name - Completed
    - [x] Step 1: Identify ingestion processing graph factories/constructors (e.g., in `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/*Graph*.cs`). - Updated FileShare ingestion graphs to accept/pass `providerName`.
    - [x] Step 2: Ensure the provider name passed is exactly the provider factory `Name` (e.g., `file-share`). - `FileShareIngestionDataProviderFactory` now passes `Name` into the provider which passes it into graph construction; file-share factory `Name` is `file-share`.
  - [x] Task 4: Verify in Aspire - Completed
    - [x] Step 1: Run under `AppHost`. - Verified.
    - [x] Step 2: In Aspire dashboard Metrics, confirm `provider` appears as a dimension on pipeline metrics. - Verified.
  - **Files**:
    - `src/UKHO.Search/Pipelines/Metrics/NodeMetrics.cs`: add `provider` dimension support.
    - `src/UKHO.Search/Pipelines/Nodes/NodeBase.cs`: plumb provider name into metrics.
    - `src/UKHO.Search/Pipelines/Nodes/*.cs`: update constructors as needed to allow provider name to be passed.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/*`: pass provider name from provider graph construction.
  - **Work Item Dependencies**:
    - Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
    - Aspire Dashboard → Metrics → confirm `provider` + `node` slicing

## Quality gates (tests + regression)
- [x] Work Item 4: Automated verification (unit tests) and documentation finalisation - Completed
  - **Purpose**: Ensure the telemetry changes are stable, regression-protected, and ready for iterative evolution.
  - **Acceptance Criteria**:
    - Unit tests validate:
      - instrument names unchanged
      - provider tags present for provider pipelines
      - gauge observations don’t collide across providers
    - `dotnet build` passes.
  - **Definition of Done**:
    - Tests added/updated.
    - Build passes.
    - Plan checkboxes updated as complete during implementation.
  - [x] Task 1: Add/update unit tests for `NodeMetrics` - Completed
    - [x] Step 1: Add tests in `test/UKHO.Search.Tests` (or most appropriate existing test project) targeting metrics tagging logic. - Added `test/UKHO.Search.Tests/Pipelines/Metrics/NodeMetricsProviderTaggingTests.cs`.
    - [x] Step 2: Include a test case with two `NodeMetrics` instances sharing the same node name but different providers, asserting both gauge measurements are present. - Added inflight + queue_depth gauge collision tests covering two providers.
  - [x] Task 2: Run build + tests - Completed
    - [x] Step 1: Run `dotnet build`. - Succeeded.
    - [x] Step 2: Run `dotnet test`. - Succeeded.
  - **Files**:
    - `test/*`: new/updated tests for `NodeMetrics` behaviour.
  - **Work Item Dependencies**:
    - Work Item 3.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`

## Summary / key considerations
- The plan deliberately delivers an early runnable slice (meter subscription) before applying the breaking meter rename and provider dimension changes.
- The provider dimension is sourced from `IIngestionDataProviderFactory.Name` and must remain low-cardinality.
- Changes must preserve Onion Architecture boundaries: pipeline metrics remain in Domain, wiring remains in `UKHO.Search.ServiceDefaults`, and provider name propagation occurs in provider graph construction.
