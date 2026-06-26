# Workbench Toolbars Specification

- Version: `v0.01`
- Status: `Draft`
- Work Package: `088-workbench-toolbars`
- Created: `2026-04-01`
- Related work: `dev/work-packages/mvp/080-workbench-initial/`, `dev/work-packages/mvp/086-workbench-output/`, `dev/work-packages/mvp/087-workbench-output-trimming/`

## Change Log

- `v0.01` - Initial specification for reintroducing the menu bar, removing the shell-wide top toolbar, adding explorer-scoped toolbar contributions, and moving active-tool toolbar actions into the active tab view.
- `v0.01` - Clarified that the explorer toolbar is a mixed surface containing both shell-global left-pane actions and active-explorer contributions.
- `v0.01` - Clarified that `WorkbenchShellRegion.ActiveToolToolbar` should be deleted entirely rather than repurposed for the in-tab toolbar.
- `v0.01` - Clarified that the menu bar should always remain visible through a host-provided minimum shell menu presence including at least `Help`, `Edit`, and `View` menus, with detailed contents deferred to a future work package.

## 1. Overview

### 1.1 Purpose

Define the next refinement of the Workbench shell chrome so command placement better matches user focus. The specification makes the active tool toolbar part of the active tab experience, introduces a dedicated explorer-toolbar contribution surface for left-pane and workspace-global actions, and removes the current mixed-purpose top toolbar row.

### 1.2 Scope

This work package covers the Workbench shell layout in `WorkbenchHost`, including:

- making the menu bar visible again
- placing the theme toggle back in the menu bar as a temporary right-aligned shell action
- removing the current full-width top toolbar row entirely rather than merely hiding it
- introducing `ExplorerToolbarContributions` as a dedicated contribution surface
- rendering `ActiveToolToolbarContributions` inside the active centre-pane tab view
- updating shell layout, region model, and contribution composition expectations to support the new toolbar structure

This work package does not cover:

- the future redesign or richer rendering model of the menu bar
- moving the theme toggle to its eventual longer-term destination outside the menu bar
- changes to the output-panel toolbar
- changes to Workbench module functionality beyond where their commands render
- a broader redesign of the activity rail, explorer item list, or tab strip behavior

### 1.3 Stakeholders

- Workbench users who need a cleaner and more focused shell layout
- Developers working on `WorkbenchHost` shell layout and rendering
- Maintainers of `UKHO.Workbench` and `UKHO.Workbench.Services`
- Module authors publishing menu, explorer, and active-tool commands into the shell

### 1.4 Definitions

- **Menu bar**: The full-width shell bar at the top of the Workbench that hosts shell and menu-level commands.
- **Explorer toolbar**: A dedicated action surface at the top of the explorer pane for explorer-scoped or workspace-global commands.
- **Active tool toolbar**: The toolbar for commands owned by the currently active tool or tab.
- **Top toolbar row**: The current full-width toolbar rendered below the menu bar area and above the working surface.
- **Tab view**: The active centre-pane content area associated with the selected Workbench tab.
- **Contribution surface**: A defined shell-owned region where contributed commands may render.

## 2. System context

### 2.1 Current state

The current Workbench shell has a mixed toolbar arrangement:

- the menu bar exists in markup but is hidden by current shell-region defaults
- the shell renders a full-width top toolbar row when `WorkbenchShellRegion.ActiveToolToolbar` is visible
- that toolbar row currently mixes together:
  - a host-owned `Home` action
  - active-tool toolbar contributions
  - the theme toggle
- the explorer pane has no dedicated toolbar contribution surface of its own
- active-tool commands are logically scoped to the active tool, but visually render outside the active tab view

This makes the shell feel less focused because explorer/global actions, shell actions, and active-tool actions are co-located in a single horizontal row above the working area.

### 2.2 Proposed state

The Workbench shell shall use a clearer responsibility split:

- the menu bar shall be visible again and shall remain visible through a minimum host-provided shell menu presence
- the menu bar shall host the theme toggle on the right as a temporary shell-level placement
- the current full-width top toolbar row shall be removed entirely from the shell layout
- the explorer pane shall gain a dedicated mixed toolbar surface backed by `ExplorerToolbarContributions`
- the active tool toolbar shall render inside the active centre-pane tab view rather than as shell-wide chrome
- the active tab experience shall become: tab strip, active-tool toolbar, active-tool body

This arrangement keeps command surfaces closer to the area of work they affect and reduces shell-level visual clutter.

### 2.3 Assumptions

- The active tool remains equivalent to the active tab for toolbar purposes.
- Existing active-tool toolbar contributions can continue to use the current contribution model, but they will render in a different place.
- `Home` is better treated as an explorer or workspace-global action than as an active-tool action.
- The explorer toolbar should support a mixed surface that combines shell-global left-pane actions with contributions owned by the active explorer.
- The host will provide a minimum shell-level menu presence so the menu bar always has meaningful structure even before richer menu work is specified.
- The theme toggle may remain in the menu bar temporarily without blocking future relocation.
- The Workbench should continue to feel desktop-like rather than web-like.
- The existing menu bar contribution model can remain functionally unchanged for this work package even if its richer rendering is deferred.

