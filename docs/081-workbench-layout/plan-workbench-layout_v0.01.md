# Implementation Plan

**Target output path:** `docs/081-workbench-layout/plan-workbench-layout_v0.01.md`

**Based on:** `docs/081-workbench-layout/spec-workbench-layout_v0.01.md`

**Mandatory repository standard:** Every code-writing task in this plan must follow `./.github/instructions/documentation-pass.instructions.md` in full. Compliance with that instruction file is a hard Definition of Done gate for each Work Item, alongside the repository coding, testing, and documentation instructions.

## Project Structure and Delivery Strategy

- Keep all implementation for this work inside `src/workbench/server/UKHO.Workbench/` and all automated tests inside `test/workbench/server/UKHO.Workbench.Tests/`.
- Use `src/workbench/server/WorkbenchHost/` only for runnable showcase and verification wiring.
- Keep the splitter API aligned with the existing WPF-style authoring surface in `src/workbench/server/UKHO.Workbench/Layout/`.
- Prefer dedicated splitter definitions such as `SplitterColumnDefinition` and `SplitterRowDefinition` within the existing `ColumnDefinition` / `RowDefinition` naming family.
- Treat `./.github/instructions/documentation-pass.instructions.md` as mandatory for all code changes:
  - every class, including internal and non-public classes, must be commented
  - every method and constructor, including internal and non-public members, must be commented
  - every public method and constructor parameter must be documented
  - every non-obvious property must be documented
  - sufficient inline or block comments must explain purpose, logical flow, and any algorithms used
- Preserve the spec's documentation-only constraints exactly, especially:
  - all production code remains in `UKHO.Workbench.csproj`
  - all tests remain in `UKHO.Workbench.Tests.csproj`
  - `src/workbench/server/UKHO.Workbench/Layout/README.md` must be updated as part of the work
  - completed implementation must not remain under `./scratch`

