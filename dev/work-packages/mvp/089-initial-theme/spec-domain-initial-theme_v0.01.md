# Workbench Initial Theme Specification

- Version: `v0.01`
- Status: `Draft`
- Work Package: `089-initial-theme`
- Created: `2026-04-01`
- Related work: `dev/work-packages/mvp/057-studio-shell/`, `dev/work-packages/mvp/085-workbench-style/`, `dev/work-packages/mvp/088-workbench-toolbars/`
- Visual reference: attached Radzen Studio screenshot supplied with the request

## Change Log

- `v0.01` - Initial specification for aligning the Workbench shell theme, chrome styling, toolbar presentation, and fixed shell dimensions to the supplied Radzen Studio-inspired reference.
- `v0.01` - Recorded the confirmed light-theme activity-pane background as `(224, 225, 228)`.
- `v0.01` - Confirmed that lower output tabs should use the same borderless and flat-tab treatment when styled separately.

## 1. Overview

### 1.1 Purpose

Define the first explicit Workbench shell theming baseline so the UI adopts a cleaner, borderless, desktop-like appearance closely aligned to the supplied Radzen Studio screenshot. This specification covers border removal, flat tabs, toolbar presentation, fixed shell heights and widths, and named light and dark theme color values for the main shell regions.

### 1.2 Scope

This work package covers visual and layout behavior for the Workbench shell in `WorkbenchHost`, including:

- removing visible borders from shell chrome and panel surfaces
- differentiating panels using background colour rather than border lines
- making shell tabs flat and borderless
- making toolbar buttons icon-only with tooltip-based labels
- changing the tab overflow affordance to a compact ellipsis-style button rather than a combo-box look
- constraining the activity rail width and toolbar heights to fixed dimensions
- defining explicit dark-theme and light-theme background colours for major shell regions
- aligning the Workbench shell presentation to the supplied Radzen Studio screenshot while staying within the repository's existing Workbench architecture

This work package does not cover:

- broader functional changes to menu composition, tab behavior, explorer behavior, or output functionality
- typography redesign beyond what is required to support flat tabs and icon-only toolbar actions
- icon redesign or replacement
- per-module custom theming rules
- implementation of additional themes beyond the specified light and dark variants

### 1.3 Stakeholders

- Workbench users who need a cleaner and more cohesive desktop-style shell
- Developers maintaining `WorkbenchHost` layout and styling
- Maintainers of `UKHO.Workbench`, `UKHO.Workbench.Layout`, and shell contribution surfaces
- Module authors whose toolbars and tab content render inside the Workbench shell

### 1.4 Definitions

- **Activity pane**: The narrow left-most activity rail containing top-level shell and module entry points.
- **Explorer pane**: The left-side pane hosting explorer content and its toolbar.
- **Centre pane**: The main working area containing tab strips, toolbars, and active tab content.
- **Output panel**: The lower shell panel used for output and status-rich content.
- **Flat tab**: A tab with no visible border or raised chrome, differentiated primarily through colour, text, and optional icon state.
- **Borderless design**: A shell style in which panel separation is achieved through spacing and background colour only, with visible borders removed.
- **Visual reference screenshot**: The attached Radzen Studio screenshot supplied with the request and used as the intended styling baseline.

## 2. System context

### 2.1 Current state

The Workbench shell already has several style-focused work packages, but shell chrome is still evolving and does not yet have a fully specified theme baseline for all of the following at once:

- complete removal of visible borders
- flat tab presentation across tab strips
- compact icon-only toolbar presentation
- fixed shell dimensions for the activity pane and toolbar rows
- a named and explicit colour palette for both light and dark themes

Without a single specification for these decisions, the shell risks accumulating inconsistent treatment between the menu bar, activity pane, explorer, centre pane, and output surfaces.

### 2.2 Proposed state

The Workbench shell shall adopt a borderless theme baseline aligned to the supplied screenshot:

