# Implementation Plan

**Target output path:** `docs/085-workbench-style/implementation-plan.md`

**Source specification:** `docs/085-workbench-style/spec-workbench-style_v0.01.md`

## Delivery rules for all Work Items

- `./.github/instructions/documentation-pass.instructions.md` is a mandatory repository standard for this work package and a hard Definition of Done gate for every code-writing task in this plan.
- Every code-writing task in this plan must implement `./.github/instructions/documentation-pass.instructions.md` in full, including developer-level comments on every class, method, and constructor touched, including internal and other non-public types; parameter documentation for every public method and constructor parameter; comments on every property whose meaning is not obvious from its name; and sufficient inline or block comments so the purpose, logical flow, and any non-obvious algorithms remain understandable to another developer.
- The Workbench shell must remain desktop-like, stay close to the stock Radzen Material theme, and continue to use the existing `UKHO.Workbench.Layout` grid and splitter primitives rather than introducing docking-style behavior or module-specific CSS workarounds.
- Shell sizing and spacing changes must be implemented in `WorkbenchHost` and shared Workbench shell state where needed so hosted module UIs remain unaware of layout mechanics.
- The menu bar must continue to span the full window, and any existing upper or lower center tab strips must remain visibly rendered after the refinement.
- Validation for this work package should stay focused on the Workbench projects and tests changed by the slice rather than the full repository suite.

## Shell Chrome Simplification Slice

- [x] Work Item 1: Deliver a runnable edge-to-edge Workbench shell with simplified toolbar chrome - Completed
  - Summary: Removed the outer Workbench shell padding, renamed the host-owned Overview action to Home, replaced the toolbar Active tab heading with a leading Home action, converted the activity rail to an icon-only tooltip-backed strip, updated focused host rendering tests, and refreshed the Workbench shell wiki.
  - **Purpose**: Make the existing Workbench immediately feel more desktop-like by removing outer padding, simplifying the toolbar, and converting the far-left pane into a compact icon rail without changing underlying module behavior.
  - **Acceptance Criteria**:
    - The Workbench shell renders flush with the browser viewport without decorative outer padding.
    - The toolbar no longer shows `ACTIVE TAB`.
    - The action previously labeled `Overview` is visible as `Home` in the vacated toolbar position.
    - The far-left rail shows icons without always-visible text labels.
    - Far-left rail items expose their names through tooltips.
  - **Definition of Done**:
    - Code implemented for shell chrome spacing, toolbar relabeling, and far-left rail presentation
    - Targeted tests passing for layout rendering and toolbar/rail behavior
    - Logging and error handling preserved where shell UI orchestration already uses them
    - Documentation updated
    - `./.github/instructions/documentation-pass.instructions.md` fully applied and treated as a hard gate
    - Can execute end-to-end via: `dotnet run --project src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
  - [x] Task 1: Remove outer shell padding in `WorkbenchHost` - Completed
    - Summary: Removed the shell-level outer padding in `MainLayout.razor.css`, kept `box-sizing: border-box`, and left hosted module views unchanged so the edge-to-edge behavior remains shell-owned.
    - [x] Step 1: Inspect `MainLayout.razor`, `MainLayout.razor.css`, `Index.razor`, and `Index.razor.css` to identify where full-screen Workbench padding is applied.
    - [x] Step 2: Remove shell-level decorative padding so the Workbench surface sits flush with the viewport while preserving required box sizing and full-window layout behavior.
    - [x] Step 3: Confirm the change is implemented in the shell rather than by compensating inside hosted module views.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - [x] Task 2: Simplify the toolbar and rename `Overview` to `Home` - Completed
    - Summary: Replaced the leading Active tab toolbar block with a persistent Home action, renamed the host-owned Overview menu and toolbar contributions to Home, and kept the toolbar aligned across the full shell width.
    - [x] Step 1: Update the toolbar markup and any backing code in `MainLayout.razor` and `MainLayout.razor.cs` so the `ACTIVE TAB` label is removed.
    - [x] Step 2: Move the current `Overview` action into the vacated toolbar position and rename it to `Home` without changing its current navigation intent.
    - [x] Step 3: Verify the toolbar still spans the full window and remains visually aligned with the refined shell layout.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - [x] Task 3: Convert the far-left pane into an icon-only rail with tooltips - Completed
    - Summary: Removed persistent activity-rail text, added accessible `aria-label` values, and reused the shared Radzen tooltip service for hover and focus interactions on rail buttons.
    - [x] Step 1: Update the far-left pane rendering in `MainLayout.razor` and related code-behind so item text is no longer rendered persistently in the rail.
    - [x] Step 2: Add or reuse the existing Radzen tooltip approach so each far-left rail item exposes its name on hover or focus.
    - [x] Step 3: Keep the presentation compact and desktop-like without introducing module-specific label logic.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - [x] Task 4: Add focused verification for the shell chrome slice - Completed
    - Summary: Extended `MainLayoutRenderingTests` to assert the new Home action, the absence of the Active tab label, activity-rail accessibility markers, and tooltip-backed rail labels; then re-ran the targeted Workbench host tests and build.
    - [x] Step 1: Update `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs` and related host tests to confirm `Home` is present, `ACTIVE TAB` is absent, and persistent far-left rail labels are removed.
    - [x] Step 2: Add focused tests that confirm tooltip-backed naming is wired for far-left rail items.
    - [x] Step 3: Verify the shell still renders edge-to-edge without regressing the existing desktop-like layout expectations.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - **Files**:
    - `src/Workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`: update toolbar and far-left rail markup.
    - `src/Workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.cs`: coordinate renamed action and tooltip-backed rail behavior if required.
    - `src/Workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`: remove shell padding and refine toolbar and rail presentation.
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Index.razor`: adjust shell host wrappers only if they contribute outer spacing.
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Index.razor.css`: remove page-level spacing only if it participates in the outer shell padding.
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: verify toolbar and rail rendering.
    - `test/workbench/server/WorkbenchHost.Tests/IndexRenderingTests.cs`: verify edge-to-edge host rendering if page-level wrappers participate.
  - **Work Item Dependencies**: Depends on the already-delivered Workbench shell foundation and may be implemented independently of later sizing refinements.
  - **Run / Verification Instructions**:
    - `dotnet build src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj`
    - `dotnet run --project src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - Launch the Workbench and confirm the shell is flush with the browser edges, the toolbar shows `Home` instead of `Overview`, `ACTIVE TAB` is absent, and hovering the far-left rail icons reveals their names.
  - **User Instructions**: Use the current local Workbench startup path and module configuration already required by `WorkbenchHost`.