## Splitter Foundation and First End-to-End Column Slice
- [x] Work Item 1: Deliver the first runnable column-splitter slice through the Workbench layout surface - Completed
  - Summary: Added `SplitterColumnDefinition`, splitter-aware `Grid` and `GridWrapper` plumbing, automatic JS/CSS asset loading, unified column resize notifications, a runnable `WorkbenchHost` showcase page, durable wiki guidance retaining inherited examples plus Work Item 1 splitter documentation, a minimal `Layout/README.md` pointer, and automated validation/default tests in `UKHO.Workbench.Tests`.
  - **Purpose**: Establish the minimum end-to-end capability proving that the existing `Grid` authoring model can render and run a draggable column splitter with automatic asset loading, built-in styling, and unified notifications.
  - **Acceptance Criteria**:
    - A consumer can declare a column splitter using the Workbench layout API with dedicated splitter definitions.
    - A splitter-enabled grid runs end to end in `WorkbenchHost` with automatic asset loading and no manual host-page includes.
    - Default styling is present: transparent at rest, blue on hover, correct resize cursor, `4px` default splitter thickness when width is omitted.
    - Continuous resize notifications flow through a unified direction-annotated notification surface.
    - `./.github/instructions/documentation-pass.instructions.md` is explicitly applied to all written code.
  - **Definition of Done**:
    - Code implemented in `UKHO.Workbench`, with runnable host integration in `WorkbenchHost`
    - Code comments and XML/developer documentation added per `./.github/instructions/documentation-pass.instructions.md`
    - Tests passing in `UKHO.Workbench.Tests`
    - Logging/error handling added for validation and initialization failures
    - `src/workbench/server/UKHO.Workbench/Layout/README.md` updated
    - Can execute end to end via: `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
  - [x] Task 1: Introduce splitter-capable authoring primitives in the layout model - Completed
    - Summary: Added the dedicated splitter authoring component, track metadata model, splitter-aware template generation, fail-fast validation, and documentation-pass comments across the changed layout types.
    - [x] Step 1: Add dedicated splitter definition components in `src/workbench/server/UKHO.Workbench/Layout/` using the existing WPF-style authoring conventions.
      - Summary: Added `SplitterColumnDefinition.razor` and its documented code-behind to register dedicated splitter gutter tracks through the existing `Grid` authoring flow.
    - [x] Step 2: Extend the grid registration flow so splitter definitions participate in track ordering, indexing, and CSS template generation alongside normal rows and columns.
      - Summary: Extended `Grid` and `GridWrapper` to register splitter columns as first-class tracks and emit splitter-aware `grid-template-columns` output.
    - [x] Step 3: Add validation rules so splitters are only valid between two resizable tracks and fail fast when configured against unsupported `Auto` track pairs.
      - Summary: Added fail-fast validation for edge splitters, adjacent splitter tracks, `Auto` participation, and content placement into reserved splitter gutters.
    - [x] Step 4: Apply `./.github/instructions/documentation-pass.instructions.md` to every new or changed class, method, constructor, parameter, and non-obvious property.
      - Summary: Added XML and developer-level documentation across the new and updated layout components, helper types, and host showcase code.
  - [x] Task 2: Render a column splitter end to end with automatic assets and defaults - Completed
    - Summary: Added automatic splitter gutter rendering, Workbench-owned JS/CSS assets, default `4px` thickness, overridable blue hover styling, and continuous unified resize notifications.
    - [x] Step 1: Add the DOM rendering and metadata needed for a column splitter gutter, ensuring splitter tracks remain non-content gutters.
      - Summary: `Grid.razor` now renders dedicated gutter DOM nodes with 1-based track metadata, and `GridElement` fails fast when content targets splitter columns.
    - [x] Step 2: Add Workbench-owned JS/CSS assets under `src/workbench/server/UKHO.Workbench/` for drag behavior and styling with automatic loading.
      - Summary: Added `workbench-grid-splitter.js` and `workbench-grid-splitter.css`, and wired `Grid` to import them automatically for splitter-enabled layouts.
    - [x] Step 3: Implement default thickness fallback to `4px` when width is omitted and default hover styling to blue.
      - Summary: Splitter track registration now defaults omitted widths to `4px`, and the built-in stylesheet supplies the default blue hover state.
    - [x] Step 4: Keep the hover highlight overridable by consumers while preserving blue as the out-of-box default.
      - Summary: The default hover highlight is driven by the `--ukho-workbench-splitter-hover-color` CSS custom property so consumers can override it.
    - [x] Step 5: Ensure continuous drag updates use a unified notification payload with explicit column direction.
      - Summary: Added `GridResizeDirection`, `GridResizeNotification`, and the unified `Grid.OnResize` callback for continuous column drag updates.
  - [x] Task 3: Make the slice runnable and documented - Completed
    - Summary: Added a runnable `WorkbenchHost` showcase page, retained durable documentation in `wiki/Workbench-Layout.md`, reduced `Layout/README.md` to a pointer, added splitter-focused automated tests, and verified build/test/run startup.
    - [x] Step 1: Add or update a `WorkbenchHost` page to demonstrate a minimal splitter-enabled column layout.
      - Summary: Added `LayoutSplitterShowcase.razor` with a fixed-plus-star splitter demo and live notification output at `/layout/splitter-column`.
    - [x] Step 2: Document usage, defaults, and limitations in `src/workbench/server/UKHO.Workbench/Layout/README.md`.
      - Summary: Kept `Layout/README.md` as a minimal pointer and moved the retained inherited examples plus Work Item 1 splitter guidance into `wiki/Workbench-Layout.md`.
    - [x] Step 3: Add automated tests in `test/workbench/server/UKHO.Workbench.Tests/` for rendering, defaults, and fail-fast behavior.
      - Summary: Added `GridSplitterLayoutShould` coverage for default thickness, metadata ordering, invalid edges, invalid `Auto` pairs, and reserved gutter protection.
    - [x] Step 4: Verify the end-to-end host path manually in the browser.
      - Summary: Verified the host startup path with `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj --no-build` and confirmed the splitter showcase host starts on the local development URLs; browser interaction still requires local user verification.
  - **Files**:
    - `src/workbench/server/UKHO.Workbench/Layout/Grid.razor`: Register and render splitter-aware track metadata.
    - `src/workbench/server/UKHO.Workbench/Layout/GridWrapper.cs`: Generate splitter-aware CSS templates and defaults.
    - `src/workbench/server/UKHO.Workbench/Layout/SplitterColumnDefinition.razor`: New dedicated column splitter authoring primitive.
    - `src/workbench/server/UKHO.Workbench/Layout/GridResizeDirection.cs`: Direction metadata for unified notifications.
    - `src/workbench/server/UKHO.Workbench/Layout/GridResizeNotification.cs`: Unified resize payload contract.
    - `src/workbench/server/UKHO.Workbench/wwwroot/*`: Workbench-owned splitter JS/CSS assets and default styling.
    - `src/workbench/server/WorkbenchHost/Components/Pages/LayoutSplitterShowcase.razor`: Runnable showcase integration for the first column-splitter slice.
    - `src/workbench/server/UKHO.Workbench/Layout/README.md`: Splitter authoring and usage guidance.
    - `test/workbench/server/UKHO.Workbench.Tests/Layout/*`: Rendering, validation, and default-behavior tests.
  - **Work Item Dependencies**: None. This is the first runnable vertical slice.
  - **Run / Verification Instructions**:
    - `dotnet build src/workbench/server/UKHO.Workbench/UKHO.Workbench.csproj`
    - `dotnet test test/workbench/server/UKHO.Workbench.Tests/UKHO.Workbench.Tests.csproj`
    - `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - Navigate to the Workbench showcase page and verify a draggable column splitter with default styling and live resize updates.
  - **User Instructions**: None expected beyond running the host locally.

## Row Splitters and Two-Dimensional Layout Slice
- [x] Work Item 2: Extend the vertical slice to row splitters and combined row/column layouts - Completed
  - Summary: Added `SplitterRowDefinition`, row-splitter-aware validation and rendering in `Grid` and `GridWrapper`, unified row notifications through the existing `Grid.OnResize` payload, a runnable showcase page covering column-only, row-only, and mixed layouts, updated layout/wiki guidance, and automated row/mixed-direction tests in `UKHO.Workbench.Tests`.
  - **Purpose**: Prove that the same Workbench-native pattern supports row splitters, mixed directions in one grid, and the shared unified notification surface.
  - **Acceptance Criteria**:
    - Consumers can declare row splitters with the same dedicated-definition model.
    - A single grid can combine both row and column splitters.
    - Unified notifications include row/column direction for both kinds of splitter activity.
    - Default thickness, cursor rules, and non-content gutter behavior remain consistent for rows and columns.
  - **Definition of Done**:
    - Code implemented and commented per `./.github/instructions/documentation-pass.instructions.md`
    - Tests passing in `UKHO.Workbench.Tests`
    - Showcase updated so both row and column splitter scenarios are demonstrable
    - Documentation updated with row-specific authoring examples
    - Can execute end to end via: `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
  - [x] Task 1: Add row splitter registration and rendering - Completed
    - Summary: Added `SplitterRowDefinition`, extended row splitter rendering and template generation, preserved 1-based indexing in mixed layouts, and applied documentation-pass comments across the changed row-splitter code.
    - [x] Step 1: Add a dedicated `SplitterRowDefinition` authoring primitive.
      - Summary: Added `SplitterRowDefinition.razor` and `SplitterRowDefinition.razor.cs` as dedicated row gutter authoring components beside the existing layout definitions.
    - [x] Step 2: Extend rendering and CSS generation for row splitter gutters, including default height fallback to `4px` when omitted.
      - Summary: Extended `Grid`, `GridWrapper`, and the Workbench-owned splitter assets so row gutters render correctly and default omitted heights to `4px`.
    - [x] Step 3: Preserve 1-based developer-facing indexing across mixed row/column splitter layouts.
      - Summary: Preserved authored splitter ordering in both directions and enforced reserved gutter placement rules using the same 1-based numbering model as the existing grid API.
    - [x] Step 4: Apply the full documentation-pass instruction set to all changed code.
      - Summary: Added XML and developer comments across the updated layout types, JS interop, showcase code, and tests for the row splitter slice.
  - [x] Task 2: Support unified row/column notifications and runtime behavior - Completed
    - Summary: Reused the existing unified resize notification surface for row drags, kept continuous updates via request-animation-frame throttling, and retained the optional lifecycle-notification model as unimplemented but documented.
    - [x] Step 1: Ensure the same notification surface carries row and column resize activity with explicit direction markers.
      - Summary: Added `NotifyRowResize` so `Grid.OnResize` now carries both row and column activity with explicit `GridResizeDirection` values.
    - [x] Step 2: Verify continuous resize notifications for row drag behavior, with optional completion notifications where implemented.
      - Summary: Extended the Workbench-owned splitter script so row drags use the same continuous throttled update path as column drags.
    - [x] Step 3: Keep drag-start and drag-end notifications optional, not required.
      - Summary: Kept the runtime contract focused on continuous resize notifications and documented the payload scope without adding mandatory drag-start or drag-end callbacks.
  - [x] Task 3: Demonstrate combined two-dimensional layouts - Completed
    - Summary: Expanded the runnable host page to cover row-only and mixed-direction layouts, documented the two-dimensional authoring model, and added automated row/mixed tests in `UKHO.Workbench.Tests`.
    - [x] Step 1: Update the showcase with a layout that mixes row and column splitters in one grid.
      - Summary: Expanded `LayoutSplitterShowcase.razor` with row-only and mixed row/column examples on the existing Workbench splitter showcase route.
    - [x] Step 2: Document the two-dimensional authoring model in `Layout/README.md`.
      - Summary: Updated the `Layout/README.md` pointer and the durable `wiki/Workbench-Layout.md` guidance with row and mixed-direction examples, defaults, and constraints.
    - [x] Step 3: Add test coverage for row-only and mixed row/column splitter scenarios.
      - Summary: Extended `GridSplitterLayoutShould` with row-specific default, validation, and mixed-direction coverage.
  - **Files**:
    - `src/workbench/server/UKHO.Workbench/Layout/SplitterRowDefinition.razor`: New dedicated row splitter primitive.
    - `src/workbench/server/UKHO.Workbench/Layout/Grid.razor`: Mixed-direction rendering and notification hookup.
    - `src/workbench/server/UKHO.Workbench/Layout/GridWrapper.cs`: Row splitter track generation.
    - `src/workbench/server/WorkbenchHost/Components/Pages/LayoutSplitterShowcase.razor`: Mixed row/column showcase updates.
    - `src/workbench/server/UKHO.Workbench/Layout/README.md`: Row and mixed-direction examples.
    - `test/workbench/server/UKHO.Workbench.Tests/Layout/*`: Row and combined-direction tests.
  - **Work Item Dependencies**: Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet build src/workbench/server/UKHO.Workbench/UKHO.Workbench.csproj`
    - `dotnet test test/workbench/server/UKHO.Workbench.Tests/UKHO.Workbench.Tests.csproj`
    - `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - Verify row-only and mixed row/column splitter demonstrations behave interactively.
  - **User Instructions**: None expected beyond running the host locally.

## Validation, Constraints, and Advanced Composition Slice
- [x] Work Item 3: Deliver authoring safeguards, complex layout support, and documentation-complete behaviour - Completed
  - Summary: Completed the advanced splitter slice by validating multiple-splitter and gap scenarios through tests, documenting 1-based diagnostics and adjacent-track notification payload scope, fixing nested-grid gutter registration, expanding the showcase with a nested multi-splitter example, and updating the durable layout guidance for final supported patterns and constraints.
  - **Purpose**: Complete the feature set by locking down invalid configurations, supporting complex layouts, and documenting all agreed rules so the feature is implementation-ready for future WorkbenchHost adoption.
  - **Acceptance Criteria**:
    - Invalid configurations fail fast, including `Auto` resize pairs and edge splitter definitions.
    - Mixed supported pairs such as `fixed + star`, multiple splitters, nested grids, and gap coexistence are supported.
    - Notifications/diagnostics include affected track information with 1-based numbering.
    - Documentation clearly records any implementation-defined areas, including payload scope and optional lifecycle notification model.
  - **Definition of Done**:
    - Code and comments updated per `./.github/instructions/documentation-pass.instructions.md`
    - Complex-layout and validation tests passing in `UKHO.Workbench.Tests`
    - Showcase demonstrates at least one nested or multi-splitter layout
    - `Layout/README.md` updated with supported patterns and fail-fast constraints
    - Can execute end to end via: `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
  - [x] Task 1: Implement fail-fast validation and diagnostics - Completed
    - Summary: Confirmed and documented the fail-fast edge and `Auto` validation rules already enforced by the layout model, and added explicit tests and final guidance for 1-based diagnostics and adjacent-track notification payload scope.
    - [x] Step 1: Reject splitter definitions at outer grid edges.
      - Summary: Retained and re-verified the fail-fast edge validation for row and column splitters through the existing and final validation tests.
    - [x] Step 2: Reject resize participation for unsupported `Auto` track pairs.
      - Summary: Retained and re-verified the fail-fast `Auto` track validation for both directions through the final validation coverage.
    - [x] Step 3: Ensure diagnostics and notifications identify affected tracks using 1-based numbering.
      - Summary: Added final payload tests and documentation confirming that splitter, previous-track, and next-track identifiers remain 1-based.
    - [x] Step 4: Document any implementation-defined conventions, including notification payload scope.
      - Summary: Expanded the durable layout guidance to document that notifications report the two content tracks immediately adjacent to the active splitter plus the resolved template string for the affected axis.
  - [x] Task 2: Complete complex layout support - Completed
    - Summary: Verified and showcased multiple splitters, fixed-plus-star adjacent pairs, additive gaps, and nested splitter-enabled grids, and hardened nested gutter registration so inner grids remain independent from outer grid interop.
    - [x] Step 1: Support multiple splitters in the same grid.
      - Summary: Added automated coverage and a runnable showcase example proving that a single grid can host multiple splitter tracks in the same direction.
    - [x] Step 2: Support mixed `fixed + star` adjacent resize pairs.
      - Summary: Preserved the existing fixed-plus-star splitter support and carried it through the expanded advanced and mixed-direction showcase examples.
    - [x] Step 3: Ensure splitters coexist with `RowGap` and `ColumnGap`, where splitter size is additional to configured gaps.
      - Summary: Added final tests and showcase examples verifying that `RowGap` and `ColumnGap` remain additive to splitter gutter thickness.
    - [x] Step 4: Support nested splitter-enabled grids without regressing the base layout model.
      - Summary: Restricted splitter registration to direct gutter children so nested grids register only their own gutters, then added automated metadata coverage and a runnable nested showcase example.
  - [x] Task 3: Finalize documentation and verification - Completed
    - Summary: Expanded the final layout guidance, added the remaining automated tests for advanced scenarios and diagnostics, and re-verified build, targeted tests, and host startup for the complete splitter slice.
    - [x] Step 1: Expand `Layout/README.md` with supported patterns, constraints, defaults, and customization guidance.
      - Summary: Updated the `Layout/README.md` pointer and expanded `wiki/Workbench-Layout.md` with final advanced patterns, constraints, notification conventions, and showcase guidance.
    - [x] Step 2: Add final automated tests covering validation, nested grids, gap behavior, and diagnostics.
      - Summary: Added final tests for multiple splitters, additive gaps, nested-grid metadata independence, and 1-based notification payload shape.
    - [x] Step 3: Perform a final host verification pass over all implemented showcase scenarios.
      - Summary: Re-verified the host startup path with `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj --no-build`; browser interaction across the advanced scenarios still requires local user verification.
  - **Files**:
    - `src/workbench/server/UKHO.Workbench/Layout/*`: Validation, diagnostics, and advanced composition logic.
    - `src/workbench/server/UKHO.Workbench/Layout/README.md`: Final usage and constraint documentation.
    - `src/workbench/server/WorkbenchHost/Components/Pages/LayoutSplitterShowcase.razor`: Advanced/nested showcase scenarios.
    - `test/workbench/server/UKHO.Workbench.Tests/Layout/*`: Complex-layout and validation coverage.
  - **Work Item Dependencies**: Depends on Work Items 1 and 2.
  - **Run / Verification Instructions**:
    - `dotnet build src/workbench/server/UKHO.Workbench/UKHO.Workbench.csproj`
    - `dotnet test test/workbench/server/UKHO.Workbench.Tests/UKHO.Workbench.Tests.csproj`
    - `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - Verify nested, multi-splitter, mixed-size, and invalid-authoring scenarios.
  - **User Instructions**: None expected beyond running the host locally.

## Architecture

## Overall Technical Approach
- Extend the existing WPF-style Blazor layout surface in `src/workbench/server/UKHO.Workbench/Layout/` rather than introducing a new top-level control family.
- Keep the public authoring model centered on `Grid`, `ColumnDefinition`, `RowDefinition`, and new dedicated splitter definitions.
- Use a Workbench-owned client-side splitter layer for CSS Grid drag behavior, shipped as automatic static assets from `UKHO.Workbench`.
- Keep all production implementation in `UKHO.Workbench` and all automated tests in `UKHO.Workbench.Tests`, as required by the spec.
- Treat `./.github/instructions/documentation-pass.instructions.md` as a hard gate for all code-writing work.

```mermaid
flowchart LR
    Author[Blazor consumer markup] --> Grid[Workbench Grid component]
    Grid --> LayoutModel[Track registration and validation]
    LayoutModel --> RenderedGrid[Rendered CSS Grid with splitter gutters]
    RenderedGrid --> SplitterJs[Workbench splitter JS/CSS assets]
    SplitterJs --> Notifications[Unified resize notifications]
    Notifications --> HostDemo[WorkbenchHost showcase]
    LayoutModel --> Tests[UKHO.Workbench.Tests]
```

## Frontend
- Primary frontend surface is the Blazor component model under `src/workbench/server/UKHO.Workbench/Layout/`.
- `Grid.razor`, `ColumnDefinition.razor`, `RowDefinition.razor`, and `GridElement.razor` remain the core authoring surface.
- New splitter-specific authoring components should live beside the existing layout components to preserve discoverability.
- `WorkbenchHost` should provide a runnable showcase page demonstrating:
  - minimal column splitter layout
  - row splitter layout
  - combined row/column layout
  - advanced nested or multi-splitter layout
- Styling remains close to built-in defaults, with blue hover highlighting as the default and consumer override supported.

## Backend
- No separate backend/API/service slice is required for this work package.
- Runtime behaviour is client-assisted through Blazor + JS interop/static assets, while validation, rendering, authoring rules, and notification contracts remain in the Workbench component layer.
- The effective data flow is:
  - declarative Blazor markup
  - in-process grid/track validation and CSS template generation
  - client-side drag handling
  - callback/event notification back into the Blazor component model
  - host showcase consumption and automated test verification

## Summary
This plan keeps the work feature-centric and runnable at each stage: first a minimal column splitter slice, then row and mixed-direction support, then validation and advanced composition. The main implementation risks are preserving the existing WPF-style API feel, keeping all automated tests inside `UKHO.Workbench.Tests`, and enforcing the mandatory repository documentation-pass standard on every code-writing task.