- all visible shell chrome borders shall be removed
- panel separation shall be achieved using background colour alone
- centre tabs shall be flat and borderless
- toolbar commands shall render as icon-only buttons with tooltips
- toolbar text labels shall be removed and exposed through tooltips where needed
- the tab overflow control shall appear as a compact ellipsis button until opened
- the activity pane width and shell toolbar heights shall use fixed values
- the shell shall support explicit dark and light theme background palettes for key regions

### 2.3 Assumptions

- The Workbench should remain visually close to the Radzen Material family while adopting the supplied borderless desktop-like styling.
- The shell owns layout sizing and chrome styling, not individual modules.
- Existing toolbar contribution mechanisms can continue to supply actions, but the shell will change how those actions are presented.
- Icon-only toolbar buttons remain usable provided tooltips are always available.
- Flat tabs still need a clear active and inactive state through text, icon, and background colour.
- Fixed heights for tab and toolbar rows are compatible with current Workbench shell layout direction.
- The supplied screenshot is the preferred design reference when resolving visual ambiguity.

### 2.4 Constraints

- The shell must remain desktop-like rather than web-like.
- The implementation must stay close to the stock Radzen Material theme so later custom theme work can build on it.
- Panel sizing must be enforced by the Workbench shell rather than module-specific CSS workarounds.
- The menu bar must continue to span the full window above the rest of the shell.
- The upper centre tab strip must remain visibly rendered.
- The lower output area and its toolbar must remain visibly rendered.
- The shell must use the confirmed light-theme activity-pane colour of `(224, 225, 228)`.

## 3. Component / service design (high level)

### 3.1 Components

The following areas are in scope:

- `WorkbenchHost` shell layout and theme styling
- activity pane chrome and sizing
- explorer pane chrome and toolbar styling
- centre pane tab-strip, toolbar, and view-surface styling
- output panel toolbar styling
- menu bar and status bar surface styling
- tab overflow control styling and presentation
- shell theme tokens or equivalent shared styling constants for light and dark modes

### 3.2 Data flows

1. The active shell theme selects the appropriate colour token set for light or dark mode.
2. Shell-owned layout regions consume those tokens for activity, explorer, centre, menu, output-toolbar, and status-bar backgrounds.
3. Toolbar contribution surfaces render actions using icon-only button chrome with tooltip labels.
4. Tab surfaces render active and inactive state using background colour, text treatment, and icon state rather than borders.
5. The tab overflow affordance renders as a compact ellipsis button and expands into the existing overflow interaction model when opened.

### 3.3 Key decisions

- All visible shell borders shall be removed.
- Panel differentiation shall rely on background colour rather than borders.
- Tabs shall be flat and borderless.
- Lower output tabs shall use the same borderless and flat-tab treatment when styled separately from upper centre tabs.
- Toolbar buttons shall be icon-only and expose labels through tooltips.
- Toolbar text labels shall be removed from rendered chrome.
- The output-panel toolbar may contain combo boxes and buttons only.
- The output-panel toolbar shall not render standalone text labels.
- The tab overflow affordance shall look like a compact ellipsis button rather than a combo box.
- The activity pane width shall be fixed at `50px`.
- The centre tab panel height shall be fixed at `36px`.
- The active toolbar, explorer toolbar, and output-panel toolbar heights shall each be fixed at `36px`, even when empty.
- The supplied Radzen Studio screenshot shall be treated as the visual baseline for borderless shell chrome.

## 4. Functional requirements