## Layout Proportion and Fixed-Rail Slice

- [x] Work Item 2: Deliver a runnable startup layout with a fixed left rail and centre-weighted pane proportions - Completed
  - Summary: Locked the activity rail to a fixed `64px` width, preserved only the explorer-centre resize boundary, extended focused layout and host rendering coverage for the startup `1fr : 4fr` proportions, and revalidated the Workbench shell documentation and startup behaviour.
  - **Purpose**: Make the Workbench startup layout match the intended desktop-like balance by fixing the far-left rail width and ensuring the explorer starts much narrower than the centre pane.
  - **Acceptance Criteria**:
    - The far-left rail renders at a fixed width of approximately `64px`.
    - The far-left rail does not expose user resize behavior.
    - The explorer and centre panes start with an approximately `1* : 4*` width ratio, excluding the fixed rail.
    - The explorer is visibly narrower than the centre pane on initial load.
    - Any remaining resize affordance applies only between the explorer and centre panes.
  - **Definition of Done**:
    - Code implemented for fixed rail width and startup pane ratios
    - Targeted tests passing for shell layout state and rendered splitter behavior
    - Logging and error handling preserved where layout initialization already uses them
    - Documentation updated
    - `./.github/instructions/documentation-pass.instructions.md` fully applied and treated as a hard gate
    - Can execute end-to-end via: `dotnet run --project src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
  - [x] Task 1: Refine Workbench shell layout state for the new startup proportions - Completed
    - Summary: Confirmed the Workbench layout library already preserves proportional star sizing, added a regression test for `1fr` and `4fr` output, and updated `MainLayout.razor` so the explorer and centre panes now start with `*` and `4*` widths while retaining the fixed `64px` rail.
    - [x] Step 1: Inspect `UKHO.Workbench.Layout` and shell-state types to identify where the current startup grid and splitter tracks are defined.
    - [x] Step 2: Update the default layout model so the far-left rail is represented as a fixed-width track of approximately `64px` and the explorer-to-centre split starts at approximately `1* : 4*`.
    - [x] Step 3: Keep the change bounded to shell-level startup defaults and avoid redesigning persistence or unrelated docking semantics.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - [x] Task 2: Remove resize behavior from the far-left rail while preserving the explorer-centre splitter - Completed
    - Summary: Removed the splitter between the fixed activity rail and the explorer in `MainLayout.razor`, shifted the explorer and centre pane grid placements to the new four-column layout, updated the proportional layout regression to `64px 1fr 4px 4fr`, and refreshed the Workbench shell wiki to record that only the explorer-centre boundary remains resizeable.
    - [x] Step 1: Update `MainLayout.razor`, layout helpers, and any splitter wiring so the far-left rail no longer exposes a resize handle.
    - [x] Step 2: Preserve or adjust the explorer-centre splitter so only that boundary remains resizeable.
    - [x] Step 3: Confirm the change is implemented through the intended grid and splitter primitives rather than CSS-only workarounds.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - [x] Task 3: Verify fixed-width rail and startup proportions end to end - Completed
    - Summary: Extended the layout regression coverage for the fixed `64px 1fr 4px 4fr` startup grid, updated host rendering checks so only the explorer-centre boundary remains resizeable, and completed targeted manual and automated verification for the refined Workbench startup layout.
    - [x] Step 1: Extend `test/workbench/server/UKHO.Workbench.Tests/Layout/GridSplitterLayoutShould.cs` or related layout tests to verify the fixed rail and the initial explorer-centre ratio.
    - [x] Step 2: Add or update `WorkbenchHost.Tests` coverage to confirm the rendered layout exposes only the intended resize boundary.
    - [x] Step 3: Verify manual Workbench startup shows the explorer narrower than the centre pane while the far-left rail remains constant width.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - **Files**:
    - `src/Workbench/server/UKHO.Workbench/Layout/GridTrackDefinition.cs`: adjust track-definition support only if needed for the fixed rail expression.
    - `src/Workbench/server/UKHO.Workbench/Layout/GridWrapper.cs`: update startup grid composition if layout state is built here.
    - `src/Workbench/server/UKHO.Workbench/Layout/Grid.razor`: adjust rendered splitter boundaries only if needed.
    - `src/Workbench/server/UKHO.Workbench/wwwroot/workbench-grid-splitter.js`: update splitter behavior only if the fixed rail requires client-side enforcement.
    - `src/Workbench/server/UKHO.Workbench/WorkbenchShell/WorkbenchShellState.cs`: store or derive the revised default shell layout only if this is where startup state is owned.
    - `src/Workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`: compose the fixed rail and explorer-centre split.
    - `src/Workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`: apply minimal styling required to support the fixed rail presentation.
    - `test/workbench/server/UKHO.Workbench.Tests/Layout/GridSplitterLayoutShould.cs`: verify layout math and fixed splitter boundaries.
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: verify rendered shell boundaries and startup proportions.
  - **Work Item Dependencies**: Depends on Work Item 1 for the compact icon-rail presentation, but the underlying startup-ratio logic can be developed in parallel before the visual polish is merged.
  - **Run / Verification Instructions**:
    - `dotnet build src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - `dotnet test test/workbench/server/UKHO.Workbench.Tests/UKHO.Workbench.Tests.csproj`
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj`
    - `dotnet run --project src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - Launch the Workbench and confirm the far-left rail remains approximately `64px` wide, cannot be resized, and the explorer starts much narrower than the centre pane while the explorer-centre split remains usable.
  - **User Instructions**: If the current shell persists layout state locally, clear only the relevant local shell state before manual verification so the new default startup proportions can be observed cleanly.

