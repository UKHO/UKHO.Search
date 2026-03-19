# Work Package 048 — FileShareEmulator Business Unit Indexing

**Target output path:** `docs/048-emulator-index-bu/spec.md`

## 1. Overview

### 1.1 Purpose
Extend `tools/FileShareEmulator` so the `Indexing.razor` page can submit indexing requests for all unindexed batches belonging to a selected business unit.

### 1.2 Scope
This work adds a new business-unit indexing path to the existing emulator indexing UI and service flow.

In scope:
- add a new `Index batch by business unit` section beneath `Index batch by id`
- load business units into a dropdown using the same source/pattern already used in `RulesWorkbench`
- submit all unindexed batches for the selected business unit to the ingestion queue
- update `IndexStatus` consistently with the existing indexing flows
- report the outcome back to the user in the UI

Out of scope:
- changing existing `Index next pending batches`, `Index all pending batches`, or `Index batch by id` behavior beyond shared refactoring required to support the new flow
- changing queue message schema
- changing ingestion processing after a queue message has been submitted

### 1.3 Stakeholders
- developers working on `FileShareEmulator`
- developers using `RulesWorkbench` and `FileShareEmulator` together for diagnostics
- testers/operators using the emulator UI to submit targeted indexing runs

### 1.4 Definitions
- **Business unit / BU**: a row from the `BusinessUnit` table used to categorize batches
- **Unindexed batch**: a batch with `IndexStatus = 0`
- **Indexing request**: the queue message submitted to the ingestion queue for a batch

## 2. System context

### 2.1 Current state
`FileShareEmulator` already supports three indexing modes on `Indexing.razor`:
- index the next `n` pending batches
- index all pending batches
- index a specific batch by id

`RulesWorkbench` already contains code on the checker page to load all business units from the `BusinessUnit` table and bind them to a dropdown selector.

### 2.2 Proposed state
`FileShareEmulator` will add a fourth indexing mode driven by business unit selection. The page will load business units for a dropdown, allow the user to select one business unit, and enqueue all unindexed batches for that selected business unit.

### 2.3 Assumptions
- `IndexStatus = 0` remains the definition of "not yet indexed" / pending for this work
- `FileShareEmulator` can access the same database used by the existing indexing flows
- the existing queue submission path in `IndexService` remains the authoritative way to create and send indexing requests
- the `BusinessUnit` table is expected to contain rows; an empty business-unit list is not a scenario this work needs to handle

### 2.4 Constraints
- all emulator code must remain within the existing emulator project set
- for this work, the existing RulesWorkbench business-unit lookup logic should be copied into `FileShareEmulator` rather than moved into a shared project
- UI behavior should remain consistent with existing Blazor Server page interaction patterns already used by the emulator

## 3. Component / service design (high level)

### 3.1 Components
Expected components affected:
- `tools/FileShareEmulator/Components/Pages/Indexing.razor`
- `tools/FileShareEmulator/Services/IndexService.cs`
- business-unit lookup support added within `tools/FileShareEmulator` by copying the established RulesWorkbench query/behavior

### 3.2 Data flows
1. `Indexing.razor` loads business units for the selector.
2. User selects a business unit.
3. User clicks `Index Business Unit`.
4. Emulator queries for batches belonging to that business unit where `IndexStatus = 0`.
5. For each matching batch, emulator builds the same ingestion queue message used by existing indexing methods.
6. Emulator sends each message to the ingestion queue.
7. Emulator updates `IndexStatus` in the same way as other successful indexing methods.
8. Emulator returns a success or failure message to the UI.

### 3.3 Key decisions
- The business unit selector should mirror `RulesWorkbench` behavior by listing all business units from the `BusinessUnit` table.
- Selector options should display both business unit name and business unit id in the format `Name (Id)`.
- The UI success message for business-unit indexing should include the count of batches sent for indexing.
- When a selected business unit has zero unindexed batches, the UI should show a message stating that no unindexed batches were found for that business unit.
- For this work, the business-unit lookup implementation should be copied into `FileShareEmulator` rather than extracted into `FileShareEmulator.Common`.
- There is no additional explicit ordering requirement for batches indexed within a selected business unit.
- The business-unit selector should include a default placeholder such as `Select a business unit`, and the action button should remain disabled until a selection is made.

## 4. Functional requirements

### 4.1 UI additions
- `Indexing.razor` shall add a new section directly below `Index batch by id` titled `Index batch by business unit`.
- The section shall include:
  - a business unit dropdown
  - a button labeled `Index Business Unit`
- The business unit dropdown shall list all business units from the `BusinessUnit` table.
- Each option shall display both the business unit name and business unit id as `Name (Id)`.
- The business unit dropdown shall include a default placeholder such as `Select a business unit`.
- The `Index Business Unit` action shall be disabled until a business unit is selected.
- After a business-unit indexing attempt, the selected business unit shall remain selected so the user can retry without reselecting it.
- While the business-unit indexing action is running, the button text shall change to `Indexing Business Unit...`.

### 4.2 Business unit loading
- `FileShareEmulator` shall load business units using the same underlying data source and query pattern already established in `RulesWorkbench`.
- For this work, the lookup logic shall be implemented directly within `FileShareEmulator`, following the same query and behavior as `RulesWorkbench`.
- If loading business units fails, the page shall show an inline error message within the business-unit indexing section.
- If loading business units fails, the `Index Business Unit` action shall remain disabled.

