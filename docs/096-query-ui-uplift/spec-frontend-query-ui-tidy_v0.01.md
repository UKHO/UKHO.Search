# Specification: Query UI Tidy-Up

**Target output path:** `docs/096-query-ui-uplift/spec-frontend-query-ui-tidy_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

### 1.1 Purpose

This specification defines a focused UI tidy-up for the query diagnostics experience in `QueryServiceHost`. The goal is to reduce explanatory copy, remove duplicated information, and make the selected result explanation view behave like a full-screen task surface rather than a translucent overlay.

### 1.2 Scope

This work covers:

- removing specific helper and descriptive text from the query UI
- simplifying the query insight panel by removing duplicated status items
- changing the selected result explanation view to take over the full screen
- removing transparency from the selected result explanation surface
- renaming the selected result explanation action from `Collapse` to `Back`
- renaming the generated query plan reset action from `Reset to generated plan` to `Reset`

This work does not cover:

- changing query generation or execution behaviour
- changing diagnostics payloads or backend contracts
- redesigning the page layout beyond the targeted selected-result explanation takeover behaviour

### 1.3 Stakeholders

- query developers
- QA and technical testers
- internal users of the query diagnostics UI

### 1.4 Definitions

- **Query insight panel**: the panel showing extracted signals and staged execution context
- **Generated query plan pane**: the Monaco-based plan editor area
- **Selected result explanation**: the detail view for the currently selected search result

## 2. System context

### 2.1 Current state

The current query UI includes several explanatory text blocks that repeat information already conveyed by layout, labels, diagnostics, or surrounding controls. The selected result explanation appears as an overlay-style panel with transparency and a `Collapse` action.

### 2.2 Proposed state

The UI shall present a cleaner, more direct diagnostics experience by removing redundant descriptive copy and by treating selected-result explanation as a dedicated full-screen detail mode with a solid, non-transparent surface and a `Back` action.

### 2.3 Assumptions

- the current page already exposes the referenced panels and labels
- the selected result explanation can be presented as a full-screen state without changing the underlying result selection model
- the removed text is informational only and not required for accessibility or workflow completion

### 2.4 Constraints

- the changes must stay within the existing work package scope for `096-query-ui-uplift`
- the update must remain a frontend-only specification unless later implementation reveals a genuine contract dependency
- the selected result explanation must still provide a clear return path to the main query workspace

## 3. Component / service design (high level)

### 3.1 Components

1. **Header area**
   - remove the descriptive text `Run a raw query to regenerate the plan shown in the Monaco workspace.`

2. **Query insight panel**
   - remove the descriptive text `Extracted signals and a compact staged trace stay visible so the current execution path can be explained without leaving the page.`
   - remove the final two items currently stating that the Elasticsearch request JSON is available in diagnostics and that execution returned `215 match(es)` through the raw-query path

3. **Generated query plan pane**
   - remove the descriptive text block beginning `The top command bar regenerates the baseline plan from raw query text...`
   - rename `Reset to generated plan` to `Reset`

4. **Results panel**
   - remove the descriptive text `Flat result rows stay visible beside the generated plan workspace.`

5. **Diagnostics panel**
   - remove the descriptive text block beginning `Diagnostics` and describing request JSON, validation output, warnings, and execution metrics remaining visible

6. **Selected result explanation view**
   - expand to a full-screen takeover state
   - use an opaque background with no transparency treatment
   - rename the action button from `Collapse` to `Back`

### 3.2 Data flows

No new data flow is required. The change is presentational only and reuses the existing query execution, diagnostics, and result selection flows.

### 3.3 Key decisions

- Redundant helper text shall be removed rather than rewritten.
- Duplicated insight items shall be removed when their information is already visible elsewhere on the page.
- Selected result explanation shall behave as a dedicated full-screen detail mode.
- The return action label shall be `Back`.
- The generated plan reset action label shall be `Reset`.

## 4. Functional requirements

- The header SHALL no longer display `Run a raw query to regenerate the plan shown in the Monaco workspace.`
- The query insight panel SHALL no longer display `Extracted signals and a compact staged trace stay visible so the current execution path can be explained without leaving the page.`
- The query insight panel SHALL remove the last two informational items that repeat diagnostics and execution-count information already shown elsewhere.
- The generated query plan pane SHALL no longer display the descriptive copy explaining raw-query regeneration, direct execution, and generated-plan readiness.
- The generated query plan reset action SHALL be labelled `Reset`.
- The results panel SHALL no longer display `Flat result rows stay visible beside the generated plan workspace.`
- The diagnostics panel SHALL no longer display the descriptive copy explaining final request JSON, validation output, warnings, and execution metrics.
- The selected result explanation panel SHALL open as a full-screen takeover view.
- The selected result explanation view SHALL use a solid, non-transparent background.
- The selected result explanation return action SHALL be labelled `Back`.
- Activating `Back` SHALL return the user to the prior main query workspace state.
- These changes SHALL not alter query execution behaviour, diagnostics generation, or result selection semantics.

## 5. Non-functional requirements

- The tidy-up SHOULD reduce visual noise and repeated instructional copy.
- The full-screen selected-result explanation view SHOULD feel intentional and desktop-like rather than modal or translucent.
- The updated labels SHOULD remain concise and immediately understandable.

## 6. Data model

No data model change is required. Existing UI state may add or reuse a boolean or equivalent state marker indicating whether the selected result explanation is in full-screen mode.

## 7. Interfaces & integration

No new external interface is required. Existing UI components and state transitions should be reused.

## 8. Observability (logging/metrics/tracing)

No new observability requirement is introduced. Existing diagnostics remain available, but duplicated on-screen explanatory copy is removed.

## 9. Security & compliance

This change does not introduce new security or compliance requirements because it only changes presentation and labels.

## 10. Testing strategy

- Verify each specified text block is absent from the updated UI.
- Verify the query insight panel no longer shows the two duplicated trailing items.
- Verify the generated query plan action label reads `Reset`.
- Verify opening selected result explanation presents a full-screen opaque view.
- Verify the explanation view action label reads `Back`.
- Verify `Back` returns to the main query workspace without losing the current page context.

## 11. Rollout / migration

Implement as an in-place refinement of the existing query UI in `QueryServiceHost`. No migration or phased rollout is required.

## 12. Open questions

None at this stage.