## Centre Tab Host Spacing Slice

- [x] Work Item 3: Deliver a runnable centre tab host that is flush on the top, bottom, and left while preserving right-anchored overflow - Completed
  - Summary: Removed the extra shell-owned tab-host wrapper, tightened the centre tab strip so it now renders flush on the top, bottom, and left edges, preserved the right-anchored always-visible overflow affordance, extended focused host rendering coverage, refreshed the Workbench shell wiki, and revalidated the Workbench host build, targeted tests, and startup command.
  - **Purpose**: Tighten the visual fit of the center document surface so tabs align cleanly with the content region without breaking the overflow behavior introduced by the prior tabs work package.
  - **Acceptance Criteria**:
    - The centre tab host removes extra top padding.
    - The centre tab host removes extra bottom padding.
    - The centre tab host removes extra left padding.
    - The tab overflow affordance remains anchored to the right side of the tab strip.
    - Existing tab activation, close, overflow list, and overflow selection behavior remain unchanged.
  - **Definition of Done**:
    - Code implemented for centre tab host spacing refinement
    - Targeted tests passing for tab-strip rendering and overflow placement
    - Logging and error handling preserved where existing shell interactions already use them
    - Documentation updated
    - `./.github/instructions/documentation-pass.instructions.md` fully applied and treated as a hard gate
    - Can execute end-to-end via: `dotnet run --project src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
  - [x] Task 1: Remove shell-owned padding around the centre tab control - Completed
    - Summary: Inspected the shell host and page wrappers, confirmed the spacing remained shell-owned, removed the intermediate `workbench-shell__tool-area` wrapper from `MainLayout.razor`, and updated the tab-strip CSS so the centre host now removes the extra top, bottom, and left padding while keeping only the minimal right-side spacing needed for overflow.
    - [x] Step 1: Inspect `MainLayout.razor`, `MainLayout.razor.css`, `Index.razor`, and `Index.razor.css` to identify which shell-owned wrappers add the current top, bottom, and left spacing around the centre tab host.
    - [x] Step 2: Remove the unnecessary padding from the centre tab host while preserving any minimal right-side space strictly required for the overflow affordance.
    - [x] Step 3: Keep the change localized to the Workbench shell and avoid modifying module content components.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - [x] Task 2: Preserve right-anchored overflow behavior in the refined tab strip - Completed
    - Summary: Reviewed the existing overflow dropdown composition from the prior tab slice, retained the shared activation and selection paths unchanged, and adjusted only the shell tab-strip layout metadata and CSS so the overflow affordance continues to render at the right edge after the spacing refinement.
    - [x] Step 1: Review the existing overflow markup and CSS added for `084-workbench-tabs` so the overflow affordance stays anchored to the right edge after spacing removal.
    - [x] Step 2: Adjust tab-strip layout classes or wrappers only as much as needed to preserve overflow alignment and existing interaction semantics.
    - [x] Step 3: Confirm that no change is made to tab lifecycle, overflow list content, or selection rules beyond visual alignment.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - [x] Task 3: Add focused verification for centre tab host spacing - Completed
    - Summary: Extended `MainLayoutRenderingTests` to assert the flush tab-host metadata, the removed shell wrapper, and the right-anchored overflow marker; then re-ran the targeted `WorkbenchHost.Tests` suite, rebuilt the Workbench host, and launched the host startup command for end-to-end verification readiness.
    - [x] Step 1: Extend `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs` and related host tests to confirm the centre tab host no longer renders the removed padding wrappers or classes.
    - [x] Step 2: Add focused verification that the overflow affordance remains present and right-aligned after the spacing change.
    - [x] Step 3: Run manual Workbench verification with enough tabs to expose overflow and confirm that tab activation and selection still behave exactly as before.
    - [x] Step 4: Implement all new and updated code in full compliance with `./.github/instructions/documentation-pass.instructions.md`, including mandatory comments on every class, method, constructor, parameter, and non-obvious property touched by this task.
  - **Files**:
    - `src/Workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`: refine tab-strip host wrappers if needed.
    - `src/Workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`: remove centre host padding and preserve overflow placement.
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Index.razor`: adjust only if page-level wrappers still contribute centre host padding.
    - `src/Workbench/server/WorkbenchHost/Components/Pages/Index.razor.css`: refine page-level spacing only if it still affects the center tab host.
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: verify flush center host and overflow presence.
    - `test/workbench/server/WorkbenchHost.Tests/IndexRenderingTests.cs`: verify page-level wrapper behavior if needed.
    - `wiki/Workbench-Shell.md`: update Workbench shell guidance if screenshots or textual descriptions of the chrome and tab host need alignment with delivered behavior.
  - **Work Item Dependencies**: Depends on Work Items 1 and 2 and must preserve the tab-strip behavior delivered by `084-workbench-tabs`.
  - **Run / Verification Instructions**:
    - `dotnet build src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj`
    - `dotnet run --project src/Workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - Launch the Workbench, open enough tabs to show overflow, and confirm the tab strip now sits flush on the top, bottom, and left while the overflow control remains anchored to the right and continues to activate tabs correctly.
  - **User Instructions**: Use the existing module explorer entries to open multiple tabs during manual verification; no additional setup should be required.

## Overall approach summary

This plan delivers `085-workbench-style` as three focused vertical slices. The first slice removes outer padding, simplifies the toolbar, and converts the far-left pane into a tooltip-backed icon rail. The second slice fixes the far-left rail width and resets startup proportions so the explorer begins much narrower than the centre pane while preserving the intended grid and splitter model. The third slice tightens the centre tab host so it renders flush on the top, bottom, and left without regressing the right-anchored overflow behavior from `084-workbench-tabs`. Across every slice, the implementation must stay shell-owned, preserve the desktop-like Workbench direction, remain close to the stock Radzen Material theme, and treat `./.github/instructions/documentation-pass.instructions.md` as a non-negotiable Definition of Done requirement.