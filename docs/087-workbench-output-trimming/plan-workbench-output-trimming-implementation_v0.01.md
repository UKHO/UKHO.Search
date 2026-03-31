# Implementation Plan

- Version: `v0.01`
- Status: `Draft`
- Work Package: `087-workbench-output-trimming`
- Target Spec: `docs/087-workbench-output-trimming/spec-domain-workbench-output-trimming_v0.01.md`
- Plan Path: `docs/087-workbench-output-trimming/plan-workbench-output-trimming-implementation_v0.01.md`

## Mandatory implementation standards

- All code-writing work in this plan must comply fully with `./.github/instructions/documentation-pass.instructions.md`.
- `./.github/instructions/documentation-pass.instructions.md` is a hard gate and mandatory Definition of Done criterion for every code-writing task in this plan.
- Every code-writing task must deliver fully commented code for all written or updated code, including internal and other non-public types, methods, constructors, relevant properties, and explanatory inline comments describing logical flow and rationale.
- Implementation must also follow repository standards in `./.github/copilot-instructions.md`, `./.github/instructions/coding-standards.instructions.md`, `./.github/instructions/frontend.instructions.md`, and `./.github/instructions/testing.instructions.md`.
- For this work package, do not run the full test suite; run targeted tests only, consistent with repository testing guidance.

## Overall approach

This plan delivers the Workbench output trimming change as a small set of vertical slices inside the existing `WorkbenchHost` shell:

1. add a runnable output-level filtering slice with `Info and above` as the default user-visible behavior
2. reduce useless debug-oriented shell output at source so the default experience becomes materially quieter without altering retained-entry semantics
3. harden and polish the output-pane behavior with targeted verification and regression coverage so the quieter output experience remains stable

Each work item leaves the Workbench in a runnable, demonstrable state.

---

## Output pane filtering and trimming

