# Implementation Plan

Based on: `dev/work-packages/mvp/017-emulator-stats/spec-emulator-stats_v0.02.md`

## Overall approach
Deliver this feature as small vertical slices in `tools/FileShareEmulator` so each Work Item leaves the emulator runnable and the new `/statistics` page demonstrable.

Constraints (from spec):
- There MUST be **no change** to existing ingestion message creation (e.g., `IndexService.CreateRequestAsync` and emitted ingestion message shape/content).
- All new statistics queries MUST be **read-only**.

---

## Feature Slice: Business Unit Statistics page (read-only)

- [x] Work Item 1: Add navigable `Statistics` page (UI skeleton) - Completed
  - **Purpose**: Provide an end-to-end, runnable entry point (`/statistics`) and navigation wiring, even before real data aggregation is implemented.
  - **Acceptance Criteria**:
    - `/statistics` route is reachable when running via `AppHost`.
    - Left navigation contains a `Statistics` link which routes to `/statistics`.
    - Page renders a placeholder layout for “per business unit” sections.
  - **Definition of Done**:
    - UI page exists and is wired into navigation.
    - Emulator runs without errors.
    - Existing tests/build for the repo still pass.
    - Can execute end-to-end via: run `AppHost` and browse to `/statistics`.
  - [x] Task 1.1: Create page and route - Completed
    - [x] Step 1: Add `tools/FileShareEmulator/Components/Pages/Statistics.razor` with `@page "/statistics"` and a `PageTitle`.
    - [x] Step 2: Add loading/empty-state placeholders consistent with existing pages (e.g., `Home.razor`).
  - [x] Task 1.2: Add navigation item - Completed
    - [x] Step 1: Update `tools/FileShareEmulator/Components/Layout/NavMenu.razor` to include a `RadzenPanelMenuItem` for `/statistics`.
    - [x] Step 2: Choose an icon consistent with the existing menu set.
  - **Files**:
    - `tools/FileShareEmulator/Components/Pages/Statistics.razor`: new page skeleton.
    - `tools/FileShareEmulator/Components/Layout/NavMenu.razor`: add nav item.
  - **Completion summary**:
    - Added `tools/FileShareEmulator/Components/Pages/Statistics.razor` (placeholder UI).
    - Added a `Statistics` nav entry in `tools/FileShareEmulator/Components/Layout/NavMenu.razor`.
    - Build: `Build successful`.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - Set AppHost run mode to `services` (see repo `README.md`).
    - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
    - In the Aspire dashboard, open the `FileShareEmulator` endpoint.
    - Navigate to `/statistics` via the menu.
  - **User Instructions**:
    - Ensure local dependencies are running via Aspire (SQL Server, Azurite, etc.) as per `README.md`.

