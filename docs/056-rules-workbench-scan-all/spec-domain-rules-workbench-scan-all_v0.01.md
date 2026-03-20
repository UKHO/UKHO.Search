# Work Package: `056-rules-workbench-scan-all` — RulesWorkbench Checker scan actions

**Target output path:** `docs/056-rules-workbench-scan-all/spec-domain-rules-workbench-scan-all_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for a small but important usability uplift to the `RulesWorkbench` checker page business-unit scan workflow.

The current checker page offers a single business-unit scan action labelled `Start scan`. That action loads a bounded set of batches and stops at the first batch whose checker result is not `OK`.

This work will:

- rename the existing `Start scan` button to `Scan`
- add a second inline button labelled `Scan All`
- add an unbounded business-unit scan mode that removes the current maximum batch limit for the selected business unit while preserving the existing stop-on-first-non-`OK` behavior

The intent is to preserve the current quick triage workflow while adding an explicit unbounded scan mode for broader rule coverage investigation.

### 1.2 Scope

This specification covers:

- the `RulesWorkbench` checker page UI text and button layout
- the distinction between bounded quick scan and unbounded business-unit scan behavior
- supporting application/service changes needed to enumerate all batches for the selected business unit
- user messaging and result behavior for both scan actions
- automated test coverage updates for the checker page and supporting services
- relevant documentation updates for the `RulesWorkbench` checker page

This specification does not introduce wider rule-engine changes, ingestion-pipeline changes, or new rule semantics.

### 1.3 Stakeholders

- rule authors using `RulesWorkbench`
- ingestion developers investigating business-unit rule coverage
- maintainers of `RulesWorkbench`
- test maintainers for `RulesWorkbench`
- repository documentation maintainers

### 1.4 Definitions

- quick scan: the existing business-unit scan behavior that checks a bounded batch set in deterministic order and stops at the first non-`OK` result
- unbounded scan: the new `Scan All` business-unit scan behavior that checks the full selected business unit but still stops at the first non-`OK` result
- checker status: the current `RulesWorkbench` result status values, including `OK`, `Warning`, and `Fail`
- selected business unit: the business unit chosen in the checker page dropdown
- max rows: the existing numeric limit used to bound the quick-scan candidate set

## 2. System context

### 2.1 Current state

The current `Checker` page under `tools/RulesWorkbench/Components/Pages/Checker.razor` contains a business-unit scan section with:

- a business-unit selector
- a `Max rows` numeric input
- one button labelled `Start scan`

The current scan implementation:

- requires a selected business unit
- loads batches through `BatchScanService.GetBatchesForBusinessUnitAsync(int businessUnitId, int maxRows, ...)`
- uses `SELECT TOP (@maxRows)` semantics in SQL
- iterates batches in deterministic order
- stops immediately when a batch returns a non-`OK` checker status
- reports success only when the bounded candidate set completes without a failing batch

This means the checker currently supports fast discovery of the first problematic batch, but it cannot currently search beyond the configured bounded batch set.

### 2.2 Proposed state

The checker page will expose two distinct business-unit scan actions:

1. `Scan`
   - this is the renamed existing quick-scan action
   - it keeps the current bounded and stop-on-first-non-`OK` behavior

2. `Scan All`
   - this is a new action placed inline with the existing scan controls
    - it scans the full selected business unit without a `Max rows` cap
    - it otherwise preserves the current stop-on-first-non-`OK` behavior

The page will make the distinction explicit through button text and completion messaging so users can choose between bounded and unbounded scan modes.

### 2.3 Assumptions

- preserving the current quick-scan behavior is desirable because it remains useful for rapid investigation
- `Scan All` should ignore the `Max rows` limit and operate on the full selected business unit, because the user requirement is to scan `ALL` batches in that unit
- existing checker result presentation can remain the same because `Scan All` still stops on the first non-`OK` batch
- the batch ordering used today should remain deterministic for both scan modes
- the existing `Max rows` input should remain visible, but it should be disabled while `Scan All` is running because it does not apply to that action
- `Scan All` does not require additional progress or cancellation features for this work and may continue using the existing simple busy-state feedback
- the checker remains scoped to the rules path only and does not invoke wider ZIP-dependent enrichment

### 2.4 Constraints

- this work is limited to `RulesWorkbench` checker behavior and associated supporting services/tests/docs
- the existing single-batch checker workflow must remain unchanged
- the page must remain usable in Blazor Server interactive mode
- the current quick-scan workflow must not regress
- no new repository root artifacts should be introduced

## 3. Component / service design (high level)

### 3.1 Components

This work affects the following areas:

1. Checker page UI
   - `tools/RulesWorkbench/Components/Pages/Checker.razor`
   - rename the existing scan button to `Scan`
   - add a second inline button labelled `Scan All`
   - wire each button to the correct scan mode

2. Batch scan service
   - `tools/RulesWorkbench/Services/BatchScanService.cs`
   - preserve the existing bounded-query path for quick scan
   - add a way to retrieve the full batch set for a business unit for unbounded scan

3. Checker orchestration
   - `Checker.razor` scan handlers and state management
   - preserve the stop-at-first-non-`OK` loop for quick scan
   - add an unbounded scan path that loads the full batch set but otherwise uses the same stop-at-first-non-`OK` behavior

4. Test coverage
   - `test/RulesWorkbench.Tests/...`
   - add or update tests for scan-mode behavior, service query behavior, and user-facing messaging

5. Documentation
   - `wiki/Tools-RulesWorkbench.md`
   - update the checker page documentation to describe both scan modes

### 3.2 Data flows

#### Quick scan (`Scan`)

1. user selects a business unit
2. user optionally adjusts `Max rows`
3. user clicks `Scan`
4. the page loads up to `Max rows` batches for that business unit in deterministic order
5. the page checks each batch sequentially
6. processing stops at the first batch whose checker status is not `OK`
7. the page shows the detailed report for that batch and a message explaining where the scan stopped
8. if all bounded batches are `OK`, the page shows a success message

#### Unbounded scan (`Scan All`)

1. user selects a business unit
2. user clicks `Scan All`
3. the page loads all batches for that business unit in deterministic order
4. the page checks each batch sequentially
5. processing stops at the first batch whose checker status is not `OK`
6. if no non-`OK` batch is found, the page completes after the full business-unit batch set has been evaluated
7. the page shows the same style of detailed result and stop/completion messaging as the existing scan flow, but for the unbounded business-unit batch set

### 3.3 Key decisions

- `Scan` is a rename of the existing `Start scan` action, not a new behavior
- `Scan All` is a distinct explicit action, not an option hidden behind the existing `Max rows` input
- quick scan remains bounded by `Max rows`; unbounded scan does not
- both scan modes use the same checker evaluation path and the same stop-at-first-non-`OK` behavior
- both scan modes must preserve deterministic batch ordering
- completion/info messaging must clearly distinguish whether the user ran a bounded scan or an unbounded scan

## 4. Functional requirements

### FR-001 Rename existing scan button

The checker page shall change the existing business-unit scan button text from `Start scan` to `Scan`.

### FR-002 Add `Scan All` button

The checker page shall add a second business-unit scan button labelled `Scan All`.

The new button shall be displayed inline with the existing business-unit scan controls.

### FR-003 Preserve quick-scan behavior behind `Scan`

Clicking `Scan` shall preserve the current business-unit scan behavior:

- require a selected business unit
- use the configured `Max rows` value
- evaluate batches in deterministic order
- stop at the first batch whose checker status is not `OK`

### FR-004 Add unbounded-scan behavior behind `Scan All`

Clicking `Scan All` shall evaluate every batch in the selected business unit.

The scan shall otherwise preserve the existing business-unit scan behavior and shall stop at the first batch whose checker status is not `OK`.

### FR-005 Business-unit selection validation

Both `Scan` and `Scan All` shall require a valid selected business unit.

If no valid business unit is selected, the page shall show an error message and shall not start a scan.

### FR-006 `Max rows` applicability

`Max rows` shall apply to the `Scan` action only.

`Scan All` shall not be limited by the `Max rows` value.

The `Max rows` input shall remain visible on the page.

While `Scan All` is running, the `Max rows` input shall be disabled.

### FR-007 Deterministic ordering

Both scan modes shall evaluate batches in deterministic ascending order consistent with the current service behavior.

### FR-008 Completion messaging for `Scan`

When `Scan` stops at the first non-`OK` batch, the page shall show an informational message indicating the position reached within the bounded scan set.

When `Scan` completes without a non-`OK` batch, the page shall show an informational message stating that no failing batches were found within the bounded scan set.

### FR-009 Completion messaging for `Scan All`

When `Scan All` stops at the first non-`OK` batch, the page shall show an informational message indicating the position reached within the full selected business-unit batch set.

When `Scan All` completes without a non-`OK` batch, the page shall show an informational message that the full selected business unit was scanned and that no failing batches were found.

That message shall distinguish unbounded scan completion from bounded quick-scan completion.

### FR-010 Detailed result behavior for `Scan All`

After `Scan All` stops at the first non-`OK` batch, the page shall show that batch's detailed checker result using the existing result presentation pattern.

If `Scan All` completes without a non-`OK` batch, the page is not required to introduce a new aggregate results presentation.

### FR-011 Empty business-unit handling

If the selected business unit has no batches, both scan modes shall show a clear informational message and shall not produce an error.

### FR-012 Busy-state protection

While either scan mode is running, the checker page shall prevent starting another scan action.

The page shall continue to prevent conflicting actions using the existing running-state pattern.

This explicitly means:

- while `Scan All` is running, the `Scan` button shall be disabled
- while `Scan` is running, the `Scan All` button shall be disabled

### FR-013 Service support for unbounded scan

The supporting batch scan service layer shall provide a way to retrieve all batches for a selected business unit without applying the current `TOP (@maxRows)` limit.

### FR-014 Backward compatibility for existing quick scan

Existing behavior for single-batch checking and bounded business-unit quick scan shall remain available after this work.

### FR-015 Documentation update

Repository documentation for `RulesWorkbench` shall describe the difference between `Scan` and `Scan All`.

## 5. Non-functional requirements

### NFR-001 Responsiveness and safety

The scan actions shall remain asynchronous and shall not block the Blazor Server UI thread.

### NFR-002 Predictable resource usage

`Scan All` shall use an implementation approach appropriate for larger business-unit batch sets and shall avoid unnecessary duplicate payload loading or duplicate query work.

### NFR-003 Diagnostic continuity

Existing checker logging should remain in place and be extended as needed so full-scan execution can be diagnosed during local investigation.

### NFR-004 Minimal UI disruption

The UI change shall remain a small extension of the existing checker page layout rather than a broad redesign.

No progress indicator or cancellation control is required for `Scan All` as part of this work.

## 6. Data model

No persistent schema change is required.

Transient UI state may need to expand to distinguish:

- quick scan versus unbounded scan
- unbounded-scan progress and completion summary
- the detailed report shown when an unbounded scan stops on the first non-`OK` batch

If aggregate full-scan summary counts are introduced, they shall remain view-model/UI state only unless a reusable DTO is clearly warranted.

## 7. Interfaces & integration

### 7.1 Checker page

The checker page shall expose two explicit event paths:

- quick scan handler for `Scan`
- full scan handler for `Scan All`

### 7.2 Batch scan service

The service contract shall support both:

- bounded retrieval for quick scan
- full retrieval for unbounded scan

This may be implemented either by:

- a second explicit full-scan method, or
- a single method with a clearly expressed mode/limit contract

The chosen shape shall make the difference between bounded and full retrieval unambiguous.

## 8. Observability (logging/metrics/tracing)

The implementation should log enough information to distinguish:

- quick scan versus unbounded scan
- selected business unit id and name
- bounded scan row limit where applicable
- number of batches loaded for scan
- number of batches evaluated
- whether non-`OK` batches were encountered

No new metrics or tracing infrastructure is required for this work.

## 9. Security & compliance

This work does not introduce a new external integration or change the current local data access/security posture of `RulesWorkbench`.

Existing handling of local SQL-backed batch data shall remain unchanged.

## 10. Testing strategy

The following automated coverage shall be added or updated:

1. checker page behavior tests
   - `Scan` renders with the renamed label
   - `Scan All` renders alongside it
   - both actions validate business-unit selection
   - running-state disablement remains correct, including disabling `Scan` while `Scan All` is running
   - `Max rows` remains visible and is disabled while `Scan All` is running
   - existing busy-state feedback remains the only required in-progress indication

2. scan orchestration tests
   - `Scan` stops at the first non-`OK` batch
    - `Scan All` stops at the first non-`OK` batch while using the full business-unit batch set rather than the bounded batch set
    - completion messages differ correctly between quick scan and unbounded scan

3. batch scan service tests
   - bounded retrieval still honors `Max rows`
   - full retrieval does not apply the bounded limit
   - ordering remains deterministic

4. regression tests
   - existing single-batch checker behavior remains unchanged

## 11. Rollout / migration

This is an in-place tooling enhancement for local developer workflows.

No migration, feature flag, or data backfill is required.

## 12. Open questions

None at this stage.