### 4.3 Business unit indexing action
- When `Index Business Unit` is clicked, the system shall identify all batches for the selected business unit where `IndexStatus = 0`.
- The system shall submit each matching batch to the ingestion queue using the same request-building and queue-submission behavior as the existing indexing actions.
- On successful submission of a batch, the system shall update `IndexStatus` in the same way as the existing indexing methods.
- The business-unit indexing flow does not require any additional explicit batch ordering beyond the implementation's normal query behavior.
- In a partial-failure case, batches already submitted before the failure shall remain marked/indexed; the system shall not attempt to roll back previously applied `IndexStatus` updates.
- The business-unit indexing action does not need to support cancellation once it has started.

### 4.4 User feedback
- When the business-unit indexing action completes successfully, the UI shall show a success message.
- The success message shall include the count of batches sent for indexing, along with the selected business unit name and id.
- When the selected business unit has zero unindexed batches, the UI shall show a non-error message stating that no unindexed batches were found for that business unit, including the selected business unit name and id.
- If the action fails part-way through after some batches have already been submitted, the UI shall report partial success and include how many batches were submitted before the failure, including the selected business unit name and id.
- Failure feedback should follow the same style as the existing indexing page patterns.
- The new business-unit indexing section shall use the same visual message style as the existing `Index batch by id` section for success and failure feedback.
- After business-unit indexing completes, the overall indexing statistics at the top of the page shall be refreshed immediately, in addition to the existing polling behavior.
- For business units with a large number of unindexed batches, the UI shall show interim progress while submissions are being processed, including the selected business unit name and id, plus submitted count and total batches to process.
- Interim progress updates do not need to refresh after every individual batch submission; periodic progress updating is acceptable.

## 5. Non-functional requirements
- The feature should reuse existing services and query patterns where practical.
- The UI should remain responsive and should prevent duplicate submissions while an indexing action is in progress.
- Logging should be consistent with the existing emulator logging style.

## 6. Data model
- Uses existing `BusinessUnit` data.
- Uses existing `Batch` rows and existing `IndexStatus` values.
- No schema changes are currently expected.

## 7. Interfaces & integration
- SQL access to `BusinessUnit` and `Batch`
- existing Azure Queue integration used by `IndexService`

## 8. Observability (logging/metrics/tracing)
- Log the selected business unit and the number of batches submitted where appropriate.
- For partial-failure cases, log both the submitted count before failure and the associated error.
- Reuse existing logging style for indexing operations.

## 9. Security & compliance
- No new external interfaces are expected.
- Existing emulator access patterns remain unchanged.

## 10. Testing strategy
- Add or update tests for the business-unit lookup logic added to `FileShareEmulator`.
- Add or update tests for indexing by business unit, including the case where matching unindexed batches are found.
- Add or update UI-focused verification for the new selector and submit action.

## 11. Rollout / migration
- No data migration is currently expected.
- No shared-project extraction is required for this work item.

## 12. Open questions
1. Resolved: the success message for business-unit indexing must include the count of batches sent for indexing.
2. Resolved: when zero unindexed batches exist for the selected business unit, show a non-error message stating that no unindexed batches were found for that business unit.
3. Resolved: if the action fails part-way through, the UI shall report partial success and include how many batches were submitted before the failure.
4. Resolved: copy the existing RulesWorkbench business-unit lookup logic into `FileShareEmulator` for this work item.
5. Resolved: no additional explicit batch ordering is required for indexing all unindexed batches for a selected business unit.
6. Resolved: include a default placeholder such as `Select a business unit`, and disable the action button until a selection is made.
7. Resolved: if loading business units fails, show an inline error message in the new section and keep the `Index Business Unit` button disabled.
8. Resolved: after a business-unit indexing attempt, keep the selected business unit in the dropdown so the user can retry without reselecting it.
9. Resolved: the new business-unit indexing section should use the same visual message style as the existing `Index batch by id` section for success and failure feedback.
10. Resolved: in a partial-failure case, batches already submitted before the failure remain marked/indexed and no rollback is attempted.
11. Resolved: the business-unit indexing success message should include the selected business unit name and id as well as the submitted count.
12. Resolved: the zero-results and partial-failure messages should also include the selected business unit name and id for consistency.
13. Resolved: while the operation is running, the button text should change to `Indexing Business Unit...`.
14. Resolved: after business-unit indexing completes, the overall indexing statistics at the top of the page should be refreshed immediately, in addition to the existing polling behavior.
15. Resolved: if a business unit has a very large number of unindexed batches, the UI should show interim progress while submissions are being processed.
16. Resolved: interim progress should include both submitted count and total batches to process.
17. Resolved: periodic progress updating is acceptable; updates do not need to occur after every batch submission.
18. Resolved: the business-unit indexing action does not need to support cancellation once it has started.
19. Resolved: the `BusinessUnit` table returning no rows is not a scenario this work needs to handle.
20. Resolved: interim progress messages should include the selected business unit name and id, as well as the submitted and total counts, for consistency with the other business-unit indexing messages.