1. The Workbench shell shall remove all visible borders from shell chrome, including panel boundaries, tab chrome, and toolbar chrome.
2. The shell shall differentiate panels using background colour rather than border lines.
3. The activity pane shall render at a fixed width of `50px`.
4. The centre tab panel shall always render at a fixed height of `36px`.
5. The active toolbar shall always render at a fixed height of `36px`, including when it contains no contributed actions.
6. The explorer toolbar shall always render at a fixed height of `36px`, including when it contains no contributed actions.
7. The output-panel toolbar shall always render at a fixed height of `36px`, including when it contains no contributed actions.
8. Centre-pane tabs shall render as flat tabs with no visible borders.
9. Active closeable tabs shall indicate state using text and icon treatment without adding visible border chrome.
10. Inactive tabs shall remain visually flat and borderless.
11. Lower output tabs shall use the same flat, borderless styling approach when they are rendered through a separate tab surface.
12. Toolbar buttons shall render as icon-only buttons.
13. Toolbar buttons shall provide a tooltip for their accessible label or descriptive text.
14. Toolbar surfaces shall not render text labels next to toolbar buttons.
15. The output-panel toolbar shall permit only combo boxes and buttons as visible control types.
16. The output-panel toolbar shall remove standalone text labels and expose the same meaning through tooltips where appropriate.
17. The tab overflow control shall render in its collapsed state as a compact ellipsis-style button displaying `...`.
18. The tab overflow control shall not present as a combo box while collapsed.
19. When opened, the tab overflow control may display the existing overflow choices, but its collapsed chrome shall still match the compact ellipsis-style presentation.
20. The dark theme shall define the following background colours:
    - activity pane: `(14, 13, 18)`
    - explorer pane: `(38, 38, 46)`
    - centre pane views: `(30, 30, 30)`
    - centre panel toolbar: `(38, 38, 46)`
    - active centre pane tab: `(38, 38, 46)`
    - inactive centre pane tab: `(30, 30, 30)`
    - menu: `(38, 38, 46)`
    - output panel toolbar: `(38, 38, 46)`
    - status bar: `(38, 38, 46)`
21. The light theme shall define the following background colours:
    - activity pane: `(224, 225, 228)`
    - explorer pane: `(239, 239, 241)`
    - centre pane views: `(255, 255, 254)`
    - centre panel toolbar: `(239, 239, 241)`
    - active centre pane tab: `(239, 239, 241)`
    - inactive centre pane tab: `(224, 225, 228)`
    - menu: `(224, 225, 228)`
    - output panel toolbar: `(224, 225, 228)`
    - status bar: `(224, 225, 228)`
22. The shell shall apply the defined colour palette consistently across all host-owned shell regions.
23. The menu bar background shall use the active theme's menu colour.
24. The explorer pane background shall use the active theme's explorer-pane colour.
25. The centre pane view background shall use the active theme's centre-pane-views colour.
26. The active centre toolbar background shall use the active theme's centre-panel-toolbar colour.
27. The output-panel toolbar background shall use the active theme's output-panel-toolbar colour.
28. The status bar background shall use the active theme's status-bar colour.
29. The shell shall ensure borderless styling does not remove or hide required interaction states such as hover, active, selected, or focus.
30. The shell shall apply these styling rules centrally so module UIs do not need module-specific theme workarounds.
31. The visual result shall stay close to the supplied Radzen Studio screenshot as the intended styling reference.

## 5. Non-functional requirements

1. The shell should feel visually cleaner and less cluttered than a bordered panel design.
2. The Workbench should preserve a desktop-like look and feel.
3. The borderless styling should remain coherent across light and dark themes.
4. Fixed toolbar heights should prevent layout jitter between populated and empty toolbar states.
5. Icon-only toolbar presentation should remain discoverable through clear tooltip behavior.
6. The shell should avoid bespoke per-region styling exceptions unless explicitly required by this specification.
7. Styling changes should be maintainable through shared theme tokens or equivalent centralized styling definitions.
8. The theme should remain sufficiently close to stock Radzen Material that future custom theme work can extend it cleanly.
9. The borderless design should not degrade keyboard focus visibility or accessibility cues.
10. The styling baseline should support future shell work without reintroducing borders as a layout dependency.

## 6. Data model

No persisted business data model change is required.

The shell requires centralized styling definitions for:

