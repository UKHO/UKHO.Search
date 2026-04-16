# Implementation Plan

**Target output path:** `docs/096-query-ui-uplift/plan-frontend-query-ui-tidy_v0.01.md`

**Version:** v0.01 (Draft)

**Based on:**
- `docs/096-query-ui-uplift/spec-frontend-query-ui-tidy_v0.01.md`

**Mandatory instruction references:**
- `./.github/instructions/wiki.instructions.md` — mandatory completion gate for every work item in this plan
- `./.github/instructions/documentation-pass.instructions.md` — mandatory Definition of Done requirement for every code-writing task in this plan

## Query UI Tidy-Up

- [x] Work Item 1: Remove redundant instructional copy and simplify query workspace labels - Completed
  - **Completion summary**: Removed the approved redundant helper copy from `SearchBar.razor`, `QueryInsightPanel.razor`, `QueryPlanPanel.razor`, `ResultsPanel.razor`, and `QueryDiagnosticsPanel.razor`; removed the final two duplicated query-insight trace items while preserving the remaining panel structure; changed the generated-plan action label from `Reset to generated plan` to `Reset`; and added focused source-based host tests in `test/QueryServiceHost.Tests/QueryHostAuthenticationCompositionTests.cs` to verify the removed text stays absent, the core headings remain present, and the simplified reset label is rendered in source. Validation performed: `run_tests` for project `QueryServiceHost.Tests` (16/16 passed) and `run_build` (successful). Wiki review result: reviewed the expected query guidance targets from this plan and searched the repository for `wiki/Query-Walkthrough.md`, `wiki/Query-Pipeline.md`, `wiki/Glossary.md`, and a `wiki/` folder; no repository wiki pages currently exist for this area, so no wiki page update was possible or required for this tightly scoped label/copy tidy-up.
  - **Purpose**: Deliver an immediately cleaner and less repetitive query workspace by removing helper copy that duplicates visible UI structure, diagnostics, and status information, while preserving the current execution flows.
  - **Acceptance Criteria**:
    - The header no longer shows `Run a raw query to regenerate the plan shown in the Monaco workspace.`
    - The query insight panel no longer shows `Extracted signals and a compact staged trace stay visible so the current execution path can be explained without leaving the page.`
    - The query insight panel no longer shows the final two duplicated informational items about diagnostics visibility and `215 match(es)`.
    - The generated query plan pane no longer shows the descriptive copy explaining baseline regeneration, direct execution, and generated-plan readiness.
    - The results panel no longer shows `Flat result rows stay visible beside the generated plan workspace.`
    - The diagnostics panel no longer shows the descriptive copy about request JSON, validation output, warnings, and execution metrics.
    - The generated query plan action label reads `Reset` instead of `Reset to generated plan`.
  - **Definition of Done**:
    - Code implemented for the targeted text removals and label updates without changing query execution behaviour
    - All code written or updated follows `./.github/instructions/documentation-pass.instructions.md` in full, including developer-level comments on every class, method, constructor, public parameter, and non-obvious property touched by the slice
    - Logging and error handling remain intact and unchanged in behaviour where copy-only UI refinements are applied
    - Targeted tests pass for the host UI state and component rendering paths affected by this slice
    - Documentation updated where contributor-facing behaviour or labels are materially clarified
    - Wiki review completed per `./.github/instructions/wiki.instructions.md`; relevant wiki or repository guidance updated, or an explicit no-change review result recorded
    - Foundational documentation retains book-like narrative depth, defines technical terms when first introduced, and includes examples or walkthrough support where the subject matter is conceptually dense
    - Can execute end to end via: run the host, open the query page, execute a query, and verify the simplified copy and updated `Reset` label appear in the active workspace
  - [x] Task 1: Remove redundant helper copy from the query host components - Completed
    - [x] Step 1: Identify the Blazor components in `QueryServiceHost` that render the header, query insight, generated query plan, results, and diagnostics descriptive text.
    - [x] Step 2: Remove only the specified helper and descriptive text blocks so the visual tidy-up remains tightly scoped to the approved specification.
    - [x] Step 3: Remove the final two duplicated query insight items while preserving the remaining insight content and panel structure.
    - [x] Step 4: Confirm no accessibility-critical labels, headings, or actionable controls are removed accidentally while deleting descriptive copy.
    - [x] Step 5: Apply the repository documentation-pass standard from `./.github/instructions/documentation-pass.instructions.md` to all code written in this task.
  - [x] Task 2: Update the generated plan action label and preserve existing behaviour - Completed
    - [x] Step 1: Change the generated query plan reset action text from `Reset to generated plan` to `Reset`.
    - [x] Step 2: Verify the underlying action still restores the Monaco editor content to the latest generated plan without changing its semantics.
    - [x] Step 3: Review any host-level constants, shared labels, or tests that assert the old text and update them consistently.
    - [x] Step 4: Apply the repository documentation-pass standard from `./.github/instructions/documentation-pass.instructions.md` to all code written in this task.
  - [x] Task 3: Add targeted verification for simplified copy and labels - Completed
    - [x] Step 1: Add or update host tests that assert the removed text is no longer rendered in the relevant components or page output.
    - [x] Step 2: Add or update tests that assert the plan reset action label is now `Reset`.
    - [x] Step 3: Prefer repository-aligned targeted host verification over introducing new test frameworks for this tidy-up slice.
  - **Files**:
    - `src/Hosts/QueryServiceHost/Components/Pages/Home.razor`: remove header-level explanatory copy if it is rendered from the page shell.
    - `src/Hosts/QueryServiceHost/Components/QueryInsightPanel.razor`: remove the specified explanatory sentence and the two duplicated trailing items.
    - `src/Hosts/QueryServiceHost/Components/QueryPlanPanel.razor`: remove the descriptive block and rename the reset action to `Reset`.
    - `src/Hosts/QueryServiceHost/Components/ResultsPanel.razor`: remove the specified descriptive text.
    - `src/Hosts/QueryServiceHost/Components/QueryDiagnosticsPanel.razor`: remove the specified diagnostics explanatory copy.
    - `test/QueryServiceHost.Tests/*`: update or add focused tests for rendered copy and label assertions.
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - `dotnet test test/QueryServiceHost.Tests/QueryServiceHost.Tests.csproj`
    - `dotnet run --project src/Hosts/QueryServiceHost/QueryServiceHost.csproj`
    - Open `/`, execute a query, and verify the specified text blocks are absent and the plan action shows `Reset`.
  - **User Instructions**:
    - Use a query that populates the insight, plan, results, and diagnostics regions so all targeted text removals can be checked in one pass.

