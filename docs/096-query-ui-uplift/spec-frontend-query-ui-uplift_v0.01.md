# Specification: Query UI Uplift

**Target output path:** `docs/096-query-ui-uplift/spec-frontend-query-ui-uplift_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

### 1.1 Purpose

This specification defines an uplift of the current developer-centric query UI in `QueryServiceHost` so it better supports inspection, editing, and execution of the query plan that sits between raw user input and the final Elasticsearch request.

The intent is not to turn the screen into a consumer search experience. The intent is to make it a stronger diagnostics and exploration surface for engineers working on query understanding, query planning, and Elasticsearch translation.

### 1.2 Scope

This work package covers:

- reshaping the current one-screen query UI into a more effective desktop-style diagnostics workspace
- splitting the current centre results area so the generated query plan is visible and editable alongside the results
- introducing Monaco-based query plan editing, reusing the established editor approach from `RulesWorkbench`
- adding the ability to execute an edited query plan directly from the UI
- reducing visual clutter by removing nested cards and similar panel-within-panel patterns
- surfacing supporting diagnostics that make query behaviour easier to understand, including transformation trace, extracted signals, final Elasticsearch request, validation/warnings, and execution metrics
- fitting the core workflow into a single screen without depending on recent-search or saved-example features

This work package does not cover:

- recent searches
- saved examples or presets
- a multi-screen workflow
- changing the underlying query semantics defined by the query planning specifications
- broad end-user UX optimisation; this is still an internal developer-focused experience

### 1.3 Stakeholders

- search/query developers
- Elasticsearch integration developers
- workbench and diagnostics tooling contributors
- QA and technical testers investigating search behaviour
- product and technical leads reviewing query interpretation behaviour

### 1.4 Definitions

- **Raw query**: the user-entered search text submitted from the main search box
- **Generated query plan**: the structured plan produced by the query pipeline from the raw query before Elasticsearch execution
- **Edited query plan**: the user-modified working copy of the generated query plan shown in the Monaco editor
- **Transformation trace**: a stage-by-stage representation of how the raw query becomes a query plan and then an Elasticsearch request
- **Diagnostics**: supporting metadata such as extracted signals, warnings, timings, counts, and the final Elasticsearch JSON request
- **Explain view**: result-specific detail that helps explain why an individual document matched or ranked as it did

## 2. System context

### 2.1 Current state

The current `QueryServiceHost` home page is an interactive Blazor screen with a three-column layout:

- left: facets
- centre: results
- right: details

The current shell is useful as an early search host, but it is not yet optimised for query debugging and query-plan inspection.

Current observed characteristics include:

- the main page (`src/Hosts/QueryServiceHost/Components/Pages/Home.razor`) renders a header search bar, a three-column main region, and a footer summary
- the current centre column is a `ResultsPanel`
- the current right column is a `DetailsPanel`
- the results and details areas currently use multiple outlined `RadzenCard` containers and placeholder content, contributing to a nested and visually heavy layout
- the current UI does not expose the generated query plan as a first-class editable artifact
- the current UI does not provide a dedicated workflow for executing an edited query plan directly

### 2.2 Proposed state

The uplifted UI shall remain a single-screen developer tool, but reorganised around the primary workflow:

1. enter a raw query
2. execute the query normally
3. inspect the generated query plan
4. edit the plan
5. execute the edited plan
6. inspect results and supporting diagnostics
7. optionally inspect a selected result in more detail

The proposed single-screen layout is:

- **Top command bar**
  - raw query input
  - primary search action for the raw query
  - compact status summary such as parse state, extracted signal count, Elasticsearch time, and hit count
- **Left diagnostics context column**
  - extracted signals
  - compact transformation trace
- **Centre workspace, split in two**
  - left half: editable Monaco query plan editor with a `Search` button directly above it
  - right half: flat results list
- **Right diagnostics column**
  - final Elasticsearch request JSON
  - validation and warnings
  - execution metadata
- **Bottom collapsible detail drawer**
  - selected-result explanation and deeper detail-on-demand

This design keeps the core editing and result-review loop in the middle of the screen while moving secondary context to the left and right edges and deeper detail to a drawer.

### 2.3 Assumptions

- `QueryServiceHost` remains a Blazor-based host and uses interactive server rendering where required for editor and button interaction
- Monaco can be reused from the established `RulesWorkbench` pattern rather than introducing a second editor approach
- Radzen remains the primary component library and the UI should stay close to the stock Radzen Material theme used by the repository
- the query runtime will be able to expose a JSON-serializable query plan suitable both for display and for direct execution
- the final Elasticsearch request can be surfaced safely in this internal diagnostics UI
- this work package is focused on a desktop-like layout rather than a mobile-first layout

### 2.4 Constraints

- the page must remain usable on a single screen without relying on modal-heavy or page-to-page navigation for the primary workflow
- the centre workspace must be split so that query plan editing and results are visible together
- the query plan editor must be editable and must not be a read-only preview
- clicking the main search action for the raw query must still execute the search in the normal generated-plan flow
- clicking the query-plan `Search` button must execute the edited query plan currently shown in Monaco
- the visual design should reduce clutter and nested panels in favour of flatter sectioning and clearer hierarchy
- recent searches and saved examples are explicitly out of scope for this work package

## 3. Component / service design (high level)

### 3.1 Components

The uplifted screen consists of the following high-level UI areas.

1. **Top command bar**
   - hosts the raw query text box and primary raw-query `Search` button
   - shows a compact run summary

2. **Query insight column**
   - shows extracted signals
   - shows the transformation trace from raw query to query plan to Elasticsearch request

3. **Query plan editor pane**
   - shows the generated query plan after a raw-query search
   - allows direct editing in Monaco
   - contains a pane-level `Search` action that executes the edited plan

4. **Results pane**
   - shows the search results as a flatter list with minimal nested structure
   - supports result selection

5. **Diagnostics column**
   - shows the final Elasticsearch request JSON
   - shows validation and warnings
   - shows execution metadata such as durations and hit counts

6. **Bottom detail drawer**
   - shows explain-oriented details for the selected result
   - remains collapsed by default until a result is selected or detail is explicitly requested

### 3.2 Data flows

Two primary execution flows are required.

#### 3.2.1 Raw query execution flow

- user enters raw query text
- user clicks the main `Search` button in the command bar
- system generates the query plan from the raw query
- system executes the search using that generated plan
- UI updates the query plan editor with the generated plan
- UI updates diagnostics, results, and run summary using that execution

#### 3.2.2 Edited query plan execution flow

- user edits the query plan shown in Monaco
- user clicks the `Search` button above the query plan editor
- system validates the edited query plan
- if valid, system executes the edited query plan directly
- UI updates diagnostics, results, and run summary using that execution
- validation errors and warnings are shown clearly if execution cannot proceed

### 3.3 Key decisions

The following decisions are captured from this conversation.

- The centre results pane will be split into two visible halves.
- The left half of that split will show the editable query plan.
- Monaco will be used for the editor, following the `RulesWorkbench` approach.
- The generated-plan flow must continue to execute when the main raw-query search is used.
- A dedicated `Search` button will sit directly above the query plan and will execute the edited plan.
- The results presentation should move to a flatter, less cluttered visual style.
- The most useful additional diagnostics to surface on the same screen are:
  - transformation trace
  - extracted signals
  - final Elasticsearch request JSON
  - validation and warnings
  - execution metrics
  - selected-result explanation
- Recent searches and saved examples are not part of this work package.
- The initial implementation will include a `Reset to generated plan` action in the query plan editor.
- The right-hand diagnostics column will start as stacked sections rather than tabs.
- The initial selected-result explanation will be a lightweight summary, with deeper clause-level explain detail deferred.
- Compare mode, diff tooling, and clause-to-result contribution views are useful follow-on ideas but are not required in this initial uplift.

## 4. Functional requirements

### 4.1 Overall screen behaviour

- The UI SHALL remain a single-screen experience for the primary query diagnostics workflow.
- The UI SHALL remain developer-centric and SHALL explicitly support understanding the transformation from user input to Elasticsearch request.
- The page SHALL preserve an interactive top-level raw-query search flow.
- The page SHALL provide an edited-query-plan execution flow in the same screen.

### 4.2 Centre workspace split

- The current centre results pane SHALL be split into two panes.
- The left side of the split SHALL display the query plan.
- The query plan display SHALL be editable.
- The right side of the split SHALL display the search results.
- The split layout SHOULD keep both plan editing and result review visible without tab switching.
- On desktop widths, the split SHOULD default to an approximately balanced layout, with implementation free to tune the exact ratio.

### 4.3 Query plan editor

- The query plan editor SHALL use Monaco.
- The implementation SHOULD reuse the existing Monaco integration pattern from `tools/RulesWorkbench` where practical.
- After a raw-query search, the editor SHALL be populated with the generated query plan used for that execution.
- The editor SHALL preserve user edits until the next explicit raw-query search regeneration or reset action.
- The editor SHOULD support formatted JSON display suitable for direct inspection and editing.
- The editor SHALL include a `Reset to generated plan` action in the initial implementation.

### 4.4 Search actions

- The main search bar `Search` button SHALL submit the raw query text.
- Submitting the raw query text SHALL continue to generate a query plan and execute the search.
- A second `Search` button SHALL appear directly above the query plan editor.
- Clicking the query-plan `Search` button SHALL execute the edited query plan currently shown in the editor.
- Executing an edited query plan SHALL not require re-entering or re-submitting the raw query text.
- The UI SHALL make it clear which search action is operating on raw text and which is operating on the edited plan.

### 4.5 Transformation trace and extracted signals

- The screen SHALL show a compact transformation trace.
- The trace SHALL help the user understand how raw input becomes the generated query plan and then the final Elasticsearch request.
- The screen SHALL show extracted signals relevant to query interpretation.
- Extracted signals SHOULD include structured values such as typed recognizer outputs and any query-derived filters or keywords exposed by the runtime.
- The trace and extracted signals SHOULD be visible without obscuring the central plan/results workflow.

### 4.6 Final Elasticsearch request and diagnostics

- The screen SHALL show the final Elasticsearch request JSON for the current execution.
- The screen SHALL show validation errors that prevent edited-plan execution.
- The screen SHALL show non-fatal warnings where the plan is executable but diagnostically interesting.
- The screen SHALL show execution metrics.
- Execution metrics SHOULD include at least total duration, Elasticsearch duration when available, and hit count.
- The right-hand diagnostics column SHALL initially use stacked sections.
- Lightweight tabs MAY be considered later if stacked sections prove too space-constrained in practice.

### 4.7 Results presentation

- Results SHALL move to a flatter presentation style.
- The results area SHALL avoid nested cards and similarly cluttered nested panel structures.
- Results SHOULD be shown as a clear list of rows or row-like summaries with key fields visible at a glance.
- The currently selected result SHOULD remain visually distinct.
- The results area SHALL continue to support result selection for detail inspection.

### 4.8 Selected result detail

- Detailed result explanation SHALL be available on the same screen.
- Deeper detail SHOULD be placed in a collapsible bottom drawer rather than another always-expanded nested panel.
- The detail drawer SHOULD remain collapsed by default to protect screen space.
- When a result is selected, the detail drawer SHOULD be able to show an explanation-oriented view of why the result matched, plus raw detail where useful.
- The initial implementation SHALL provide a lightweight selected-result explanation summary.
- Deeper clause-level explain detail MAY be added later when backend support is ready.

### 4.9 One-screen information prioritisation

The screen SHALL prioritise information as follows:

1. central editing and result review loop
2. side diagnostics needed for understanding the run
3. deep detail on demand

If space becomes constrained, the UI SHOULD degrade in this order:

1. compact or collapse the transformation trace first
2. reduce or tab the right-column diagnostics sections next
3. keep selected-result explain content in the bottom drawer rather than the main columns

## 5. Non-functional requirements

### 5.1 Usability

- The page SHOULD support rapid iteration for search diagnostics work.
- The raw-query and edited-plan workflows SHOULD each require minimal clicks.
- The visual hierarchy SHOULD make the primary workflow immediately understandable.

### 5.2 Visual design

- The screen SHOULD adopt a flatter, lighter visual style than the current nested-card presentation.
- Section separation SHOULD rely more on spacing, headings, and subtle separators than on heavily nested outlined panels.
- The implementation SHOULD remain close to the stock Radzen Material theme used in this repository.

### 5.3 Performance

- Query plan rendering and updates SHOULD feel responsive for normal developer diagnostic use.
- Editor initialisation SHOULD not introduce unnecessary lag on first query execution.
- Re-running searches from raw query or edited plan SHOULD keep the rest of the page stable where possible.

### 5.4 Accessibility

- Both search actions SHOULD remain keyboard accessible.
- The plan editor host and surrounding actions SHOULD remain operable in an interactive Blazor page.
- Status, warnings, and validation output SHOULD be visible in a way that does not depend only on colour.

## 6. Data model

The UI should work with a run-oriented view model conceptually similar to the following.

### 6.1 Raw query run state

- raw query text
- generated query plan JSON
- execution status
- run summary metrics

### 6.2 Edited plan run state

- current editor text
- validation state
- last successfully executed edited plan
- edited-plan execution status

### 6.3 Diagnostics state

- extracted signals collection
- transformation trace entries
- final Elasticsearch request JSON
- warnings collection
- validation errors collection
- selected result detail/explain payload

This specification does not mandate exact CLR types, but the UI state model must distinguish the generated plan from the user-edited working copy.

## 7. Interfaces & integration

### 7.1 Query UI integration expectations

The UI requires two logical execution capabilities from the host/runtime.

1. **Generate and execute from raw query**
   - input: raw query text and any current UI filters
   - output: generated query plan, final Elasticsearch request, results, metrics, and diagnostics

2. **Execute supplied query plan**
   - input: edited query plan JSON
   - output: final Elasticsearch request, results, metrics, and diagnostics

The second capability is essential to support the new query-plan `Search` button.

### 7.2 Query plan contract dependency

This work package depends on the existence of a stable JSON-serializable query plan contract. The design should align with the repository's query planning direction and diagnostics expectations rather than inventing a separate ad hoc editor-only schema.

### 7.3 Editor integration

The Monaco host should follow the existing repository pattern used in `tools/RulesWorkbench`, adapted as needed for this page.

## 8. Observability (logging/metrics/tracing)

- The UI SHALL surface run-level metrics useful for diagnostics.
- The UI SHOULD surface warnings separately from blocking validation errors.
- The UI SHOULD make transformation stages inspectable without requiring server log access.
- Server-side logging may still exist, but the core developer debugging path SHOULD be possible directly from the screen.

## 9. Security & compliance

- This UI is intended for internal diagnostics and should only expose data already appropriate for internal developer/test use.
- Surfaced Elasticsearch request JSON should not introduce new unsafe exposure beyond what the host is already permitted to execute and show.
- Edited-plan execution should validate incoming plan content before attempting execution.

## 10. Testing strategy

- Add UI-focused tests covering the raw-query and edited-plan execution flows.
- Verify that a raw-query search populates the query plan editor and still returns results.
- Verify that editing the query plan and clicking the pane-level `Search` button executes the edited plan path.
- Verify that validation errors are shown for invalid edited plans.
- Verify that the one-screen layout keeps plan and results simultaneously visible at desktop sizes.
- Verify that results render in the flatter presentation rather than nested-card-heavy composition.
- Prefer end-to-end verification for the Blazor UI behaviour where practical in this repository.

## 11. Rollout / migration

- Implement the uplift as an evolution of the existing `QueryServiceHost` page rather than as a separate replacement page.
- Preserve the existing raw-query search behaviour while adding edited-plan execution.
- Replace visually heavy nested panel structures incrementally with the flatter layout sections defined here.
- Introduce the additional diagnostics in a way that supports gradual enhancement if backend contracts arrive in stages.

## 12. Open questions

None at this stage. The initial implementation defaults are:

- include a `Reset to generated plan` action in the plan editor
- use stacked sections in the right-hand diagnostics column
- provide a lightweight selected-result explanation summary first, with deeper explain support deferred

## 13. Change log

### v0.01

- Initial draft created for work package `096-query-ui-uplift`
- Captures the requested centre split, Monaco query plan editing, edited-plan execution button, flatter results presentation, and one-screen layout approach discussed in chat
- Records additional high-value diagnostics to show now, while leaving recent searches and saved examples out of scope
- Closes the initial open questions by adopting defaults for reset behaviour, stacked diagnostics sections, and lightweight initial result explanation