- [ ] Work Item 1: Deliver end-to-end output-level filtering with `Info and above` as the default visible mode
  - **Purpose**: Introduce the core user-facing capability that immediately reduces output-pane noise in normal operation while preserving access to `Debug` output when explicitly requested.
  - **Acceptance Criteria**:
    - The output pane defaults to showing `Info`, `Warning`, and `Error` entries only.
    - `Debug` entries are hidden by default but can be revealed through a toolbar selector.
    - The output-pane toolbar contains a minimum-level selector with `Error`, `Warning and above`, `Info and above`, and `Debug` options.
    - Changing the selector updates the visible output immediately without requiring the panel to close and reopen.
    - Hidden lower-level entries do not contribute to unseen indicators or badges.
    - Detail lines remain visible for all visible entries, including when `Debug` is enabled.
  - **Definition of Done**:
    - Code implemented for output filter state, toolbar UI, and filtered rendering
    - Logging and error handling kept consistent with existing Workbench shell patterns
    - Targeted tests passing for rendering and selection behavior
    - Code comments and developer documentation added in full compliance with `./.github/instructions/documentation-pass.instructions.md`
    - Documentation updated where needed inside this work package
    - Can execute end-to-end via: run `WorkbenchHost`, open the output pane, switch filter levels, and observe the visible output change live
  - [ ] Task 1: Add session-scoped output-level filter state to the existing Workbench output-panel model
    - [ ] Step 1: Review the existing output-panel state model and identify the smallest extension point for session-only minimum severity selection.
    - [ ] Step 2: Add or update the output-panel state/service contract so the current visible minimum level is tracked in-memory only for the active session.
    - [ ] Step 3: Ensure the default state initializes to `Info and above` whenever a new session starts.
    - [ ] Step 4: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 2: Add the output-level selector to the output-pane toolbar in `WorkbenchHost`
    - [ ] Step 1: Update the output-pane toolbar markup in the Workbench layout to include a selector that fits the current Radzen Material styling approach.
    - [ ] Step 2: Wire the selector to the new session-scoped minimum-level state.
    - [ ] Step 3: Keep the toolbar desktop-like and aligned with the existing output-pane interaction model.
    - [ ] Step 4: Ensure the selector alone is the only debug exposure control; do not add a separate debug toggle or reset-to-default affordance.
    - [ ] Step 5: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Apply the filter to the visible output rendering path without changing retained-entry semantics
    - [ ] Step 1: Update the output projection/rendering logic so the visible entry set is filtered by the current minimum level before rendering.
    - [ ] Step 2: Preserve retained output entries internally so filter changes can reveal previously hidden entries in-session.
    - [ ] Step 3: Keep detail lines visible for all entries that remain visible after filtering.
    - [ ] Step 4: Ensure hidden lower-level entries do not affect unseen indicators or badges.
    - [ ] Step 5: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 4: Add targeted verification for the end-to-end filter slice
    - [ ] Step 1: Add or update Workbench host rendering tests covering the default `Info and above` view.
    - [ ] Step 2: Add tests verifying that `Debug` entries are hidden by default and revealed when the selector is set to `Debug`.
    - [ ] Step 3: Add tests verifying the selector options and immediate visible output refresh behavior.
    - [ ] Step 4: Add tests confirming detail lines remain visible for all visible severities.
    - [ ] Step 5: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - **Files**:
    - `src/workbench/server/UKHO.Workbench/Output/OutputPanelState.cs`: extend session output-pane state with the minimum visible level if needed
    - `src/workbench/server/UKHO.Workbench/Output/IWorkbenchOutputService.cs`: expose filter-related state or update operations if needed
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`: add the toolbar selector
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.cs`: apply filter state and visible rendering behavior
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`: style the selector consistently with the output toolbar if needed
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: add targeted rendering and behavior tests
  - **Work Item Dependencies**: None
  - **Run / Verification Instructions**:
    - Run targeted tests for `WorkbenchHost.Tests` that cover `MainLayout` rendering behavior
    - Start `WorkbenchHost`
    - Open the output pane
    - Confirm the default filter is `Info and above`
    - Switch between `Error`, `Warning and above`, `Info and above`, and `Debug`
    - Confirm the visible output updates immediately without pane reopen
  - **User Instructions**: No manual setup beyond running the existing Workbench host

- [ ] Work Item 2: Reduce useless shell debug output at source while preserving meaningful retained entries
  - **Purpose**: Make the output pane materially quieter in normal operation by preventing low-value shell-state messages from being emitted repeatedly, instead of deduplicating entries after they have been written.
  - **Acceptance Criteria**:
    - Known non-useful shell-state messages such as repeated `Tool surface ready: True` and `Active region: ToolSurface` no longer flood the output pane.
    - Source-level trimming is implemented by reducing or preventing useless debug output emission, not by deduplicating retained entries after write.
    - Genuine retained entries that are still emitted remain fully visible and are not coalesced or suppressed by the output pane.
    - Normal operation produces materially fewer visible lines than before under the default filter.
  - **Definition of Done**:
    - Shell context/status emission logic updated to stop producing useless repeated messages
    - Existing retained output behavior preserved for messages that are still emitted
    - Targeted tests passing for context/status projection behavior
    - Code comments and developer documentation added in full compliance with `./.github/instructions/documentation-pass.instructions.md`
    - Can execute end-to-end via: run `WorkbenchHost`, navigate through normal shell activity, and observe a much quieter output pane without any retained-entry deduplication behavior
  - [ ] Task 1: Audit current shell-emitted output sources that create high-volume low-value debug noise
    - [ ] Step 1: Review context projection and status projection paths in the Workbench layout and related shell services.
    - [ ] Step 2: Identify which messages are genuinely useful diagnostics versus which are low-value state chatter.
    - [ ] Step 3: Use the specification examples as minimum mandatory targets, especially repeated `Tool surface ready` and `Active region` messages.
    - [ ] Step 4: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 2: Reduce useless repeated debug output at source
    - [ ] Step 1: Adjust shell context/status projection rules so unchanged low-value state is not repeatedly written into the output stream.
    - [ ] Step 2: Preserve meaningful informational, warning, and error events so output still reflects real operational activity.
    - [ ] Step 3: Ensure the implementation does not deduplicate retained output entries after write; the reduction must happen before or at emission.
    - [ ] Step 4: Keep the current chronological retained-entry model intact for messages that are still emitted.
    - [ ] Step 5: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Add focused regression coverage for shell output-noise reduction
    - [ ] Step 1: Add or update tests around shell context and status projection to verify low-value unchanged state is no longer emitted repeatedly.
    - [ ] Step 2: Add tests confirming meaningful emitted entries remain visible and unmodified.
    - [ ] Step 3: Add tests confirming no UI-side deduplication/coalescing behavior is introduced.
    - [ ] Step 4: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - **Files**:
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.cs`: refine shell output emission and projection rules
    - `src/workbench/server/UKHO.Workbench/WorkbenchShell/*.cs`: update related shell state/contribution behavior only if required by the proven root cause
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: add regression tests for reduced low-value shell output
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - Run targeted `WorkbenchHost.Tests` covering shell output projection
    - Start `WorkbenchHost`
    - Open the output pane with the default `Info and above` filter
    - Exercise normal shell interactions such as tool activation and region changes
    - Confirm that the previous flood of low-value debug/state messages no longer appears
    - Switch to `Debug` and confirm that meaningful debug output remains available when intentionally enabled
  - **User Instructions**: No manual setup beyond normal Workbench navigation