- [x] Work Item 2: Convert selected result explanation into a full-screen opaque detail mode - Completed
  - **Completion summary**: Reworked `Home.razor` and `Home.razor.css` so the selected-result explanation now takes over the page in a dedicated full-screen mode when open and returns to the standard query workspace when closed; updated `ResultExplainDrawer.razor` and `ResultExplainDrawer.razor.css` so the open state uses a solid opaque surface, the action label reads `Back`, the compact closed state remains available as the launcher beneath the main workspace, and the raw JSON section now stays constrained and scrollable inside the full-screen detail view through an explicit grid-based remaining-height layout; aligned `QueryUiState.cs` comments and explanation-mode terminology with the retained state behavior; and added focused verification in `test/QueryServiceHost.Tests/QueryHostAuthenticationCompositionTests.cs` and `test/QueryServiceHost.Tests/QueryUiStateTests.cs` to cover the full-screen explanation shell, `Back` wording, opaque styling, and workspace-state preservation when returning from explanation mode. Validation performed: `run_tests` for project `QueryServiceHost.Tests` (17/17 passed) and `run_build` (successful). Wiki review result: updated `wiki/Query-Pipeline.md` and `wiki/Query-Walkthrough.md` so the current QueryServiceHost workspace narrative now describes the `Reset` plan action, the full-screen opaque selected-result explanation mode, and the `Back` return path accurately.
  - **Purpose**: Make selected-result inspection feel deliberate and readable by replacing the translucent overlay-style presentation with a full-screen opaque explanation mode that provides a clear `Back` return path to the main workspace.
  - **Acceptance Criteria**:
    - Opening selected result explanation presents a full-screen takeover view.
    - The explanation view uses a solid, non-transparent background.
    - The explanation action label reads `Back` instead of `Collapse`.
    - Activating `Back` returns the user to the prior main query workspace state without altering the current query, plan, diagnostics, or result selection semantics.
    - Existing explanation content remains available within the new presentation mode.
  - **Definition of Done**:
    - Code implemented for the full-screen selected-result explanation state, opaque styling, and `Back` action label
    - All code written or updated follows `./.github/instructions/documentation-pass.instructions.md` in full, including developer-level comments on every class, method, constructor, public parameter, and non-obvious property touched by the slice
    - State transitions, logging, and user-visible error handling remain coherent if explanation content is absent or selection changes
    - Targeted tests pass for explanation-view state transitions and return navigation behaviour
    - Documentation updated where contributor-facing workflow or terminology is materially clarified
    - Wiki review completed per `./.github/instructions/wiki.instructions.md`; relevant wiki or repository guidance updated, or an explicit no-change review result recorded
    - Foundational documentation retains book-like narrative depth, defines technical terms when first introduced, and includes examples or walkthrough support where the subject matter is conceptually dense
    - Can execute end to end via: run a query, select a result, open explanation, verify full-screen opaque presentation, and return with `Back`
  - [x] Task 1: Rework the explanation component into a dedicated full-screen state - Completed
    - [x] Step 1: Identify the current Blazor component and page-level state that control selected-result explanation presentation.
    - [x] Step 2: Replace the translucent overlay-style rendering with a full-screen takeover layout that still fits the current `QueryServiceHost` page architecture.
    - [x] Step 3: Preserve the existing selected-result explanation content while moving it into the new full-screen surface.
    - [x] Step 4: Ensure the transition into and out of full-screen mode does not reset the active query workspace state.
    - [x] Step 5: Apply the repository documentation-pass standard from `./.github/instructions/documentation-pass.instructions.md` to all code written in this task.
  - [x] Task 2: Update explanation styling and return action wording - Completed
    - [x] Step 1: Remove transparency styling and use an opaque background that visually reads as a dedicated detail surface.
    - [x] Step 2: Rename the explanation action button from `Collapse` to `Back`.
    - [x] Step 3: Verify the updated label still communicates a return-to-workspace action clearly in desktop Blazor usage.
    - [x] Step 4: Apply the repository documentation-pass standard from `./.github/instructions/documentation-pass.instructions.md` to all code written in this task.
  - [x] Task 3: Add targeted verification for full-screen explanation behaviour - Completed
    - [x] Step 1: Add or update host tests that assert the explanation state can enter full-screen mode and return cleanly.
    - [x] Step 2: Add or update tests that assert the `Back` label is rendered instead of `Collapse`.
    - [x] Step 3: Add or update tests that cover the expected state preservation for the main query workspace after returning from explanation view.
  - **Files**:
    - `src/Hosts/QueryServiceHost/Components/ResultExplainDrawer.razor`: evolve the selected-result explanation surface into a full-screen detail mode.
    - `src/Hosts/QueryServiceHost/Components/Pages/Home.razor`: host any page-level state or layout changes required to let the explanation take over the full screen.
    - `src/Hosts/QueryServiceHost/Components/Pages/Home.razor.css`: update layout rules for the explanation full-screen state and opaque styling.
    - `src/Hosts/QueryServiceHost/State/QueryUiState.cs`: add or refine host state used to enter and exit the explanation full-screen mode while preserving the main workspace state.
    - `test/QueryServiceHost.Tests/*`: add or update focused tests for full-screen explanation transitions and `Back` behaviour.
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test test/QueryServiceHost.Tests/QueryServiceHost.Tests.csproj`
    - `dotnet run --project src/Hosts/QueryServiceHost/QueryServiceHost.csproj`
    - Open `/`, execute a query with at least one result, select a result, open explanation, verify the opaque full-screen view, then click `Back` and confirm the main workspace returns intact.
  - **User Instructions**:
    - Use a query that returns at least one result so the explanation workflow can be exercised.
    - Keep the same browser session open when verifying that returning from explanation view preserves the active workspace context.

- [ ] Work Item 3: Complete the mandatory wiki review and record the tidy-up documentation outcome
  - **Purpose**: Satisfy the repository requirement that every work package ends with an explicit wiki review or wiki update record covering the implemented contributor-facing behaviour, terminology, and workflow guidance.
  - **Acceptance Criteria**:
    - Relevant wiki pages and repository guidance have been reviewed against the implemented Query UI tidy-up behaviour.
    - Any stale guidance is updated in current-state narrative form with technical terms explained and examples added where they materially improve comprehension.
    - If no wiki page requires changes, the final work-package record explicitly states what was reviewed and why no update was required.
  - **Definition of Done**:
    - Wiki review completed per `./.github/instructions/wiki.instructions.md`
    - Final execution record states which wiki or repository guidance pages were updated, created, retired, or why none changed
    - Any dense architecture or workflow explanations remain in long-form, book-like narrative prose rather than terse bullet-only treatment
    - Can execute completion review via: inspect the changed wiki pages or the recorded no-change result before closing the work package
  - [ ] Task 1: Review the query UI contributor guidance reader path
    - [ ] Step 1: Review query-related wiki or repository guidance that describes the diagnostics workspace, generated plan actions, or selected-result explanation workflow.
    - [ ] Step 2: Check whether the copy removal, `Reset` label, or `Back` terminology materially affects contributor understanding or screenshots/walkthrough prose.
    - [ ] Step 3: Update the relevant guidance if the implemented current-state behaviour is no longer described accurately.
  - [ ] Task 2: Record the final documentation outcome
    - [ ] Step 1: Record which wiki or repository guidance pages were updated, created, retired, or reviewed and left unchanged.
    - [ ] Step 2: If no changes were needed, record a grounded no-change result explaining what was reviewed and why the existing documentation remains sufficient.
  - **Files**:
    - `wiki/Query-Walkthrough.md`: likely review target if contributor-facing query UI usage steps or screenshots are affected.
    - `wiki/Query-Pipeline.md`: likely review target if query workspace terminology or behaviour descriptions are affected.
    - `wiki/Glossary.md`: possible review target if `Back`, explanation mode, or other UI terminology needs clarification.
    - `docs/096-query-ui-uplift/*`: record the final work-package outcome if the execution workflow for this package captures a closure summary.
  - **Work Item Dependencies**: Work Item 2
  - **Run / Verification Instructions**:
    - Review the changed or reviewed wiki pages in the repository viewer.
    - Confirm the final work-package record explicitly captures the wiki review outcome.

## Overall approach summary

This plan keeps the tidy-up intentionally small, frontend-focused, and runnable after each slice. The first slice removes redundant copy and aligns labels without disturbing existing query behaviour. The second slice upgrades selected-result explanation into a clearer full-screen detail mode while preserving the surrounding Blazor workspace state. The final slice enforces the repository's mandatory wiki-review completion gate and records whether contributor guidance needed to change.

Key implementation considerations are:
- keep the scope tightly bound to the approved presentational changes
- preserve existing raw-query, edited-plan, diagnostics, and result-selection behaviour
- use targeted host tests rather than broad new test infrastructure
- treat `./.github/instructions/documentation-pass.instructions.md` and `./.github/instructions/wiki.instructions.md` as hard completion gates for implementation
- keep any contributor-facing documentation updates in current-state, long-form narrative style where the topic is conceptually dense
