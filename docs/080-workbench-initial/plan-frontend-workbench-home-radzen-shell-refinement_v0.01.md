# Implementation Plan

**Target output path:** `docs/080-workbench-initial/plan-frontend-workbench-home-radzen-shell-refinement_v0.01.md`

**Based on:** `docs/080-workbench-initial/spec-frontend-workbench-home-radzen-mockup_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Planning Constraints

- This plan covers only the newly specified shell refinement features: top application menu, per-section tabs, lower middle pane, and upper-middle Radzen styling showcase.
- The existing completed plan documents in `docs/080-workbench-initial` remain historical records and must not be overwritten.
- Every code-writing task in this plan must comply fully with `./.github/instructions/documentation-pass.instructions.md`.
- Compliance with `./.github/instructions/documentation-pass.instructions.md` is a mandatory Definition of Done gate, not optional polish.
- Every touched class, method, constructor, relevant property, and public parameter must be documented to the standard required by `./.github/instructions/documentation-pass.instructions.md`, including internal and other non-public types where applicable.
- The implementation must continue following repository Blazor guidance, including explicit `@rendermode InteractiveServer` on interactive pages.
- The implementation should prefer Radzen-native components in line with `./.github/instructions/radzen-instructions.md` and repository frontend guidance.
- The work remains a temporary visual mock-up. Real commands, persisted tabs, backend integration, and production workflows remain out of scope.
- No new automated tests are to be added for this work item, exactly as required by the specification.
- Even though no new automated tests are required, final validation for code changes must still include a full solution build and a full test suite run per `./.github/instructions/documentation-pass.instructions.md`.

## Overall Project Structure

- Keep the implementation centered on the existing `WorkbenchHost` Blazor page rather than introducing a new page or route.
- Prefer updating the existing shell files first:
  - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor`
  - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor.css`
- Add small supporting models or helper types only if they make the tab, menu, or sample data markup materially clearer.
- If helper C# types are introduced, place them alongside the relevant Workbench component code, keep one public type per file, use block-scoped namespaces, use Allman braces, and document them fully per `./.github/instructions/documentation-pass.instructions.md`.

## Shell Refinement Slices

### Menu and Section Tab Chrome
- [x] Work Item 1: Add the top application menu and tab chrome to the shell - Completed
  - **Purpose**: Deliver the first visible refinement slice by making the existing shell look more like a desktop workbench without yet changing the underlying feature scope.
  - **Acceptance Criteria**:
    - A full-width `RadzenMenu` appears above the current shell body.
    - The menu renders standard placeholder headings such as `File`, `Edit`, `View`, and `Help` with sensible submenu items.
    - The left sidebar content area, upper middle pane, and right sidebar content area all render a top-aligned tab strip.
    - Each rendered section tab strip exposes two or three tabs.
    - Each tab header includes a close icon aligned to the right side of the tab label.
    - Existing left and right activity bar behavior still works after the menu and tab chrome are introduced.
  - **Definition of Done**:
    - Code implemented for top menu and section tab chrome
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for every touched source file
    - Developer-level comments added to all touched files as required by `./.github/instructions/documentation-pass.instructions.md`
    - No new automated tests added, per specification
    - Full solution build completed successfully
    - Full test suite run completed successfully, or any failures are explained as pre-existing and unrelated
    - Can execute end-to-end via `WorkbenchHost` and visually confirm menu and tab chrome
  - [x] Task 1: Add top application menu structure using Radzen components - Completed
    - [x] Step 1: Add a full-width `RadzenMenu` above the current shell layout in `Home.razor`.
    - [x] Step 2: Add top-level menu items for `File`, `Edit`, `View`, and `Help` using nested `RadzenMenuItem` entries.
    - [x] Step 3: Populate each top-level entry with sensible placeholder submenu items.
    - [x] Step 4: Keep menu behavior placeholder-only unless minimal click handling is required for a complete visual example.
    - [x] Step 5: Document all touched code per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 2: Add reusable tab presentation to the existing shell sections - Completed
    - [x] Step 1: Choose the simplest Radzen-native tab approach for fixed tab sets in this mock-up.
    - [x] Step 2: Add two or three tabs to the left sidebar content region.
    - [x] Step 3: Add two or three tabs to the upper middle pane.
    - [x] Step 4: Add two or three tabs to the right sidebar content region.
    - [x] Step 5: Add a right-aligned close icon to each tab header using the chosen tab header rendering approach.
    - [x] Step 6: Treat tab close behavior as visual-only unless a trivial no-op handler is required to complete the UI affordance.
    - [x] Step 7: Document all touched code per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 3: Update styling so new chrome feels consistent with the existing shell - Completed
    - [x] Step 1: Add menu spacing, borders, and background treatment that fits the existing desktop shell.
    - [x] Step 2: Add tab strip styling that works in both light and dark themes.
    - [x] Step 3: Ensure tab close icons remain visible, aligned, and discoverable.
    - [x] Step 4: Preserve existing activity bar, sidebar, and theme toggle visuals.
    - [x] Step 5: Document non-obvious styling choices with comments where appropriate.
  - **Files**:
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor`: top menu and tabbed section markup
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor.css`: menu and tab chrome styling
    - `src/Workbench/server/WorkbenchHost/Components/_Imports.razor`: additional Radzen namespaces only if required
  - **Work Item Dependencies**: Current shell baseline only.
  - **Run / Verification Instructions**:
    - Run the solution host path used for `WorkbenchHost`
    - Open the `WorkbenchHost` root page
    - Confirm the top menu renders above the shell body
    - Confirm left, upper middle, and right sections show tab strips with close icons
    - Confirm existing sidebars still open and close correctly
  - **User Instructions**: None.
  - **Completion Summary**:
    - Added a full-width placeholder `RadzenMenu` row plus fixed `RadzenTabs` chrome for the left sidebar, upper middle pane, and right sidebar in `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor`.
    - Updated `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor.css` to convert the shell to a two-row grid, style the new menu and tab chrome for light/dark themes, and keep the existing activity bars, sidebar resize handles, and theme toggle intact.
    - No new automated tests were added, per specification.
    - Validation completed with a full workspace build and a full test-project run across `test/**/*.csproj`; all executed tests passed.

### Split Central Workspace
- [x] Work Item 2: Add the lower middle pane with a horizontal splitter and tabbed placeholder content - Completed
  - **Purpose**: Deliver a runnable center-layout slice that introduces the denser workbench composition requested by the specification.
  - **Acceptance Criteria**:
    - The central area is split into upper and lower panes.
    - A visible splitter separates the upper and lower panes.
    - The splitter supports user resizing.
    - The lower middle pane is visible on initial page load.
    - The lower middle pane has its own top-aligned tab strip with two or three tabs.
    - The lower middle pane renders placeholder text content for layout review.
    - Existing left and right sidebars continue to push and resize the center area rather than overlaying it.
  - **Definition of Done**:
    - Code implemented for vertically split central workspace
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for every touched source file
    - Developer-level comments added to all touched files as required by `./.github/instructions/documentation-pass.instructions.md`
    - No new automated tests added, per specification
    - Full solution build completed successfully
    - Full test suite run completed successfully, or any failures are explained as pre-existing and unrelated
    - Can execute end-to-end via `WorkbenchHost` and manually resize the center splitter
  - [x] Task 1: Restructure the center of `Home.razor` into upper and lower panes - Completed
    - [x] Step 1: Introduce a vertical layout for the central workspace that can host upper and lower panes.
    - [x] Step 2: Add a real splitter between the panes, preferring `RadzenSplitter` in line with the specification.
    - [x] Step 3: Ensure the lower pane is visible by default.
    - [x] Step 4: Preserve the existing theme toggle within the upper area unless a clearer placement is needed.
    - [x] Step 5: Document all touched code per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 2: Add lower-pane tabs and placeholder content - Completed
    - [x] Step 1: Add a top-aligned tab strip to the lower middle pane.
    - [x] Step 2: Add two or three fixed tabs with right-aligned close icons.
    - [x] Step 3: Add simple placeholder text content that differs slightly by tab.
    - [x] Step 4: Keep the pane clearly visual and non-functional.
    - [x] Step 5: Document all touched code per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 3: Ensure center-pane layout behaves correctly with open sidebars - Completed
    - [x] Step 1: Verify the center split layout still resizes correctly when the left sidebar opens.
    - [x] Step 2: Verify the center split layout still resizes correctly when the right sidebar opens.
    - [x] Step 3: Verify the center split layout remains stable when both sidebars are open together.
    - [x] Step 4: Preserve desktop-oriented behavior without introducing responsive auto-hide rules.
    - [x] Step 5: Document any non-obvious layout logic per `./.github/instructions/documentation-pass.instructions.md`.
  - **Files**:
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor`: split central workspace markup and lower pane tabs
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor.css`: central pane, splitter, and lower panel styling
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - Run the solution host path used for `WorkbenchHost`
    - Open the `WorkbenchHost` root page
    - Confirm the center area contains upper and lower panes
    - Confirm the lower pane is visible by default
    - Drag the splitter and confirm the panes resize
    - Open left and right sidebars and confirm the center layout still behaves correctly
  - **User Instructions**: None.
  - **Completion Summary**:
    - Reworked `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor` so the center workspace now uses a horizontal `RadzenSplitter` with visible upper and lower panes.
    - Preserved the upper-pane tabs and theme toggle, and added lower-pane `RadzenTabs` with three placeholder tabs and distinct non-functional review text.
    - Updated `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor.css` with splitter sizing, pane fill behavior, lower-pane spacing, and splitter-bar styling so the center layout continues to resize cleanly with open sidebars.
    - Corrected the rendered behavior by switching the splitter to the proper upper/lower orientation and by retargeting the scoped CSS through parent wrappers so sidebar and center tab content remain visible in Blazor CSS isolation.
    - Replaced the sidebar tab implementation with a lightweight local tab strip after the Radzen sidebar tabs proved unreliable in the constrained sidebar layout, restoring visible content for expanded left and right panels while preserving the expected tabbed chrome.
    - Corrected the shell grid so the menu now spans the full window above the workbench content, moved the left and right activity bars below that menu row, and forced the center pane hosts to stretch their rendered tab roots so the upper and lower middle tab strips remain visible.
    - Simplified the middle-pane `RadzenTabs` implementation further by removing the custom middle-tab header templates and letting the built-in Radzen tab headers render directly inside the splitter panes, which restored visible upper and lower tab strips without introducing non-Radzen workarounds.
    - No new automated tests were added, per specification.
    - Validation completed with a full workspace build and a full test-project run across `test/**/*.csproj`; all executed tests passed.

### Upper-Middle Radzen Styling Showcase
- [x] Work Item 3: Populate the upper middle tabs with common Radzen form and data components using sample data - Completed
  - **Purpose**: Deliver the styling-review slice that makes the upper middle pane useful for comparing control appearance across themes.
  - **Acceptance Criteria**:
    - The upper middle pane contains a representative Radzen styling showcase across selected tabs.
    - The showcase uses Radzen-native controls first.
    - The showcase includes common form controls such as text, multiline text, numeric, date, and selection inputs.
    - The showcase includes representative toggle or choice controls.
    - The showcase includes at least one file-oriented control.
    - The showcase includes a sample tree with static hierarchical data.
    - The showcase includes a sample data grid with static row data.
    - The showcase remains fully mock-up oriented and does not call backend services.
    - The page remains visually usable in both light and dark themes.
  - **Definition of Done**:
    - Code implemented for the upper-middle Radzen component showcase
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for every touched source file
    - Developer-level comments added to all touched files as required by `./.github/instructions/documentation-pass.instructions.md`
    - No new automated tests added, per specification
    - Full solution build completed successfully
    - Full test suite run completed successfully, or any failures are explained as pre-existing and unrelated
    - Can execute end-to-end via `WorkbenchHost` and visually inspect the showcase in both themes
  - [x] Task 1: Add in-memory sample view data for the showcase - Completed
    - [x] Step 1: Define the minimal in-memory sample data needed for drop-downs, tree nodes, and grid rows.
    - [x] Step 2: Keep sample data generic, static, and non-sensitive.
    - [x] Step 3: Add any helper models only if they materially improve clarity.
    - [x] Step 4: Document all added types and members per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 2: Add representative Radzen form controls to selected upper-middle tabs - Completed
    - [x] Step 1: Add a form-oriented tab using Radzen-native form primitives such as `RadzenTemplateForm` and `RadzenFormField` where appropriate.
    - [x] Step 2: Include representative inputs such as `RadzenTextBox`, `RadzenTextArea`, `RadzenNumeric`, `RadzenDatePicker`, and a selection control.
    - [x] Step 3: Include representative choice controls such as `RadzenCheckBox`, `RadzenSwitch`, `RadzenRadioButtonList`, or `RadzenSelectBar`.
    - [x] Step 4: Include at least one sample file-oriented control such as `RadzenFileInput` or `RadzenUpload`.
    - [x] Step 5: Add light validation or helper text only where it improves styling review.
    - [x] Step 6: Document all touched code per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 3: Add representative Radzen data-display components to selected upper-middle tabs - Completed
    - [x] Step 1: Add a sample `RadzenTree` using static in-memory hierarchical data.
    - [x] Step 2: Add a sample `RadzenDataGrid<TItem>` using static in-memory rows and explicit columns.
    - [x] Step 3: Keep data-grid behavior simple and visual-first.
    - [x] Step 4: Add any helpful inline placeholder text, badges, alerts, or fieldsets only if they improve visual comparison.
    - [x] Step 5: Document all touched code per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 4: Perform final styling and verification pass for the refinement features - Completed
    - [x] Step 1: Verify the menu, tabs, lower pane, and showcase render coherently in dark theme.
    - [x] Step 2: Verify the same shell in light theme.
    - [x] Step 3: Confirm placeholder interactions do not require backend connectivity.
    - [x] Step 4: Run a full solution build.
    - [x] Step 5: Run the full test suite as required by `./.github/instructions/documentation-pass.instructions.md` and record any unrelated pre-existing failures if present.
  - **Files**:
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor`: upper-middle showcase markup and sample state
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor.css`: showcase layout and component spacing adjustments
    - `src/Workbench/server/WorkbenchHost/Components/Pages/`: helper types only if needed for sample data clarity
  - **Work Item Dependencies**: Work Items 1 and 2.
  - **Run / Verification Instructions**:
    - Run the solution host path used for `WorkbenchHost`
    - Open the `WorkbenchHost` root page
    - Confirm upper-middle tabs include a Radzen styling showcase
    - Confirm the showcase includes sample form controls, a tree, and a data grid
    - Toggle between dark and light themes and compare styling
    - Confirm the shell still behaves correctly with left and right sidebars open
  - **User Instructions**: None.
  - **Completion Summary**:
    - Replaced the upper-middle placeholder tabs in `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor` with three Radzen showcase tabs covering a documented `RadzenTemplateForm`, static `RadzenTree`, and static `RadzenDataGrid<TItem>` while preserving the theme toggle in the upper pane.
    - Added fully documented helper types in `src/Workbench/server/WorkbenchHost/Components/Pages/WorkbenchShowcaseOption.cs`, `WorkbenchShowcaseTreeNode.cs`, `WorkbenchShowcaseGridRow.cs`, and `WorkbenchShowcaseFormModel.cs` so the showcase sample data remains typed, static, generic, and in-memory only.
    - Updated `src/Workbench/server/WorkbenchHost/Components/Pages/Home.razor.css` with theme-aware showcase layout, helper-text, card, and status-badge styling so the new controls remain visually usable in both light and dark themes.
    - No new automated tests were added, per specification.
    - Validation completed with a full workspace build and a full test-project run across the workspace `test/**/*.csproj` projects; the executed test runs completed successfully with no unrelated failures recorded.

## Summary

This plan delivers the new shell refinement features in three runnable vertical slices:

1. add the top application menu and per-section tab chrome
2. add the lower middle pane and resizable central splitter
3. populate the upper middle tabs with Radzen form and data showcase content

Key implementation considerations:

- keep the work centered on the existing Blazor `Home.razor` shell rather than creating a new route
- treat `./.github/instructions/documentation-pass.instructions.md` as a mandatory hard gate for every code-writing task
- preserve the existing sidebar mechanics while layering in the new menu, tabs, and center split
- prefer Radzen-native components for both layout and styling showcase content
- honor the spec’s constraint that this remains a temporary visual mock-up with no new automated tests added
