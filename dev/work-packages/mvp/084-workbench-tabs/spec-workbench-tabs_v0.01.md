# Work Package: `084-workbench-tabs` — Workbench tabbed view activation

**Target output path:** `dev/work-packages/mvp/084-workbench-tabs/spec-workbench-tabs_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created to capture the requirement that Workbench views open in tabs instead of replacing the current view.
- `v0.01` — Records the current working direction that re-selecting the same explorer item should focus the existing tab rather than opening a duplicate tab by default.
- `v0.01` — Confirms that re-selecting the same explorer item shall focus the existing tab and that duplicate-tab behavior is deferred from the first implementation.
- `v0.01` — Confirms that tabs shall be user-closable in the first implementation.
- `v0.01` — Confirms that closing the active tab shall activate the most recently active remaining tab.
- `v0.01` — Confirms that users shall not be able to reorder tabs manually in the first implementation and that tab order shall remain fixed by shell behavior.
- `v0.01` — Confirms that the Workbench shall not restore previously open tabs between sessions in the first implementation.
- `v0.01` — Confirms that dirty-state indicators are deferred to a later work package and are out of scope for the first implementation.
- `v0.01` — Confirms that explorer single-click selects only and double-click opens or focuses the corresponding tab.
- `v0.01` — Confirms that middle-click is ignored in the first implementation.
- `v0.01` — Confirms that tabs start with the explorer item label and may be updated by the hosted view after opening.
- `v0.01` — Confirms that closing the last remaining tab leaves no central content and returns user focus to the explorer.
- `v0.01` — Confirms that close warnings for unsaved changes are deferred with dirty-state support and are out of scope for the first implementation.
- `v0.01` — Confirms that keyboard shortcuts for tab management are deferred to a later work package.
- `v0.01` — Confirms that tabs shall show icons in the first implementation when an explorer item or hosted view provides one.
- `v0.01` — Confirms that when both icon sources exist, the hosted view icon shall replace the explorer item icon after the tab is open.
- `v0.01` — Confirms that different parameter identities for the same view type shall open separate tabs, while matching parameter identities shall reuse the existing tab.
- `v0.01` — Confirms that visible title disambiguation rules for same-title tabs are deferred to a later work package.
- `v0.01` — Confirms that hosted-view title and icon updates shall apply immediately even when the tab is inactive.
- `v0.01` — Confirms that the first implementation shall not impose a fixed cap on the number of open tabs.
- `v0.01` — Confirms that the close affordance shall be shown only on the active tab in the first implementation.
- `v0.01` — Confirms that the first implementation shall include a basic tab context menu.
- `v0.01` — Confirms that the first tab context menu shall include `Close` only.
- `v0.01` — Confirms that tab overflow shall be handled by an always-visible small overflow dropdown at the right side of the tab strip, listing all open tabs and allowing selection of any tab.
- `v0.01` — Confirms that selecting a tab from the overflow dropdown shall reorder the strip minimally, only enough to make the selected tab visible in the main strip.
- `v0.01` — Confirms that the overflow dropdown shall indicate which tab is currently active when opened.
- `v0.01` — Confirms that overflow dropdown close actions are deferred to a later work package and that the first implementation supports selection only.
- `v0.01` — Confirms that tabs opened from the explorer shall always become active immediately in the first implementation.
- `v0.01` — Confirms that reopening recently closed tabs is not supported in the first implementation.
- `v0.01` — Confirms that all tabs follow the same close behavior and that pinned or non-closable tabs are not part of the first implementation.
- `v0.01` — Confirms that closing a non-active tab shall leave the current active tab unchanged in the first implementation.
- `v0.01` — Confirms that each open tab preserves its in-memory view state while open, within the current user session.
- `v0.01` — Confirms that closing a tab shall dispose its in-memory state immediately.
- `v0.01` — Confirms that the same logical tab open-or-focus rules apply across explorer and other shell entry points in the first implementation.
- `v0.01` — Confirms that no first-implementation shell entry point may bypass reuse rules to force a new tab.
- `v0.01` — Confirms that browser refresh or Blazor Server reconnect should restore the current tab session when the user session survives.
- `v0.01` — Confirms that if the user session does not survive refresh or reconnect, the first implementation falls back to a fresh empty shell without additional recovery.
- `v0.01` — Confirms that visible failure messaging for session recovery fallback is deferred to a later work package.
- `v0.01` — Confirms that closing a tab from the tab strip and from the tab context menu shall follow exactly the same behavior and outcomes.
- `v0.01` — Confirms that overflow dropdown ordering rules are deferred to a later work package.
- `v0.01` — Confirms that the first implementation overflow dropdown is list-only and does not include filter or search input.
- `v0.01` — Confirms that the first implementation shall define long-tab-title display rules rather than deferring them.
- `v0.01` — Confirms that long tab titles shall truncate with ellipsis and show the full title on hover in the first implementation.
- `v0.01` — Confirms that truncated tab-title hover behavior shall use the Radzen tooltip service rather than the browser's native tooltip.
- `v0.01` — Confirms that the Radzen tooltip for tab titles appears on every tab hover, not only when the title is truncated.
- `v0.01` — Confirms that tab-title tooltips shall use the default Radzen tooltip positioning behavior in the first implementation.
- `v0.01` — Confirms that tab-title tooltips shall use the default Radzen tooltip timing behavior in the first implementation.
- `v0.01` — Confirms that the first implementation keeps overflow dropdown entries text-only and does not show tab icons there.
- `v0.01` — Confirms that long overflow dropdown entries shall use the same Radzen tooltip behavior as the tab strip.
- `v0.01` — Confirms that the current active indication in the overflow dropdown is sufficient for the first implementation and that remaining look-and-feel refinements may be reviewed later.

## 1. Overview

### 1.1 Purpose

This specification defines the first tabbed view behavior for the Workbench shell.

Its purpose is to replace the current single-view replacement behavior with a tabbed document-style model that better matches a desktop-like Workbench experience.

### 1.2 Scope

This specification currently includes:

- tabbed opening behavior for Workbench views activated from explorer items
- explorer selection versus open activation behavior
- explicit non-behavior for middle-click in the first implementation
- tab title initialization and hosted-view title updates
- tab icon display when an icon is available
- icon precedence between explorer items and hosted views
- parameter-sensitive logical tab identity rules
- immediate tab metadata updates for inactive tabs
- uncapped open-tab count for the first implementation
- active-tab-only close affordance behavior
- basic tab context menu support
- single-action initial tab context menu behavior
- tab overflow handling through an always-visible overflow dropdown
- minimal overflow-selection reordering to restore selected tabs to the visible strip
- active-tab indication within the overflow dropdown
- selection-only overflow dropdown behavior
- immediate activation of explorer-opened tabs
- no recently-closed-tab reopen support in the first implementation
- uniform close behavior for all tabs
- stable active-tab behavior when closing a non-active tab
- per-tab in-memory state preservation while tabs remain open
- immediate disposal of tab state on close
- consistent tab reuse rules across shell entry points
- no forced-new-tab exceptions in the first implementation
- refresh and reconnect restoration within a surviving user session
- fresh-shell fallback when the user session does not survive
- deferred recovery-failure messaging
- consistent close behavior across tab close entry points
- list-only overflow dropdown behavior without filter/search
- long-tab-title display behavior
- truncation and hover-title behavior for long tab titles
- Radzen tooltip implementation for truncated tab titles
- always-on tab-title tooltip behavior
- default Radzen tooltip positioning
- default Radzen tooltip timing
- text-only overflow dropdown entries
- matching tooltip behavior for long overflow dropdown entries
- tab activation rules when a view is already open
- user-driven tab closing behavior
- fixed tab ordering behavior for the first implementation
- per-session tab lifecycle behavior
- empty-shell behavior after the last tab is closed
- expected shell behavior for focus and reuse of existing tabs
- initial constraints intended to avoid the complexity of full docking behavior
- interaction expectations relevant to the Blazor-based Workbench shell

This specification currently excludes:

- full docking support
- advanced tab grouping or tear-off windows
- arbitrary layout persistence beyond what is already defined elsewhere
- cross-session restoration of previously open tabs
- reopening recently closed tabs
- pinned or non-closable tabs
- dirty-state indicators on tabs
- close warnings for unsaved changes
- keyboard shortcuts for tab management
- visible title disambiguation rules for same-title tabs
- close actions within the overflow dropdown
- overflow dropdown ordering rules
- overflow dropdown filter or search input
- icon display within overflow dropdown entries
- final implementation details for settings storage unless explicitly brought into scope

### 1.3 Stakeholders

- Workbench platform developers
- module and tool authors
- UX and product stakeholders for the Workbench shell
- repository maintainers responsible for Workbench consistency

### 1.4 Definitions

- `view`: a Workbench-hosted UI surface opened from explorer interaction or equivalent shell navigation
- `tab`: the shell-level representation of an open view
- `tab reuse`: activation of an already open tab instead of creating a duplicate
- `duplicate tab`: a second tab representing the same logical explorer item or view target
- `parameter identity`: the stable set of input values that distinguishes one logical target of a view type from another

## 2. System context

### 2.1 Current state

The Workbench direction is already constrained away from full docking behavior because docking is expected to introduce avoidable edge cases.

The agreed shell direction is desktop-like, using structured layout regions and splitters rather than free-form docking.

The key navigation requirement is that Workbench views must open in tabs and repeated activation of the same explorer item must reuse the existing tab.

### 2.2 Proposed state

The Workbench shell shall open explorer-driven views in tabs instead of replacing the current central view.

When a user activates an explorer item whose logical view is already open, the shell shall focus the existing tab instead of opening a second copy.

This establishes a predictable IDE-like experience, reduces accidental tab proliferation, and keeps the first implementation simpler.

A potential future enhancement is to support explicit duplicate-tab opening through a separate later work package, but that behavior is out of scope for the first implementation.

Each Workbench session shall start without restoring previously open tabs from an earlier session.

Dirty or unsaved-state indicators on tabs are deferred from the first implementation.

Warnings on close for unsaved changes are also deferred from the first implementation.

Keyboard shortcuts for tab management are also deferred from the first implementation.

Visible title disambiguation rules for same-title tabs are also deferred from the first implementation.

The first implementation shall not impose a fixed cap on the number of open tabs.

Overflow dropdown close actions are also deferred from the first implementation.

Tabs opened from the explorer shall always become active immediately in the first implementation.

Reopening recently closed tabs is not supported in the first implementation.

All tabs follow the same close behavior in the first implementation.

Closing a non-active tab leaves the current active tab unchanged in the first implementation.

Each open tab preserves its in-memory view state while it remains open in the current user session.

Closing a tab disposes that tab's in-memory state immediately.

The same logical tab open-or-focus rules apply across explorer and other shell entry points in the first implementation.

No first-implementation shell entry point may bypass those reuse rules to force a new tab.

Browser refresh or Blazor Server reconnect should restore the current tab session when the user session survives.

If the user session does not survive refresh or reconnect, the first implementation falls back to a fresh empty shell without additional recovery.

Visible failure messaging for session recovery fallback is deferred from the first implementation.

Closing a tab from the tab strip and from the tab context menu follows exactly the same behavior and outcomes.

Overflow dropdown ordering rules are deferred from the first implementation.

The first implementation overflow dropdown is list-only and does not include filter or search input.

The first implementation shall define long-tab-title display behavior.

Long tab titles shall truncate with ellipsis and show the full title on hover in the first implementation.

Truncated tab-title hover behavior shall use the Radzen tooltip service rather than the browser's native tooltip.

The Radzen tooltip for tab titles appears on every tab hover, not only when the title is truncated.

Tab-title tooltips shall use the default Radzen tooltip positioning behavior in the first implementation.

Tab-title tooltips shall use the default Radzen tooltip timing behavior in the first implementation.

Overflow dropdown entries remain text-only in the first implementation.

Long overflow dropdown entries shall use the same Radzen tooltip behavior as the tab strip.

The current active indication in the overflow dropdown is sufficient for the first implementation.

### 2.3 Assumptions

- the Workbench should behave more like a desktop tool host than a page-navigation web application
- explorer items can be mapped to stable logical view identities suitable for tab reuse checks
- logical view identity can include both view type and stable parameter identity when needed
- the first implementation should use predictable tab reuse rather than duplicate-tab behavior
- duplicate-tab support, if needed, can be added later without invalidating the primary tabbed model
- cross-session tab restoration is not required for the first implementation
- dirty-state signaling is not required for the first implementation
- unsaved-change close interception is not required for the first implementation
- keyboard-driven tab management is not required for the first implementation
- visible same-title disambiguation is not required for the first implementation
- a fixed open-tab limit is not required for the first implementation
- overflow-dropdown close actions are not required for the first implementation
- explorer-driven background tab opening is not required for the first implementation
- recently closed tab history is not required for the first implementation
- pinned or special non-closable tab behavior is not required for the first implementation
- closing a non-active tab does not require active-tab reassignment in the first implementation
- per-tab in-memory state is isolated to the current user session and does not imply cross-user sharing
- closed-tab state retention is not required for the first implementation
- non-explorer shell entry points can participate in the same logical tab identity model
- forced new-tab exceptions are not required for the first implementation
- surviving user sessions can restore their current tab state after refresh or reconnect
- non-surviving sessions fall back to a fresh empty shell without additional recovery
- explicit recovery-failure messaging is not required for the first implementation
- tab close entry points share the same lifecycle and activation rules in the first implementation
- a specific overflow dropdown ordering strategy is not required for the first implementation
- overflow dropdown filter or search input is not required for the first implementation
- long-tab-title display rules should be defined for the first implementation
- hover tooltips are an acceptable first-implementation mechanism for exposing full long tab titles
- Radzen tooltip behavior is the preferred tooltip mechanism for this Workbench UI
- showing the tab title tooltip on every hover is acceptable in the first implementation
- explicit custom tooltip positioning is not required for the first implementation
- explicit custom tooltip timing is not required for the first implementation
- overflow dropdown entries do not require tab iconography in the first implementation
- overflow dropdown entries may reuse the same tooltip pattern as tabs for long titles

### 2.4 Constraints

- the solution must remain compatible with the current Workbench shell direction and avoid introducing docking semantics
- the first implementation should minimize edge cases around duplicate state, focus, and tab explosion
- the behavior must remain simple enough to implement cleanly in the Blazor Workbench shell

## 3. Component / service design (high level)

### 3.1 Components

1. `Explorer activation layer`
   - detects user activation of Workbench explorer items
   - resolves the logical target view identity
   - distinguishes selection from open activation
   - ignores middle-click behavior in the first implementation
   - activates newly opened tabs immediately

2. `Shell tab activation layer`
   - applies the same logical tab identity and reuse rules across explorer and other shell entry points
   - routes menu, command, link, and overflow activations through shared tab activation behavior

3. `Tab management service`
   - tracks open tabs and their logical identities
   - decides whether to open a new tab or focus an existing tab
   - distinguishes same-type views with different parameter identities
   - preserves per-tab in-memory state while tabs remain open

4. `Workbench shell tab strip`
   - renders open tabs
   - displays active tab state
   - allows users to switch between open views
   - allows users to close tabs
   - preserves shell-controlled tab ordering
   - shows the close affordance on the active tab only
    - supports a basic tab context menu
    - exposes `Close` as the initial context-menu action
    - includes an always-visible overflow dropdown aligned with the tabs
    - reorders minimally when overflow selection is used so the selected tab becomes visible
    - indicates the currently active tab within the overflow dropdown
    - supports selection only within the overflow dropdown in the first implementation
    - remains list-only without filter or search input in the first implementation
    - keeps overflow dropdown entries text-only in the first implementation
    - applies defined long-title display behavior in the first implementation
    - truncates long titles with ellipsis and exposes the full title on hover
    - uses Radzen tooltip behavior for truncated-title hover display
    - shows the title tooltip on every tab hover
    - uses default Radzen tooltip positioning in the first implementation
    - uses default Radzen tooltip timing in the first implementation
    - uses the same Radzen tooltip behavior for long overflow dropdown entries

5. `Hosted view surface`
   - displays the content of the active tab within the main Workbench region
   - may update the active tab title after the view has opened
   - may provide an icon for tab display after the view has opened
   - takes precedence for icon display after the tab has opened
   - applies title and icon updates immediately even when its tab is inactive

### 3.2 Data flows

1. The user activates an explorer item.
2. A single-click updates explorer selection only.
3. A double-click resolves the logical identity of the requested view.
4. The tab management service checks whether a tab for that logical identity is already open.
5. If no matching tab exists, the Workbench opens a new tab and displays the view.
6. If a matching tab exists, the Workbench focuses the existing tab and displays its current state.
7. If the user closes a tab, the Workbench removes that tab from the open tab set and updates the active tab accordingly.
8. If the closed tab was active, the Workbench activates the most recently active remaining tab.
9. Tab order remains under shell control and is not changed directly by the user.
10. Middle-click on an explorer item or tab has no special effect in the first implementation.
11. A newly opened tab starts with the explorer item label and may later receive a title update from the hosted view.
12. If the last remaining tab is closed, the central content area becomes empty and focus returns to the explorer.
13. A newly opened tab shows an icon when one is available from the explorer item or hosted view.
14. If both icon sources exist, the explorer item icon is used initially and the hosted view icon replaces it after the view provides one.
15. Hosted-view title or icon updates are reflected immediately, even if the tab is not currently active.
16. The close affordance is visible only on the active tab.
17. A basic context menu is available for tab interactions in the first implementation.
18. The initial tab context menu includes `Close` only.
19. An always-visible overflow dropdown at the right side of the tab strip lists all open tabs and allows selecting one.
20. Selecting a tab from the overflow dropdown reorders the strip minimally, only enough to make the selected tab visible in the main strip.
21. The overflow dropdown indicates which tab is currently active when the menu is opened.
22. The overflow dropdown supports selection only and does not expose close actions in the first implementation.
23. A tab opened from the explorer becomes active immediately.
24. Switching away from a tab does not reset its in-memory state while the tab remains open.
25. Closing a tab disposes its in-memory state immediately.
26. Non-explorer shell entry points use the same logical tab identity rules to decide whether to open a new tab or focus an existing one.
27. No first-implementation shell entry point bypasses reuse rules to force a new tab.
28. Browser refresh or Blazor Server reconnect restores the current tab session when the user session survives.
29. If the user session does not survive refresh or reconnect, the Workbench falls back to a fresh empty shell without additional recovery.
30. The first implementation shall not require visible messaging when session recovery fallback occurs.
31. Closing a tab from the tab strip and closing a tab from the tab context menu shall have identical behavior and outcomes.
32. The first implementation overflow dropdown shall not include filter or search input.
33. The first implementation shall define long-tab-title display behavior.
34. Long tab titles shall truncate with ellipsis and show the full title on hover in the first implementation.
35. Truncated tab-title hover behavior shall use the Radzen tooltip service rather than the browser's native tooltip.
36. The first implementation shall show the tab title tooltip on every tab hover, not only when the title is truncated.
37. The first implementation shall use default Radzen tooltip positioning for tab-title hover behavior.
38. The first implementation shall use default Radzen tooltip timing for tab-title hover behavior.
39. Overflow dropdown entries shall remain text-only in the first implementation.
40. Long overflow dropdown entries shall use the same Radzen tooltip behavior as tab titles.

### 3.3 Key decisions

- views open in tabs instead of replacing the current view
- explorer single-click shall select only
- explorer double-click shall open or focus the corresponding tab
- middle-click shall be ignored in the first implementation
- tabs shall initialize from the explorer item label and may be renamed by the hosted view after opening
- tabs shall show icons when one is available from the explorer item or hosted view
- hosted view icons shall take precedence over explorer item icons after the tab is open
- different parameter identities for the same view type shall produce separate tabs
- hosted-view title and icon updates shall apply immediately even for inactive tabs
- the close affordance shall be shown only on the active tab
- a basic tab context menu shall be supported in the first implementation
- the initial tab context menu shall include `Close` only
- tab overflow shall be handled through an always-visible small overflow dropdown aligned with the tab strip
- selecting from the overflow dropdown shall reorder the strip minimally so the selected tab becomes visible
- the overflow dropdown shall indicate the currently active tab
- overflow dropdown close actions are deferred to a later work package
- tabs opened from the explorer shall always activate immediately
- reopening recently closed tabs is out of scope for the first implementation
- all tabs shall follow the same close behavior in the first implementation
- closing a non-active tab shall leave the current active tab unchanged
- each open tab shall preserve its in-memory state while it remains open
- closing a tab shall dispose its in-memory state immediately
- the same logical tab reuse rules shall apply across explorer and other shell entry points
- no first-implementation shell entry point shall bypass reuse to force a new tab
- surviving user sessions shall restore the current tab session after refresh or reconnect
- non-surviving sessions shall fall back to a fresh empty shell without additional recovery
- visible messaging for session recovery fallback is deferred to a later work package
- tab strip close and tab context-menu close shall behave identically
- overflow dropdown ordering rules are deferred to a later work package
- the first implementation overflow dropdown shall remain list-only without filter or search input
- the first implementation shall define long-tab-title display behavior
- long tab titles shall truncate with ellipsis and show the full title on hover
- truncated tab-title hover behavior shall use Radzen tooltip behavior
- tab title tooltip behavior shall apply on every tab hover
- tab-title tooltips shall use default Radzen positioning
- tab-title tooltips shall use default Radzen timing
- overflow dropdown entries shall remain text-only
- long overflow dropdown entries shall use the same Radzen tooltip behavior as tabs
- the current active indication in the overflow dropdown is sufficient for the first implementation
- repeated activation of the same explorer item shall focus the existing tab
- duplicate tabs for the same logical item are out of scope for the first implementation
- users shall not be able to reorder tabs manually in the first implementation
- each new session shall start with no restored open tabs
- dirty-state indicators are deferred to a later work package
- closing the last remaining tab shall leave no central content and return focus to the explorer
- close warnings for unsaved changes are deferred with dirty-state support
- keyboard shortcuts for tab management are deferred to a later work package
- visible title disambiguation rules for same-title tabs are deferred to a later work package
- the first implementation shall not impose a fixed cap on open tabs
- any duplicate-tab capability should be treated as a future explicit product decision rather than implicit shell behavior

## 4. Functional requirements

1. The Workbench shall open explorer-activated views in tabs.
2. A single-click on an explorer item shall update selection only and shall not open a tab.
3. A double-click on an explorer item shall open or focus its corresponding tab.
4. Opening a new explorer item shall not replace the currently active view if that view remains open in another tab.
5. The Workbench shall maintain a visible tab strip for open views.
6. Each open tab shall represent a stable logical view identity.
7. When a user double-clicks an explorer item whose logical view is not already open, the Workbench shall open a new tab for that view and activate it.
8. When a user double-clicks an explorer item whose logical view is already open, the Workbench shall focus the existing tab instead of opening a duplicate tab.
9. The Workbench shall preserve the state of an already open view when focus returns to its existing tab.
10. The Workbench shall allow users to switch between open tabs without reloading the entire shell.
11. The initial implementation shall not support opening duplicate tabs for repeated activation of the same explorer item.
12. The shell shall define a consistent logical identity strategy so that repeated activation can reliably determine whether a matching tab is already open.
13. Logical tab identity may include stable parameter identity in addition to view type.
14. When the same view type is requested with different parameter identities, the Workbench shall open separate tabs.
15. When the same view type is requested with the same parameter identity, the Workbench shall focus the existing tab.
16. If future duplicate-tab behavior is introduced, it shall be triggered explicitly by a deliberate user action or configuration rather than by the default explorer click behavior.
17. Tabs shall be user-closable in the first implementation.
18. When the active tab is closed, the Workbench shall activate the most recently active remaining tab.
19. Users shall not be able to reorder tabs manually in the first implementation.
20. Tab order shall remain fixed by the shell's open and activation behavior until a later work package changes that behavior.
21. The Workbench shall not restore previously open tabs when a later session starts in the first implementation.
22. The first implementation shall not require dirty or unsaved-state indicators on tabs.
23. Middle-click on an explorer item or tab shall be ignored in the first implementation.
24. A newly opened tab shall initialize its title from the corresponding explorer item label.
25. A hosted view may update its own tab title after opening through an approved shell interaction.
26. When the last remaining tab is closed, the Workbench shall leave the main content region empty.
27. When the last remaining tab is closed, the Workbench shall return focus to the explorer.
28. The first implementation shall not require a close warning when a hosted view has unsaved changes.
29. The first implementation shall not require keyboard shortcuts for tab management.
30. Tabs shall display an icon in the first implementation when an icon is available from the explorer item or hosted view.
31. If both an explorer item and a hosted view provide an icon, the hosted view icon shall replace the explorer item icon after the tab is open.
32. The first implementation shall not require visible title disambiguation when different tabs share the same base title.
33. A hosted view title update shall be reflected immediately even when its tab is inactive.
34. A hosted view icon update shall be reflected immediately even when its tab is inactive.
35. The first implementation shall not impose a fixed maximum number of open tabs.
36. The first implementation shall show the close affordance only on the active tab.
37. The first implementation shall include a basic context menu for tab interactions.
38. The first implementation tab context menu shall include `Close` only.
39. The first implementation shall include an always-visible overflow dropdown at the right side of the tab strip.
40. The overflow dropdown shall list all open tabs.
41. Selecting an item from the overflow dropdown shall activate the corresponding tab.
42. Selecting an item from the overflow dropdown shall reorder the strip minimally, only enough to make the selected tab visible in the main strip.
43. The overflow dropdown shall indicate which tab is currently active when the menu is opened.
44. The first implementation overflow dropdown shall not expose close actions for listed tabs.
45. A tab opened from the explorer shall become active immediately in the first implementation.
46. The first implementation shall not support reopening recently closed tabs.
47. The first implementation shall not include pinned or non-closable tabs.
48. Closing a non-active tab shall leave the current active tab unchanged in the first implementation.
49. While a tab remains open, switching away from it and back again shall preserve that tab's in-memory view state.
50. Closing a tab shall dispose that tab's in-memory state immediately.
51. Shell entry points other than the explorer shall use the same logical tab identity and open-or-focus rules in the first implementation.
52. The first implementation shall not define any shell entry point that bypasses reuse rules to force a new tab.
53. If the current user session survives a browser refresh or Blazor Server reconnect, the Workbench shall restore the current tab session.
54. If the current user session does not survive a browser refresh or Blazor Server reconnect, the Workbench shall fall back to a fresh empty shell without additional recovery.
55. The first implementation shall not require visible user messaging when session recovery fallback occurs.
56. Closing a tab from the tab strip or from the tab context menu shall produce the same disposal, activation, and focus outcomes.
57. The first implementation shall not require a specific ordering strategy for entries in the overflow dropdown.
58. The first implementation shall not require filter or search input within the overflow dropdown.
59. The first implementation shall define a specific display rule for long tab titles.
60. The first implementation shall truncate long tab titles with ellipsis and expose the full title on hover.
61. The first implementation shall use the Radzen tooltip service to display the full title for truncated tab titles.
62. The first implementation shall show the tab title tooltip on every tab hover.
63. The first implementation shall use the default Radzen tooltip position for tab-title hover behavior.
64. The first implementation shall use the default Radzen tooltip timing for tab-title hover behavior.
65. The first implementation shall not require icons in overflow dropdown entries.
66. The first implementation shall use the same Radzen tooltip behavior for long overflow dropdown entries as for tab titles.
67. The first implementation shall not require stronger visual distinction for the active overflow entry beyond the current active indication.

## 5. Non-functional requirements

1. Tab activation behavior should feel immediate and predictable.
2. The first implementation should minimize state-management complexity.
3. The design should avoid introducing corner cases associated with docking or uncontrolled multi-instance behavior.
4. The behavior should align with a desktop-like Workbench mental model.
5. Overflow handling should preserve predictable access to any open tab without forcing aggressive tab compression.
6. Overflow selection should avoid disruptive reshuffling of tab order beyond what is needed to restore visibility of the selected tab.
7. Overflow navigation should preserve orientation by clearly indicating the currently active tab in the overflow menu.
8. Overflow interactions should remain simple in the first implementation by limiting the dropdown to tab selection behavior.
9. Per-tab state management should preserve expected user continuity while remaining scoped to the current user session.
10. The first implementation overflow experience should avoid extra interaction complexity such as inline filtering.
11. Long tab titles should be handled predictably so tabs remain readable without destabilizing the strip layout.
12. Long-title handling should preserve readability while keeping the tab strip compact.
13. Tooltip behavior for tab titles should align with the Radzen-based Workbench UI approach.
14. Tab-title hover behavior should remain consistent rather than varying by truncation state.
15. Tooltip behavior should avoid unnecessary first-implementation customization when default Radzen behavior is sufficient.
16. Tooltip timing should avoid unnecessary first-implementation customization when default Radzen behavior is sufficient.
17. Overflow navigation should remain simple and text-focused in the first implementation.
18. Long-title behavior should remain consistent between the tab strip and overflow dropdown.
19. Overflow active-state presentation should avoid extra first-implementation visual complexity when the current indication is sufficient.

## 6. Data model

The shell requires a tab descriptor that can at minimum capture:

- a stable tab identifier
- a logical view identity used for tab reuse checks
- parameter identity where needed to distinguish same-type tabs
- a title
- an icon or equivalent visual marker if used by the shell
- active state
- closable state

The shell also requires tab activity ordering metadata so it can determine the most recently active remaining tab when the active tab is closed.

The shell requires per-tab in-memory state to remain associated with each open tab until that tab is closed.

The shell requires closed tabs to release their associated in-memory state immediately.

The shell requires session-aware restoration of open tabs and their in-memory state when the current user session survives refresh or reconnect.

The shell requires a fresh-shell fallback path when the current user session does not survive refresh or reconnect.

## 7. Interfaces & integration

The Workbench shell requires an internal activation contract between explorer interactions and tab management so that view identity resolution and tab reuse decisions are centralized.

The same activation contract should be reusable by other shell entry points so tab identity and reuse behavior remains consistent.

The exact API shape is out of scope for this draft.

## 8. Observability (logging/metrics/tracing)

Initial implementation should support lightweight diagnostics around:

- view activation requests
- new-tab creation events
- existing-tab focus events
- tab resolution failures if a requested logical identity cannot be mapped correctly

## 9. Security & compliance

No unique security or compliance requirements are identified beyond standard Workbench host behavior.

## 10. Testing strategy

The implementation should be validated with UI-driven tests that prove:

- a single-click selects an explorer item without opening a tab
- a first double-click on an explorer item opens a new tab
- a second distinct explorer item opens in a second tab
- double-clicking the first explorer item again focuses its existing tab rather than opening a duplicate
- requesting the same view type with different parameter identities opens separate tabs
- requesting the same view type with the same parameter identity focuses the existing tab
- a newly opened tab starts with the explorer item label
- a hosted view can update its tab title after opening
- a tab shows an icon when one is available
- a hosted view icon replaces the explorer item icon when both are provided
- hosted-view title and icon updates appear immediately on inactive tabs
- opening additional tabs is not blocked by a fixed first-implementation tab limit
- the tab strip shows the close affordance only on the active tab
- a user can close an open tab
- closing the active tab activates the most recently active remaining tab
- closing a non-active tab leaves the current active tab unchanged
- closing the last remaining tab leaves no central content and returns focus to the explorer
- switching away from a tab and back again preserves that tab's in-memory state while it remains open
- closing a tab disposes that tab's in-memory state immediately
- users cannot reorder tabs manually
- a new session starts without restoring previously open tabs
- middle-click on explorer items and tabs has no special effect
- selecting a tab from the overflow dropdown makes that tab visible in the main strip with minimal reordering
- the overflow dropdown indicates the currently active tab
- the overflow dropdown provides selection only and no close action
- the overflow dropdown remains list-only without filter or search input
- the overflow dropdown remains text-only
- long tab titles follow a defined first-implementation display rule
- long tab titles truncate with ellipsis and show the full title on hover
- truncated tab titles use Radzen tooltip behavior
- tab title tooltips appear on every tab hover
- tab title tooltips use the default Radzen position
- tab title tooltips use the default Radzen timing
- long overflow dropdown entries use the same Radzen tooltip behavior as tabs
- a tab opened from the explorer becomes active immediately
- recently closed tabs cannot be reopened in the first implementation
- all tabs follow the same close behavior in the first implementation
- browser refresh or Blazor Server reconnect restores the current tab session when the user session survives
- browser refresh or Blazor Server reconnect falls back to a fresh empty shell when the user session does not survive
- tab strip close and tab context-menu close behave identically
- non-explorer shell entry points use the same open-or-focus reuse rules

Dirty-state indicator behavior is out of scope for this work package and does not require first-implementation test coverage.

Unsaved-change close warning behavior is also out of scope for this work package and does not require first-implementation test coverage.

Keyboard shortcut behavior for tab management is also out of scope for this work package and does not require first-implementation test coverage.

Visible title disambiguation behavior for same-title tabs is also out of scope for this work package and does not require first-implementation test coverage.

Fixed tab-limit behavior is out of scope for this work package beyond confirming that no cap is imposed in the first implementation.

Overflow dropdown ordering behavior is out of scope for this work package and does not require first-implementation test coverage.

Overflow dropdown filter/search behavior is out of scope for this work package beyond confirming that no filter or search input is included in the first implementation.

## 11. Rollout / migration

The change should replace any current single-view replacement behavior in the Workbench main content region with tabbed activation behavior.

Migration should preserve the broader Workbench layout direction and must not introduce docking as part of this change.

## 12. Open questions

No outstanding functional questions remain for this work package.

Remaining look-and-feel refinements may be reviewed later during implementation or UX review.