- [ ] Work Item 3: Polish the filtered output experience and close verification gaps for a stable demonstrable slice
  - **Purpose**: Finalize the user experience and regression protection so the trimmed output pane remains understandable, responsive, and aligned with the specification as the feature evolves.
  - **Acceptance Criteria**:
    - The output-pane toolbar remains visually coherent after adding the selector.
    - Output filtering, detail-line visibility, and source-level noise reduction all work together without breaking existing panel interactions.
    - Existing panel behaviors such as scrolling, find, clear, and output visibility toggling continue to work with the new filtering model.
    - Regression coverage exists for the integrated output-pane experience.
  - **Definition of Done**:
    - Integrated feature validated end-to-end in the Workbench host
    - Targeted regression tests passing
    - Logging/error-handling behavior remains appropriate
    - Code comments and developer documentation added in full compliance with `./.github/instructions/documentation-pass.instructions.md`
    - Can execute end-to-end via: run `WorkbenchHost`, use the output pane under normal and debug modes, and verify the trimmed experience remains stable
  - [ ] Task 1: Validate interaction compatibility with existing output-pane features
    - [ ] Step 1: Review how filtering interacts with clear, find, auto-scroll, scroll-to-end, and visibility toggle behavior.
    - [ ] Step 2: Make only the minimal adjustments needed so these behaviors continue to work predictably with filtered visible output.
    - [ ] Step 3: Ensure no new ambiguity is introduced between retained entries and currently visible entries.
    - [ ] Step 4: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 2: Refine toolbar and output-pane presentation details
    - [ ] Step 1: Adjust styling only where needed to keep the new selector aligned with the current Radzen Material toolbar presentation.
    - [ ] Step 2: Verify the output pane still feels desktop-like rather than web-form-like.
    - [ ] Step 3: Keep the solution shell-owned; do not introduce module-specific styling workarounds.
    - [ ] Step 4: Add developer-level comments and XML documentation as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Add integrated verification and documentation completion
    - [ ] Step 1: Add final targeted tests covering the integrated behavior of filtering, visible details, and reduced source-level shell noise.
    - [ ] Step 2: Verify the implementation against the work package spec line by line.
    - [ ] Step 3: Confirm all code-writing tasks in this work item satisfy `./.github/instructions/documentation-pass.instructions.md` as a hard Definition of Done requirement.
    - [ ] Step 4: Update this work package documentation if implementation details require clarifying notes.
  - **Files**:
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.cs`
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`
    - `docs/087-workbench-output-trimming/spec-domain-workbench-output-trimming_v0.01.md`: clarification updates only if implementation evidence requires them
  - **Work Item Dependencies**: Work Item 1, Work Item 2
  - **Run / Verification Instructions**:
    - Run targeted `WorkbenchHost.Tests`
    - Start `WorkbenchHost`
    - Open the output pane
    - Verify the default `Info and above` experience is readable
    - Switch to `Debug` and verify details remain visible
    - Use clear, find, auto-scroll, and output visibility toggle interactions
    - Confirm the pane remains stable and quieter than the current baseline
  - **User Instructions**: No additional setup required

## Summary

This plan delivers the workbench output trimming change in three runnable slices: first the end-user severity filter and default `Info and above` view, then source-level removal of useless debug noise, and finally integration polish plus regression coverage. The key implementation consideration is to improve readability by changing what gets emitted and what is shown by default, while explicitly preserving retained-entry semantics and treating `./.github/instructions/documentation-pass.instructions.md` as a mandatory completion gate for all code-writing work.