### 2.4 Constraints

- The shell must remain aligned with the repository’s Workbench desktop-style UI direction.
- The solution must use the Workbench contribution model rather than introducing ad hoc per-module toolbar rendering.
- The menu bar must span the full window above all other content.
- The output panel and its toolbar must continue to behave independently of these toolbar changes.
- The implementation must remove the old top toolbar row rather than leaving an unused hidden region in regular layout flow.
- The design should stay close to the current Radzen Material appearance.

## 3. Component / service design (high level)

### 3.1 Components

The following areas are in scope:

- `WorkbenchHost.Components.Layout.MainLayout`
- shell region definitions and default visible-region configuration in `UKHO.Workbench.WorkbenchShell`
- shell contribution composition in `UKHO.Workbench.Services.Shell`
- contribution contracts or models needed to support `ExplorerToolbarContributions`
- host-owned shell actions currently surfaced through the top toolbar

### 3.2 Data flows

1. Static and runtime shell contributions are registered with the Workbench shell manager.
2. Menu contributions are composed for the visible menu bar.
3. Explorer toolbar contributions are composed as a mixed surface for the explorer pane, combining shell-global left-pane actions with contributions owned by the active explorer, and are rendered at the top of that pane.
4. Active-tool toolbar contributions are composed for the current active tool and rendered inside the active tab view.
5. Executing actions from any of these surfaces continues to route through the shared shell command path.

### 3.3 Key decisions

- The menu bar shall be visible in this work package.
- The menu bar shall always remain visible through host-provided minimum shell menus.
- The minimum host-provided shell menus shall include `Help`, `Edit`, and `View`.
- The detailed contents of `Help`, `Edit`, and `View` are intentionally deferred to a future work package.
- The theme toggle shall render right-aligned in the menu bar for now.
- The existing full-width top toolbar row shall be removed rather than hidden.
- `WorkbenchShellRegion.ActiveToolToolbar` shall be removed from the shell region model rather than repurposed.
- Explorer-scoped and workspace-global left-pane actions shall share one mixed explorer toolbar contribution surface.
- Active-tool actions shall render inside the active tab view.
- The eventual richer menu bar rendering can be deferred to a later work package without blocking this structural change.

## 4. Functional requirements

1. The Workbench shell shall make the menu bar visible by default.
2. The menu bar shall continue to span the full shell width above the working area.
3. The menu bar shall remain visible even when there are no active-tool menu contributions.
4. The host shall provide a minimum shell-level menu presence so the menu bar always contains meaningful shell menus.
5. The minimum shell-level menu presence shall include at least `Help`, `Edit`, and `View` menus.
6. The detailed contents of the minimum shell-level menus are out of scope for this work package and shall be specified later.
7. The menu bar shall include the theme toggle as a right-aligned action for this work package.
8. The shell shall remove the current full-width top toolbar row entirely.
9. The shell shall not retain a separate rendered toolbar row between the menu bar and the centre working surface.
10. The Workbench contribution model shall introduce a dedicated `ExplorerToolbarContributions` surface.
11. The explorer pane shall render explorer-toolbar contributions at the top of the explorer panel.
12. Explorer-toolbar contributions shall be a mixed surface intended for both explorer-scoped and workspace-global left-pane actions.
13. Host-owned actions such as `Home` shall render through the explorer toolbar contribution surface rather than the active-tool toolbar surface.
14. The existing active-tool toolbar contribution concept shall remain available for active-tool commands.
15. Active-tool toolbar contributions shall render inside the active centre-pane tab view.
16. The active tab presentation shall support a clear structure of tab strip, active-tool toolbar, and active content body.
17. Switching tabs shall update the rendered active-tool toolbar content to match the newly active tab.
18. When no tab is open, the shell shall not render an empty active-tool toolbar surface in the centre pane.
19. The shell shall continue to route menu, explorer-toolbar, and active-tool-toolbar actions through the shared command execution path.
20. The shell shall not require module UIs to become aware of layout mechanics in order to receive toolbar rendering.
21. The shell shall preserve explorer-pane interaction and tab activation behavior while relocating toolbar surfaces.
22. The shell shall remove special handling that treats the old top toolbar as the host surface for mixed shell, explorer, and active-tool actions.
23. The shell region model shall be updated so the removed top-toolbar region is no longer part of the normal rendered shell layout.
24. The output-panel toolbar shall remain unchanged by this work package.
25. The menu bar rendering introduced here shall be sufficient for functional use, even though richer presentation work may be deferred to a later work package.
26. The implementation shall support future expansion of explorer-toolbar contributions without requiring another shell-layout redesign.
27. The explorer toolbar shall support composition of both shell-global left-pane actions and actions contributed by the currently active explorer within the same rendered surface.
28. `WorkbenchShellRegion.ActiveToolToolbar` shall be deleted entirely rather than retained as a renamed or repurposed region.