- [x] Work Item 2: Implement read-only per-BU aggregation in `StatisticsService` and render it - Completed
  - **Purpose**: Provide the real end-to-end feature: compute per-BU batch attribute-name usage and file MIME type counts, and render them.
  - **Acceptance Criteria**:
    - For each business unit represented in the emulator DB (decision: data-present BUs only, unless otherwise specified), the page shows:
      - Distinct batch attribute names with a count of **batches** using each name.
      - Distinct MIME types with a count of **files** using each MIME type.
    - Null/blank `AttributeKey` and `MIMEType` values do not appear and do not break rendering.
    - Implementation uses set-based SQL (no per-BU N+1 queries).
    - There is **no change** to ingestion message creation (`IndexService` remains unchanged).
  - **Definition of Done**:
    - New `StatisticsService` method implemented and used by the UI.
    - Queries are read-only and have reasonable command timeout (consistent with existing code).
    - Errors are handled gracefully in the UI (render an error message rather than crashing the circuit).
    - Repo builds successfully.
    - Can execute end-to-end via: run `AppHost` and browse to `/statistics`.
  - [x] Task 2.1: Add/extend service contracts (models) - Completed
    - [x] Step 1: Add `BusinessUnitStatistics` model (one public type per file).
    - [x] Step 2: Add `NamedCount` model (one public type per file) or equivalent.
    - [x] Step 3: Ensure naming, nullability, and casing match existing repo conventions.
  - [x] Task 2.2: Implement `StatisticsService.GetBusinessUnitStatisticsAsync` - Completed
    - [x] Step 1: Implement SQL query for batch attribute-name counts using `COUNT(DISTINCT Batch.Id)` grouped by BU name + attribute key.
    - [x] Step 2: Implement SQL query for MIME type counts using `COUNT(*)` grouped by BU name + MIME type.
    - [x] Step 3: Exclude null/whitespace keys/types at query time where possible.
    - [x] Step 4: Map results into a per-BU object graph sorted by BU name, then by count/name as appropriate for UI.
    - [x] Step 5: Ensure the method is cancellation-aware and uses read-only queries only.
  - [x] Task 2.3: Render results in `Statistics.razor` - Completed
    - [x] Step 1: Inject `StatisticsService` and load the per-BU statistics on initialization.
    - [x] Step 2: Render a section per BU with two Radzen grids/tables:
      - Batch attribute name counts
      - MIME type counts
    - [x] Step 3: Add empty-state text when a BU has no rows in a category.
  - [x] Task 2.4: Safety check: confirm ingestion message creation is untouched - Completed
    - [x] Step 1: Verify `tools/FileShareEmulator/Services/IndexService.cs` has not been changed as part of this work.
    - [x] Step 2: Ensure no shared helper refactors inadvertently modify ingestion request creation.
  - **Files**:
    - `tools/FileShareEmulator/Services/StatisticsService.cs`: add `GetBusinessUnitStatisticsAsync`.
    - `tools/FileShareEmulator/Services/BusinessUnitStatistics.cs`: new model.
    - `tools/FileShareEmulator/Services/NamedCount.cs`: new model.
    - `tools/FileShareEmulator/Components/Pages/Statistics.razor`: replace skeleton with real rendering.
  - **Completion summary**:
    - Added `StatisticsService.GetBusinessUnitStatisticsAsync` with set-based, read-only SQL aggregation for:
      - Batch attribute-name usage (`COUNT(DISTINCT Batch.Id)`)
      - MIME type usage (`COUNT_BIG(1)`)
    - Added models `BusinessUnitStatistics` and `NamedCount` (one public type per file).
    - Updated `Statistics.razor` to load and render per-BU tables with loading/empty/error states.
    - Confirmed no edits were made to ingestion message creation (`tools/FileShareEmulator/Services/IndexService.cs`).
    - Build: `Build successful`.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
    - Open FileShareEmulator UI and navigate to `/statistics`.
    - Validate a couple of counts by running equivalent SQL against the emulator DB (spot check).

- [x] Work Item 3: UI polish + verification aids - Completed
  - **Purpose**: Make the page easier to use and reduce ambiguity in counts.
  - **Acceptance Criteria**:
    - Tables are consistently sorted and readable (e.g., highest count first).
    - Page clearly labels what each count represents (batches vs files).
    - Loading and error states are user-friendly.
  - **Definition of Done**:
    - UI polish implemented without impacting other pages.
    - Repo builds successfully.
    - Can execute end-to-end via: run `AppHost` and browse to `/statistics`.
  - [x] Task 3.1: Sorting and formatting - Completed
    - [x] Step 1: Decide and implement sort order (e.g., by `Count DESC`, then `Name ASC`).
    - [x] Step 2: Format counts using `N0`.
  - [x] Task 3.2: Error state handling - Completed
    - [x] Step 1: Catch and display exceptions from `StatisticsService` calls (do not crash the Blazor circuit).
    - [x] Step 2: Add minimal logging (via `ILogger`) if consistent with existing patterns.
  - [x] Task 3.3: Optional: add automated verification - Completed
    - [x] Step 1: If an existing Playwright test harness exists for hosts/UI, add a navigation test that asserts `/statistics` loads.
    - [x] Step 2: Otherwise, document manual verification steps in this plan as the primary approach.
  - **Files**:
    - `tools/FileShareEmulator/Components/Pages/Statistics.razor`: sorting, formatting, and error state improvements.
    - `tools/FileShareEmulator/Services/StatisticsService.cs`: optional logging.
  - **Completion summary**:
    - Added consistent ordering of per-BU rows (count desc, then name) when building `BusinessUnitStatistics`.
    - Added UI labels clarifying count semantics (batches vs files) and kept number formatting as `N0`.
    - Improved error handling by logging via `ILogger<Statistics>` and providing a `Retry` button.
    - No Playwright harness was found in the repo; manual verification instructions in this plan remain the primary approach.
    - Build: `Build successful`.
  - **Work Item Dependencies**: Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
    - Navigate to `/statistics` and verify sorting/empty/error states.

---

## Summary of key considerations
- Keep the implementation strictly additive: UI + `StatisticsService` read-only queries only.
- Avoid any refactors that touch `IndexService` or ingestion message creation.
- Use set-based SQL aggregation to avoid per-BU loops and keep performance predictable.