- fixed shell dimensions for the activity pane and toolbar rows
- light-theme and dark-theme region background values
- flat-tab state styling
- icon-only toolbar button presentation
- compact tab-overflow control presentation

These may be represented through CSS variables, theme tokens, component parameters, or equivalent host-owned styling abstractions, but the exact implementation mechanism is out of scope for this specification.

## 7. Interfaces & integration

- `WorkbenchHost` shell layout components must apply fixed width and height rules for the activity pane, centre tab row, active toolbar, explorer toolbar, and output-panel toolbar.
- Shell-owned styling for menu bar, activity pane, explorer pane, centre pane, output toolbar, and status bar must consume the active theme palette.
- Toolbar contribution rendering must support icon-only button chrome with tooltip text.
- Output-panel toolbar rendering must limit visible control types to combo boxes and buttons.
- Tab rendering must support flat active and inactive state styling without visible borders.
- The tab overflow control must expose a compact collapsed ellipsis presentation while still integrating with the existing overflow interaction path.
- Theme switching must update all defined shell-region backgrounds consistently.

## 8. Observability (logging/metrics/tracing)

No new external observability platform is introduced.

Implementation diagnostics should make it possible to confirm:

- the expected theme palette is selected for the active theme
- shell regions receive the correct background colour values
- fixed shell widths and heights remain applied even when toolbars are empty
- toolbar actions continue to render and execute correctly after becoming icon-only
- the tab overflow affordance still functions after its collapsed styling is changed

## 9. Security & compliance

No new security boundary is introduced.

However:

- styling changes must not bypass existing command execution, authorization, or validation paths
- icon-only toolbar rendering must still provide accessible naming through tooltips and equivalent accessible metadata
- borderless presentation must preserve visible focus indication for keyboard users
- shell theme values must use the confirmed RGB values consistently across all regions

## 10. Testing strategy

Testing should cover:

- activity pane width renders at `50px`
- centre tab panel renders at `36px` height
- active toolbar renders at `36px` height even when empty
- explorer toolbar renders at `36px` height even when empty
- output-panel toolbar renders at `36px` height even when empty
- visible borders are removed from the targeted shell regions
- centre tabs render as flat, borderless tabs
- lower output tabs render with the same flat, borderless treatment when styled separately
- active and inactive tabs remain visually distinguishable through non-border cues
- toolbar buttons render as icon-only controls with tooltips
- output-panel toolbar renders only combo boxes and buttons
- standalone toolbar text labels are not rendered
- tab overflow control appears as a compact ellipsis button when collapsed
- tab overflow interaction still works when opened
- dark-theme shell regions apply the specified RGB values
- light-theme shell regions apply the specified RGB values, including the confirmed activity-pane value of `(224, 225, 228)`
- theme switching updates all relevant shell backgrounds consistently
- keyboard focus and interaction states remain visible despite border removal

Suggested test layers:

- host rendering tests for shell-region structure and class or style application
- targeted UI interaction tests for tooltip behavior and overflow control behavior
- Playwright end-to-end verification for theme switching and visible shell chrome presentation

## 11. Rollout / migration

This change should be introduced as a shell-theme refinement of the existing Workbench layout.

Rollout expectations:

1. Introduce centralized theme tokens or equivalent styling definitions for the specified shell regions.
2. Apply fixed dimensions for the activity pane and toolbar rows.
3. Remove borders from targeted shell surfaces and tabs.
4. Update toolbar rendering to icon-only buttons with tooltip-based labels.
5. Update output-panel toolbar presentation to remove text labels and retain only combo boxes and buttons.
6. Replace the collapsed tab-overflow chrome with a compact ellipsis-style button.
7. Verify both dark and light themes render consistently against the supplied visual baseline.
8. Verify the confirmed light-theme activity-pane value of `(224, 225, 228)` is applied consistently.

No external data migration is required.

## 12. Open questions

No open questions remain at this time.
