# Specification: FileShareEmulator Business Unit Statistics Page

Version: v0.01  
Status: Draft  
Work Package: `dev/work-packages/mvp/017-emulator-stats/`

## 1. Purpose
Add a dedicated `Statistics` page to `FileShareEmulator` that provides per-business-unit insight into:
- Which batch attribute *names* are present (and how commonly they occur across batches).
- Which file MIME types are present (and how commonly they occur across files).

This is intended to support emulator users in validating seeded data and understanding what metadata is available for rules/enrichment/testing.

## 2. Scope
### In scope
- Add a new Blazor page `Statistics.razor` to `tools/FileShareEmulator`.
- Add the new page to the emulator navigation.
- Extend `tools/FileShareEmulator/Services/StatisticsService.cs` to expose a per-business-unit statistics view model.
- Implement SQL aggregation over the emulator database to compute:
  - Per business unit: distinct batch attribute names (from `BatchAttribute.AttributeKey`) with a count of **batches** using each attribute name.
  - Per business unit: distinct MIME types (from `File.MIMEType`) with a count of **files** using each MIME type.
- Render the results on the `Statistics` page as one section/table per business unit.

### Out of scope
- Any schema migrations or changes to the emulator database.
- Any changes to existing ingestion message creation (e.g., `IndexService`) beyond what is needed to support the new statistics.
- Export/reporting (CSV download), filtering UI, or charting (can be proposed later).

## 3. High-level design
### 3.1 Data model (derived from current emulator SQL usage)
Based on existing queries in `FileShareEmulator`:
- `BusinessUnit` (`Id`, `Name`, `IsActive`)
- `Batch` (`Id`, `BusinessUnitId`, ...)
- `BatchAttribute` (`BatchId`, `AttributeKey`, `AttributeValue`)
- `File` (`BatchId`, `MIMEType`, ...)

`FileAttribute` exists but is not required for the current requirements.

### 3.2 Service API surface
Extend `StatisticsService` with a new async method, for example:
- `Task<IReadOnlyList<BusinessUnitStatistics>> GetBusinessUnitStatisticsAsync(CancellationToken ct = default)`

Proposed new data contracts (names indicative; exact names may vary):
- `BusinessUnitStatistics`
  - `string BusinessUnitName`
  - `IReadOnlyList<NamedCount> BatchAttributeNames`
  - `IReadOnlyList<NamedCount> MimeTypes`
- `NamedCount`
  - `string Name`
  - `int Count`

Counts are expected to be `int` for UI rendering; implementation should use `COUNT_BIG` in SQL if desired, then clamp/checked cast.

### 3.3 SQL aggregation approach
To avoid N+1 queries (one per business unit), the service SHOULD compute aggregates using set-based queries.

#### 3.3.1 Batch attribute name counts (count of batches using each attribute name)
For each business unit:
- Consider all batches in that BU.
- For each batch, consider `BatchAttribute.AttributeKey` values.
- For each distinct attribute key, count how many **distinct batches** in that BU have at least one row with that key.

A suitable pattern is:
- `JOIN` `BusinessUnit` -> `Batch` -> `BatchAttribute`
- `GROUP BY` `BusinessUnit.Name`, `BatchAttribute.AttributeKey`
- `COUNT(DISTINCT Batch.Id)`

Null/whitespace attribute keys SHOULD be excluded.

#### 3.3.2 MIME type counts (count of files using each MIME type)
For each business unit:
- Consider all batches in that BU.
- Consider all files in those batches.
- For each distinct `File.MIMEType`, count how many files in that BU have that MIME type.

A suitable pattern is:
- `JOIN` `BusinessUnit` -> `Batch` -> `File`
- `GROUP BY` `BusinessUnit.Name`, `File.MIMEType`
- `COUNT(*)`

Null/whitespace MIME types SHOULD be excluded.

### 3.4 UI design (`Statistics.razor`)
- Route: `@page "/statistics"`.
- Add a navigation item in `Components/Layout/NavMenu.razor`:
  - Text: `Statistics`
  - Path: `/statistics`
  - Icon: TBD (consistent with existing menu)

Layout proposal:
- Page title: `Statistics`.
- For each business unit (sorted by name):
  - Display a heading with the business unit name.
  - Display **two tables**:
    1. Batch attribute names
       - Columns: `Attribute name`, `Batches`
    2. MIME types
       - Columns: `MIME type`, `Files`

If a business unit has no rows for a given category, render an empty-state message (e.g., `No batch attributes found` / `No files found`).

## 4. Functional requirements
### FR-1: Statistics page exists and is navigable
- The emulator MUST contain a `Statistics` page accessible at `/statistics`.
- The page MUST be reachable via the left navigation.

### FR-2: Per-BU batch attribute name counts
- For each business unit, the system MUST list each distinct `BatchAttribute.AttributeKey` used by any batch in that BU.
- For each listed attribute name, the system MUST display a count of how many **batches** in that BU use that attribute name (at least once).
- Attribute keys MUST be treated as case-sensitive or case-insensitive consistently (TBD); the initial implementation SHOULD use the database collation behaviour.

### FR-3: Per-BU MIME type counts
- For each business unit, the system MUST list each distinct `File.MIMEType` used by any file in any batch in that BU.
- For each listed MIME type, the system MUST display a count of how many **files** in that BU use that MIME type.

### FR-4: Presentation
- The statistics page MUST present the data grouped by business unit, clearly labelled with the BU name.
- Within each BU section, attribute-name counts and MIME-type counts MUST be shown in separate tabular lists.

## 5. Non-functional requirements
- Performance: statistics computation SHOULD complete in under 2 seconds for typical emulator datasets.
- Reliability: null/blank `AttributeKey` and `MIMEType` values MUST NOT break rendering.
- Cancellation: service methods SHOULD accept and honour `CancellationToken`.

## 6. Acceptance criteria
- A new `/statistics` page exists in `FileShareEmulator` and is visible in navigation.
- For a seeded emulator database containing multiple business units, batches, batch attributes, and files:
  - The page renders a BU section for each business unit that has at least one relevant row (or all BUs if chosen; see Open questions).
  - Each BU section shows:
    - A list of distinct batch attribute names with a correct count of batches using that name.
    - A list of distinct MIME types with a correct count of files using that MIME type.

## 7. Testing strategy
- Manual verification:
  - Run `FileShareEmulator` locally.
  - Navigate to `/statistics`.
  - Spot-check counts against direct SQL queries.
- Automated verification (preferred):
  - Add service-level tests that run against a local SQL instance (or test container) seeded with minimal rows for:
    - 2 business units
    - Multiple batches per BU
    - Overlapping and distinct `BatchAttribute.AttributeKey` values
    - Multiple files with different `MIMEType` values
  - Assert:
    - `COUNT(DISTINCT BatchId)` semantics for batch-attribute counts.
    - `COUNT(*)` semantics for MIME-type counts.

## 8. Implementation notes
### Target files
- UI:
  - `tools/FileShareEmulator/Components/Pages/Statistics.razor` (new)
  - `tools/FileShareEmulator/Components/Layout/NavMenu.razor` (update)
- Service:
  - `tools/FileShareEmulator/Services/StatisticsService.cs` (extend)
  - `tools/FileShareEmulator/Services/*` (new model records as needed)

### Open questions / decisions
1. Should the UI include business units with **zero** batches/files (i.e., show empty sections), or only business units that have data?
2. Should attribute keys and MIME types be normalised (e.g., trimmed and lower-cased) for grouping, or should grouping follow SQL collation exactly?
3. Should inactive business units (`BusinessUnit.IsActive = 0`) be included?