## 5. Non-functional requirements

1. The resulting shell should feel more focused and less visually cluttered than the current arrangement.
2. Command placement should align with user attention so actions appear near the area of work they affect.
3. The shell should remain desktop-like rather than web-like.
4. Toolbar relocation should not introduce noticeable command-latency regressions.
5. The design should remain visually close to the current Radzen Material styling.
6. The shell should avoid introducing duplicate command surfaces for the same action.
7. The new layout should make future menu bar rendering work easier rather than harder.
8. The contribution model should remain understandable to module authors.

## 6. Data model

This work package requires contribution-model refinement rather than a new persisted business data model.

The shell conceptually needs to support:

- menu contributions
- explorer toolbar contributions
- active-tool toolbar contributions
- host-owned shell actions mapped to the appropriate contribution surface

Likely model impacts include:

- removal of host assumptions that special-case `Home` from the active-tool toolbar collection
- introduction of explorer-toolbar contribution contracts and composition paths
- retention of the current active-tool toolbar contribution contract for tab-scoped commands

No persisted storage change is required.

## 7. Interfaces & integration

- `MainLayout` must be updated to render the menu bar, explorer toolbar, tab strip, and active-tool toolbar in their new locations.
- `WorkbenchShellState` and related shell-region configuration must be updated to reflect the removed top-toolbar row and visible menu bar.
- Host-level menu registrations must ensure a minimum menu presence including at least `Help`, `Edit`, and `View`.
- `WorkbenchShellRegion` should delete the old shell-wide `ActiveToolToolbar` region rather than retaining it as a standalone or repurposed region.
- `WorkbenchShellManager` and contribution composition services must expose explorer-toolbar contributions separately from active-tool toolbar contributions.
- Existing command routing should remain centralized through `ExecuteCommandAsync` and related shell paths.
- Module and host registrations that currently depend on the old toolbar placement must be updated to target the correct new contribution surface.

## 8. Observability (logging/metrics/tracing)

No new external observability platform is introduced.

However, implementation diagnostics should make it possible to confirm:

- the menu bar is visible by default
- the menu bar remains visible through host-provided minimum menus even when there are no active-tool menu contributions
- explorer-toolbar contributions are composed and rendered for the active explorer
- active-tool toolbar contributions change with tab activation
- host-owned actions such as `Home` are no longer sourced from the active-tool toolbar collection
- command execution continues to flow through the shared shell command path after the layout change

## 9. Security & compliance

No new security boundary is introduced.

However:

- command-surface relocation must not bypass existing command authorization or validation behavior
- host-owned shell actions must continue to use the same controlled command-routing paths as before
- the temporary placement of the theme toggle in the menu bar must not introduce direct module-owned control over shell appearance outside approved shell contracts

## 10. Testing strategy

Testing should cover:

- the menu bar renders visibly in the shell by default
- the menu bar remains visible when there are no active-tool menu contributions
- the host provides at least `Help`, `Edit`, and `View` menus
- the theme toggle renders in the menu bar and is right-aligned
- the old top toolbar row is no longer rendered
- explorer-toolbar contributions render at the top of the explorer pane
- the `Home` action renders in the explorer toolbar rather than the active-tool toolbar
- active-tool toolbar contributions render inside the active tab view
- switching tabs updates the active-tool toolbar content correctly
- no active-tool toolbar surface is rendered when there is no open active tab
- command execution from menu bar, explorer toolbar, and active-tool toolbar still routes correctly
- output-panel toolbar behavior remains unaffected

Suggested test layers:

- Workbench host rendering tests for shell layout and toolbar placement
- shell-manager and contribution-composition tests for explorer-toolbar versus active-tool toolbar separation
- targeted interaction tests for tab switching and command execution from relocated toolbar surfaces

## 11. Rollout / migration

This change should be introduced as a shell-structure refinement of the existing Workbench layout.

Rollout expectations:

1. Make the menu bar visible and place the theme toggle in it temporarily.
2. Introduce explorer-toolbar contributions and migrate host-owned explorer/global actions such as `Home` to that surface.
3. Remove the full-width top toolbar row from layout and markup.
4. Render active-tool toolbar contributions inside the active tab view.
5. Verify that command behavior remains unchanged while command placement becomes more focused.
6. Defer richer menu bar rendering refinements to a later work package.

No external data migration is required.

## 12. Open questions

1. Whether the menu bar should remain visible even when it has no active-tool menu contributions, or whether the host should always provide a minimum shell-level menu presence.
