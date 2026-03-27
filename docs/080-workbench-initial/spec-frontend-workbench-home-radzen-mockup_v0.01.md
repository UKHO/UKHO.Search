# Work Package: `080-workbench-initial` — `WorkbenchHost` Radzen shell refinement mock-up

**Target output path:** `docs/080-workbench-initial/spec-frontend-workbench-home-radzen-mockup_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Updates the existing Radzen shell mock-up specification so the current work package continues to use a single specification document as its source of truth.
- `v0.01` — Captures the requirement for a full-width top application menu bar above the current Workbench shell.
- `v0.01` — Captures the requirement for a horizontally split central area with a new lower panel and placeholder text content.
- `v0.01` — Captures the requirement for top-aligned Radzen tabs in each visible shell section, each with a right-aligned close icon.
- `v0.01` — Captures the requirement for a top-middle styling showcase that uses a representative set of common Radzen form and data components with sample data.
- `v0.01` — Confirms that menu items, tab close icons, and sample controls are primarily visual placeholders for shell and styling review rather than production workflows.
- `v0.01` — Confirms that the existing left and right activity bars, mirrored sidebars, theme toggle, and manual-only verification remain in scope unless explicitly superseded below.

## 1. Overview

### 1.1 Purpose

This specification defines the next refinement of the temporary `WorkbenchHost` home page mock-up.

The purpose of this refinement is to evolve the existing shell into a more workbench-like desktop layout by adding top-level menu chrome, tabbed pane treatments, a lower center panel, and a Radzen component styling showcase in the upper center pane.

### 1.2 Scope

This specification currently includes:

- retaining the current Radzen-based Workbench shell direction on `Home.razor`
- retaining the existing left and right activity bars with independently controlled sidebars
- adding a full-width top application menu using `RadzenMenu` and `RadzenMenuItem`
- adding a lower center panel beneath the current middle area, separated by a user-resizable splitter
- adding a tab strip to each content-bearing section of the shell
- adding two or three tabs per section, with a close icon rendered on the right side of each tab header
- adding placeholder text content to the new lower center panel
- adding a collection of common Radzen form and data display components to selected tabs in the upper middle section for styling review
- using only sample in-memory data for all form, tree, and grid content
- keeping this work visual-first and mock-up focused rather than workflow or data driven
- continuing to rely on manual visual verification rather than new automated tests

This specification currently excludes:

- real application menu commands or keyboard shortcut implementation
- actual tab closing, tab persistence, or tab reordering behavior unless it is trivial and purely visual
- real data loading, persistence, or backend integration for the form showcase
- production information architecture for the pane contents
- responsive/mobile redesign work
- new automated tests, UI tests, or test scaffolding

### 1.3 Stakeholders

- Workbench developers shaping the shell experience
- contributors responsible for Blazor and Radzen UI composition in `WorkbenchHost`
- stakeholders reviewing the visual direction of the future desktop-style Workbench shell

### 1.4 Definitions

- `application menu bar`: the horizontal top menu running left to right above the shell body
- `upper middle pane`: the main central pane above the new lower panel
- `lower middle pane`: the new panel beneath the upper middle pane, separated by a splitter
- `section tab strip`: the top-aligned Radzen tab header area shown within a shell section
- `styling showcase`: a deliberately non-functional collection of representative Radzen controls used to assess look and feel

## 2. System context

### 2.1 Current state

`WorkbenchHost` already exposes a temporary Radzen-based desktop shell direction on `Home.razor`.

The current shell includes left and right edge activity bars, independently controlled sidebars, a minimal central area, and a temporary theme toggle. The current central area does not yet include a bottom panel. The page also does not yet expose a top application menu bar or consistent tabbed pane treatments across its content regions.

The current middle area remains deliberately simple, so the next refinement is now needed to evaluate how a denser, tabbed, tool-oriented Workbench layout feels before any real feature implementation begins.

### 2.2 Proposed state

In the proposed state after this work:

- the page presents a full-width `RadzenMenu` across the very top of the browser viewport
- the top menu runs left to right and includes standard placeholder entries such as `File`, `Edit`, `View`, and `Help`
- menu entries expose sensible placeholder submenu items, but do not need to perform real commands
- beneath the menu, the existing shell body remains edge-to-edge and keeps the current left and right activity bars and resizable sidebars
- the central area is split into an upper middle pane and a lower middle pane using a horizontal splitter so the user can resize the relative heights
- the lower middle pane is visible by default and contains placeholder text content for visual review
- each content-bearing section uses a top-aligned Radzen tab strip, including the left sidebar content area, the upper middle pane, the lower middle pane, and the right sidebar content area
- each section tab strip contains two or three tabs
- each tab header includes a close icon on the right side of the tab label
- the close icon is primarily a visual affordance for this mock-up and does not require production tab lifecycle behavior
- the upper middle pane hosts selected tabs that present a styling showcase of common Radzen form and data components
- the styling showcase uses static sample data, including representative sample items for a tree and a data grid
- the existing theme toggle remains available so the revised shell can still be reviewed in both light and dark themes
- the overall result reads as a richer desktop Workbench mock-up while remaining temporary and disposable

### 2.3 Assumptions

- `Home.razor` remains the intended page for the temporary shell refinement
- the current left and right activity bar mechanics remain valid and should be preserved unless directly superseded
- the top application menu is a visual application-menu placeholder rather than a functional command system
- standard menu headings such as `File`, `Edit`, `View`, and `Help` are sufficient for this iteration
- submenu entries may be arbitrary but should appear plausible for a desktop workbench application
- each content-bearing section should have its own tab strip even when the content is only placeholder material
- tab sets may be fixed rather than dynamically created
- the close icon on each tab may be decorative or no-op for now, provided it looks intentional and aligned to the right side of the tab header
- the lower middle pane is intended to help assess workbench density and should be present by default rather than collapsed
- the horizontal splitter should behave as a real resize affordance because layout feel is important to this iteration
- the upper middle pane is the best location for a styling showcase because it is the visual focus area of the shell
- the styling showcase should follow the Radzen guidance that prefers Radzen-native components over custom HTML controls
- the showcase may mix form controls and data-display controls because the purpose is visual review rather than a pure data-entry journey
- sample data may be hard-coded, in-memory, and arbitrary
- no persistence is required for menu selection, tab selection, splitter sizes, or form field values
- manual visual verification remains sufficient for this mock-up phase

### 2.4 Constraints

- the output must remain a single specification document in `docs/080-workbench-initial`
- the implementation must continue using Radzen in `WorkbenchHost`
- the existing left and right edge activity bars must remain anchored to the outer edges of the window
- the existing sidebars must continue to push and resize the center area rather than overlaying it
- the new application menu bar must sit above the current shell UI and span the full width of the window
- the new application menu bar must use `RadzenMenu` and `RadzenMenuItem`
- the central section must gain a new lower panel beneath the current middle content area
- a splitter must separate the upper and lower middle panes and allow user resizing
- each shell content section must include a top-aligned tab treatment
- each section must expose two or three tabs
- each tab must show a right-aligned close icon
- the upper middle pane must include a representative collection of common Radzen form components across a selection of tabs
- the upper middle pane sample content must include sample tree data and sample grid data
- all showcase data must remain static and non-sensitive
- this refinement must remain a visual mock-up and must not introduce real business workflows or backend dependencies
- no automated tests are required for this work

## 3. Component / service design (high level)

### 3.1 Components

1. `WorkbenchHost`
   - continues to host the Blazor UI and Radzen integration for the temporary shell mock-up

2. `Home.razor`
   - remains the main shell page
   - gains the top application menu, the vertically split center region, and tabbed pane treatments

3. `Top application menu`
   - provides desktop-style placeholder navigation using `RadzenMenu`
   - exposes standard menu headings and arbitrary submenu items

4. `Left sidebar content section`
   - continues to host placeholder content associated with the left activity bar
   - gains a tab strip with multiple fixed tabs and close icons

5. `Upper middle styling showcase`
   - becomes the main tabbed content pane in the center of the shell
   - hosts representative Radzen form, selection, tree, and data grid examples using sample data

6. `Lower middle panel`
   - provides a second central pane beneath the styling showcase
   - contains placeholder text content under its own tab strip

7. `Right sidebar content section`
   - continues to host placeholder content associated with the right activity bar
   - gains a tab strip with multiple fixed tabs and close icons

### 3.2 Data flows

#### Runtime interaction flow

1. the user opens the `WorkbenchHost` home page
2. the page renders the top menu, the left and right shell edges, and the split central area
3. the user can open left or right sidebars using the existing activity bars
4. the user can switch among the fixed tabs shown in each visible section
5. the user can resize the split between the upper and lower central panes
6. the upper middle tabs display sample Radzen controls and sample data for styling review
7. the lower middle panel displays placeholder textual content for layout review

### 3.3 Key decisions

- this work remains visual-first and does not depend on real Workbench functionality
- `RadzenMenu`, `RadzenTabs`, and other Radzen-native primitives are preferred over custom menu or tab markup
- the top menu and tab close icons are primarily shell affordances, not full workflow features
- the upper middle pane acts as a style gallery rather than a business form
- sample data remains static and disposable
- automated tests remain out of scope for this mock-up

## 4. Functional requirements

1. `Home.razor` shall retain the existing left and right activity bar shell behavior already established in the current mock-up.
2. The page shall provide a full-width application menu bar at the very top of the window above the current shell UI.
3. The application menu bar shall use `RadzenMenu` with nested `RadzenMenuItem` entries.
4. The application menu bar shall render left-to-right headings including `File`, `Edit`, `View`, and `Help`.
5. Each top-level menu heading shall expose one or more sensible placeholder submenu items.
6. The menu items may be visual placeholders and do not need to invoke real commands in this iteration.
7. The shell body beneath the menu bar shall remain edge-to-edge and desktop-oriented.
8. The central area shall be divided into an upper middle pane and a lower middle pane.
9. The upper and lower middle panes shall be separated by a visible splitter.
10. The splitter between the upper and lower middle panes shall support user resizing.
11. The lower middle pane shall be visible on initial page load.
12. The lower middle pane shall contain placeholder text content beneath its tab strip.
13. Each content-bearing shell section shall render a top-aligned tab strip.
14. The tabbed sections shall include the left sidebar content area, the upper middle pane, the lower middle pane, and the right sidebar content area.
15. Each section tab strip shall provide two or three tabs.
16. Each tab header shall include a close icon positioned to the right of the tab text.
17. The tab close icon may be a visual-only affordance and does not need to remove the tab in this iteration.
18. Each section shall present visibly different placeholder content or control groupings across its tabs.
19. The upper middle pane shall host a representative Radzen styling showcase across a selection of its tabs.
20. The upper middle styling showcase shall use Radzen-native components before any custom HTML controls are considered.
21. The upper middle styling showcase shall include common input controls such as text, multiline text, numeric, date, and selection controls.
22. The upper middle styling showcase shall include representative toggle or choice controls such as checkbox, switch, radio-style, or select-bar interactions.
23. The upper middle styling showcase shall include at least one sample file-oriented control such as file input or upload.
24. The upper middle styling showcase shall include a sample tree using arbitrary in-memory hierarchical data.
25. The upper middle styling showcase shall include a sample data grid using arbitrary in-memory row data.
26. The upper middle styling showcase may include validators, form fields, alerts, badges, or other common Radzen primitives where helpful for styling review.
27. The showcase data shall be static and shall not depend on backend calls.
28. The existing page theme toggle shall remain available so the revised shell can be reviewed in both light and dark themes.
29. The revised shell shall remain a temporary visual mock-up focused on layout and styling rather than business behavior.
30. No new automated tests shall be required for this work item.

## 5. Non-functional requirements

1. The mock-up should prioritize visual clarity, shell density review, and predictable pane mechanics.
2. The top menu, tab strips, and split center layout should read as a coherent desktop workbench composition.
3. The styling showcase should help reviewers compare Radzen control appearance in both light and dark themes.
4. The page should remain easy to revise as workbench look and feel evolves.
5. The UI should align with repository Blazor guidance, including explicit interactive render behavior where click and resize interactions are required.
6. The tab close icons and menu structures should remain visually accessible and discoverable even when their behavior is placeholder-only.

## 6. Data model

No persistent or business data model is required for this mock-up.

Any menu structure, tab metadata, form state, tree nodes, or data grid rows may use temporary in-memory view models only.

## 7. Interfaces & integration

1. `Radzen` integration
   - `WorkbenchHost` must continue to use the packages, services, assets, and host wiring needed for Radzen
   - the refinement should prefer `RadzenMenu`, `RadzenTabs`, `RadzenSplitter`, `RadzenTemplateForm`, `RadzenTree`, and `RadzenDataGrid` where appropriate

2. `Home.razor`
   - must host the revised top menu and tabbed pane layout
   - must remain the primary surface for the temporary Workbench shell review

## 8. Observability (logging/metrics/tracing)

No additional observability requirements are currently defined for this mock-up.

## 9. Security & compliance

1. The mock-up shall not introduce privileged behavior or business-specific data.
2. All sample menu items, form values, tree nodes, and data grid rows should remain generic and non-sensitive.

## 10. Testing strategy

1. Manual visual verification shall be sufficient for this work.
2. Verification should confirm the top application menu renders across the full width above the shell body.
3. Verification should confirm the menu headings and submenu items are visible and styled consistently.
4. Verification should confirm the existing left and right activity bar behaviors still work after the new top menu and lower panel are added.
5. Verification should confirm the central area is visibly split into upper and lower panes.
6. Verification should confirm the splitter between the upper and lower panes supports user resizing.
7. Verification should confirm the lower middle pane is visible by default and contains placeholder text.
8. Verification should confirm each shell section renders a tab strip at its top edge.
9. Verification should confirm each section exposes two or three tabs.
10. Verification should confirm each tab header shows a right-aligned close icon.
11. Verification should confirm the upper middle pane displays a representative collection of Radzen controls for styling review.
12. Verification should confirm the upper middle showcase includes sample tree data and sample data grid rows.
13. Verification should confirm the revised shell remains usable in both light and dark themes.
14. Verification should confirm the page remains an interactive Blazor page and that placeholder interactions do not require backend connectivity.
15. No new automated tests are required.

## 11. Rollout / migration

1. Refine the existing `Home.razor` mock-up rather than creating a separate page.
2. Use the revised menu, tabs, lower panel, and component showcase as the next disposable visual foundation for Workbench shell review.
3. Defer real commands, dynamic tabs, persisted layouts, and business workflows until the shell direction is agreed.

## 12. Open questions

No open questions are currently recorded.
