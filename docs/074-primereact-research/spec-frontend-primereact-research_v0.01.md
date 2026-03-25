# Work Package: `074-primereact-research` — Temporary PrimeReact demo pages for Theia evaluation

**Target output path:** `docs/074-primereact-research/spec-frontend-primereact-research_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft capturing the current UX research direction for evaluating PrimeReact inside Theia using temporary demo pages.
- `v0.01` — Recorded the current preference to avoid PrimeReact dialogs and popups for now because Theia already provides workbench-native shell interactions.
- `v0.01` — Recorded the current preference to drop PrimeReact `Terminal` from scope because it is not a real shell-backed terminal and is not the current evaluation target.
- `v0.01` — Recorded the intent to include PrimeReact `Tree` on at least one demo page as part of the research set.
- `v0.01` — Recorded the preference to compare themed and unthemed rendering through a per-page toggle or checkbox unless implementation complexity proves disproportionate.
- `v0.01` — Recorded that all demo pages are temporary evaluation assets and are expected to be removed later.
- `v0.01` — Clarified that the demo pages should be exposed in the same pattern as the existing home page: available from the `View` menu but not shown by default.
- `v0.01` — Clarified that `.github/instructions/primereact.instructions.md` is included as non-normative reference material and a lookup source for implementation guidance, not as a set of project requirements.

## 1. Overview

### 1.1 Purpose

This work package defines a temporary frontend research spike to evaluate whether PrimeReact is a good fit for richer application-style UI inside the Theia-based Studio shell.

The purpose is not to redesign the shell or replace Theia-native workbench primitives wholesale. The purpose is to create a small set of disposable demo pages that can be opened inside Theia so the team can assess how PrimeReact feels in practice for richer controls that Theia does not provide out of the box.

The research focus is on embedded page-level controls rather than shell-level interactions. PrimeReact is therefore being considered as a component library for content surfaces inside Theia, not as a replacement for Theia’s own dialogs, popups, or real terminal subsystem.

### 1.2 Scope

This specification covers:

- creation of a temporary set of PrimeReact demo pages hosted within the Theia shell
- comparison of PrimeReact `styled` and `unstyled` presentation on the same demo surfaces
- inclusion of representative richer controls such as forms, table/grid-style content, and tree-based content
- use of disposable sample content sufficient to evaluate UX, interaction, density, and visual fit
- explicit treatment of the demo pages as temporary research assets to be removed after evaluation

This specification does not cover:

- migration of product UI to PrimeReact
- replacement of Theia-native dialogs, popups, or workbench shell primitives
- replacement of the Theia-backed real terminal
- final design-system decisions or production styling decisions
- permanent information architecture or navigation decisions beyond what is needed to expose the demos for evaluation

### 1.3 Stakeholders

- Studio shell developers
- product and UX reviewers evaluating richer UI options inside Theia
- maintainers of the Theia-based frontend shell
- developers responsible for future page-level Studio experiences that may benefit from richer controls

### 1.4 Definitions

- `PrimeReact styled mode`: PrimeReact rendered with its built-in theme and component styles enabled
- `PrimeReact unstyled mode`: PrimeReact rendered as a behavior/accessibility engine with host-controlled styling
- `demo page`: a temporary page or view used only to assess the practical fit of PrimeReact controls within Theia
- `temporary research asset`: an implementation artifact expected to be deleted after the evaluation concludes

## 2. System context

### 2.1 Current state

The current discussion identifies a practical UX gap: Theia is strong as a workbench shell, but does not provide especially rich page-level controls out of the box for scenarios such as grids and similar business UI.

The current working assumptions from the discussion are:

- PrimeReact should be treated as a possible page-level component engine inside Theia
- the safest default for an embedded host shell remains `unstyled` mode
- the team still wants to see both themed and unthemed behavior in practice before deciding
- Theia-native dialogs and popups are likely preferable to PrimeReact overlays for this solution and are therefore not the current evaluation focus
- PrimeReact `Terminal` is no longer in scope for the demos because it is not a real shell terminal and does not address the actual terminal requirement
- PrimeReact `Tree` is of interest as a likely easier page-level tree option than the Theia tree widget for some embedded content scenarios
- `PrimeReact TreeTable` is also in scope because hierarchy plus columns may be more realistic for some Studio-style data views

### 2.2 Proposed state

A temporary research surface shall be introduced inside the Theia shell consisting of one or more demo pages that exercise selected PrimeReact controls in realistic but disposable layouts.

The demos shall allow reviewers to compare themed and unthemed rendering during normal development use without needing a rebuild solely to switch between the two presentation modes, unless that proves materially more complex than expected.

The evaluation shall remain focused on page-level content inside Theia. Shell-level overlays and the real terminal experience are out of scope for this work item.

At minimum, the demo set should provide enough coverage to evaluate:

- simple controlled form inputs
- richer grid or tabular presentation using PrimeReact table-oriented components
- tree-based hierarchical presentation using PrimeReact `Tree`
- hierarchical plus column-based presentation using PrimeReact `TreeTable`
- card/list-style presentation using PrimeReact `DataView` or an equivalent list-oriented component
- a combined page showing how multiple PrimeReact controls feel together inside a workbench-hosted content surface

### 2.3 Assumptions

- Theia remains the host shell and continues to own shell-level UX conventions
- PrimeReact will be evaluated primarily for embedded content surfaces rather than workbench primitives
- `unstyled` mode is the likely production-leaning default for Theia integration, but `styled` mode is still worth evaluating as part of the research
- a runtime toggle between themed and unthemed modes is preferable to rebuild-only comparison if it can be achieved without disproportionate implementation effort
- the demo pages should use a mixed sample-data approach, combining generic examples with loosely Studio-shaped examples
- the `unstyled` presentation should loosely align with `Theia` while staying visually light, modern, and minimally boxed
- there is no strong requirement for toggle-state persistence, so the simplest practical behavior is acceptable
- the `styled` comparison should use standard stock PrimeReact light and dark themes rather than a custom styled theme
- when running in `styled` mode, the demo should automatically follow the current `Theia` light or dark theme using the corresponding stock PrimeReact theme
- separate fixed light and dark review passes are not required as long as styled mode follows Theia theme changes dynamically
- the per-page mode toggle is intended to switch PrimeReact itself between styled and unstyled mode rather than to restyle surrounding page chrome
- the same page content should be reused in both styled and unstyled modes so visual comparison remains like-for-like
- layout evaluation should include resizing interactions such as draggable `Splitter` panes
- desktop/workbench width is sufficient for this research and narrower responsive behavior does not need to be a focus
- demo-page groupings should be chosen pragmatically and sensibly because the implementation is temporary and optimized for quick look-and-feel evaluation
- there is no strong preference on page granularity, so the demo set may use the most practical mix of broader and narrower pages
- there is no strong preference on explanatory helper text within the demos, so implementation may choose the simplest practical approach
- there is no strong preference on demo page naming, so implementation may choose the most practical naming convention
- where minor presentation details remain unspecified, sensible defaults should be chosen in service of broad, useful feature coverage
- table-oriented demos should include basic interaction features rather than remaining visually static
- `.github/instructions/primereact.instructions.md` should be treated as reference material for how to implement PrimeReact components and where to look up official guidance, not as a requirements source for this work package
- the demos are intentionally temporary and may prioritise learning value over long-term maintainability
- the preferred exposure pattern is the same as the current home page: available from the `View` menu but not visible by default

### 2.4 Constraints

- the work package is documentation-first research and does not itself imply production adoption of PrimeReact
- the demos must be clearly temporary and easy to remove later
- PrimeReact dialogs, popups, and similar overlay-heavy patterns are out of scope for the initial evaluation
- PrimeReact `Terminal` is out of scope for the current evaluation set
- the spec should remain a single markdown document for this work package
- a themed/unthemed checkbox or toggle should appear on each demo page unless that becomes too difficult for the value returned

## 3. Component / service design (high level)

### 3.1 Components

1. `Temporary demo surface host`
   - a temporary way to expose one or more PrimeReact evaluation pages inside Theia
   - should follow the same pattern as the existing home page by being available from the `View` menu without being shown by default
   - exists only for the duration of the research

2. `Demo page set`
   - one or more pages containing representative PrimeReact controls
   - pages do not need a finalised information architecture
   - page count and control distribution can remain flexible
   - groupings should be chosen pragmatically to get a useful range of styled and unstyled PrimeReact controls on screen quickly

3. `Mode toggle`
   - a per-page checkbox or toggle to switch between themed and unthemed presentation
   - intended to support fast comparison without rebuild where practical
   - intended to switch PrimeReact itself between styled and unstyled mode on that page
   - may persist only for the session or through simple client-side storage if useful

4. `Form controls demo`
   - a simple page or section showing controlled PrimeReact input controls
   - intended to evaluate density, labeling, alignment, and general embedded page fit
   - should include validation states and simple inline feedback
   - should include a broad range of representative form controls such as text, dropdown, autocomplete, date, checkbox, radio, and multi-select where practical

5. `Grid/table demo`
   - a page or section using PrimeReact table-oriented components such as `DataTable`
   - intended to evaluate whether PrimeReact meaningfully closes the “richer control” gap inside Theia
   - should include basic interaction features such as sorting, filtering, row selection, and pagination
   - should include editable states such as inline editing where practical

6. `Tree demo`
   - a page or section using PrimeReact `Tree`
   - intended to evaluate whether page-level hierarchical content is easier and more productive to build with PrimeReact than with the Theia tree widget for non-shell scenarios
   - should include basic interactions and simple actions such as expand/collapse, selection, and lightweight toolbar actions

7. `TreeTable demo`
   - a page or section using PrimeReact `TreeTable`
   - intended to evaluate hierarchical data with columns where a plain tree may be too limited for Studio-style scenarios
   - should include basic interactions and simple actions where appropriate

8. `Card/list demo`
   - a page or section using card/list-style presentation such as `DataView`
   - intended to evaluate non-tabular item presentation, density, and visual fit alongside grid-based approaches

9. `Combined realistic demo`
   - a page or composite layout combining filters, a table, and tree content or similar representative controls
   - intended to show a wider variety of controls together so overall PrimeReact fit inside a Theia-hosted page can be judged
   - may include simple edit/detail panel scenarios to help evaluate editable states

10. `Layout and container demo elements`
   - layout and container components such as `Tabs`, `Splitter`, `Panel`, and `Divider`
   - intended to evaluate whether PrimeReact supports clean modern layout composition inside Theia effectively
   - should emphasize more spacious modern layouts rather than compact dense layouts
   - should include resizing interactions such as draggable `Splitter` panes

### 3.2 Data flows

#### Demo rendering flow

1. the user opens a temporary demo page inside Theia
2. the page renders sample PrimeReact controls using representative demo data
3. the user interacts with the page to assess layout, control density, keyboard feel, and visual fit
4. the user can toggle themed versus unthemed presentation on that page where supported
5. the same page content re-renders in the alternate presentation mode without a rebuild where practical

#### Research feedback flow

1. reviewers interact with the temporary demo pages
2. reviewers compare embedded page UX against Theia-native capabilities and expectations
3. the team determines whether PrimeReact is suitable for future page-level Studio features
4. the temporary pages are later removed after the decision is made

### 3.3 Key decisions

- PrimeReact is being evaluated as an embedded content library inside Theia, not as a replacement for Theia’s shell UX
- the research should avoid dialogs and popups for now because Theia likely already serves that shell concern better
- PrimeReact `Terminal` is excluded because it is not a real backend terminal and does not match the intended use
- PrimeReact `Tree` should be included because it is a plausible candidate for simpler embedded hierarchical content
- `unstyled` mode is the default architectural preference, but side-by-side practical comparison with `styled` mode remains valuable
- all demo pages are temporary and should be designed to be deleted after research completion

## 4. Functional requirements

### 4.1 Demo presence

1. The solution shall provide a temporary set of PrimeReact demo pages inside Theia for UX evaluation.
2. The solution shall provide at least one demo surface for simple controlled form inputs.
3. The solution shall provide at least one demo surface for richer tabular or grid-style content.
4. The solution shall provide at least one demo surface containing PrimeReact `Tree`.
5. The solution shall provide at least one demo surface containing PrimeReact `TreeTable`.
6. The solution shall provide at least one demo surface containing card/list-style presentation such as `DataView`.
7. The solution shall include layout and container components such as `Tabs`, `Splitter`, `Panel`, and `Divider` within the demo set.
8. The solution may combine multiple control categories on the same page where that is more practical than creating separate pages.

### 4.2 Styled versus unstyled comparison

6. Each demo page shall expose a themed/unthemed checkbox or equivalent toggle unless implementation complexity proves disproportionate.
7. The preferred implementation shall allow switching between themed and unthemed presentation without rebuilding the application.
8. Each demo page shall keep its own themed/unthemed toggle rather than relying on a single shared global toggle.
9. If a no-rebuild mode switch proves impractical, the implementation shall record the limitation explicitly and use the simplest workable fallback that still preserves both presentation modes where practical.
10. The per-page toggle shall switch PrimeReact itself between styled and unstyled mode and is not required to restyle surrounding non-PrimeReact page chrome.
11. The same page content should be reused in both styled and unstyled modes so reviewers can compare like-for-like presentations.

### 4.3 Temporary/disposable nature

9. The demo pages shall be implemented as temporary research assets.
10. The demo pages shall be available from the `View` menu in the same general way as the existing home page and shall not be shown by default.
11. The demo pages shall use disposable sample content and interactions sufficient for evaluation.
12. The implementation shall be easy to remove after the research has concluded.
13. Demo-page groupings may be chosen pragmatically rather than architected for long-term maintainability, provided they still support broad and useful UX evaluation.

### 4.4 Evaluation intent

13. The form-oriented demo content shall be sufficient to assess labels, spacing, density, and general page ergonomics inside Theia.
14. The form-oriented demo content shall include validation states and simple inline feedback so reviewers can assess error presentation and field guidance.
15. The form-oriented demo content should include a broad range of representative form controls rather than a minimal subset.
16. The table/grid-oriented demo content shall be sufficient to assess whether PrimeReact offers a materially better page-level grid experience than current Theia-native options.
17. Table and tree demo datasets shall be large enough to exercise scrolling behavior and visual density.
18. The tree-oriented demo content shall be sufficient to assess whether PrimeReact `Tree` is easier or more suitable than Theia’s tree widget for non-shell page content.
19. The tree-oriented demo content shall include basic interactions and simple actions such as expand/collapse, selection, and lightweight toolbar actions.
20. The `TreeTable` demo content shall be sufficient to assess whether PrimeReact `TreeTable` is a better fit than plain tree presentation for hierarchical Studio-style data with columns.
21. Card/list-style demo content shall be sufficient to assess whether non-tabular item presentation such as `DataView` feels useful and visually compatible inside Theia.
22. The demos shall include editable states such as inline row editing or edit/detail panel scenarios so editing UX can be assessed.
23. At least one demo surface should show multiple PrimeReact controls together so reviewers can assess how coherent the overall page experience feels.
24. The demo content should use a mixed sample-data approach so that some surfaces feel generic and some feel loosely aligned to Studio concepts.
25. At least one temporary demo page shall explicitly combine `Tree`, table/grid content, and form controls on the same page.
26. Where practical, the demo set should favour broader control and feature coverage so reviewers can judge fit across a wide range of PrimeReact capabilities.
27. Where minor demo-shape decisions remain unspecified, sensible defaults may be used as long as they support broad and useful UX evaluation.

### 4.5 Explicit exclusions

28. PrimeReact dialogs, popup menus, and similar overlay-heavy controls shall not be part of the initial demo scope.
29. PrimeReact `Terminal` shall not be part of the initial demo scope.
30. The demos shall not attempt to replace Theia’s real terminal subsystem.

## 5. Non-functional requirements

1. The demo pages should load and respond fast enough for normal interactive UX evaluation in local development.
2. The implementation should minimise disruption to the existing Theia shell and avoid broad architectural changes.
3. The controls should preserve basic accessibility practices, including labels and keyboard-safe interaction patterns where applicable.
4. The styled/unstyled switch should be simple and obvious to reviewers.
5. The demo implementation should favour low-friction removal once the research exercise is complete.
6. The `unstyled` demos should aim for a clean modern appearance, closer to Visual Studio than to heavily bordered enterprise admin UI.
7. The `unstyled` demos should avoid unnecessary borders, panels, and box-heavy composition unless a specific control requires them for clarity.
8. The `styled` demos should use standard stock PrimeReact light and dark themes for comparison rather than introducing custom themed styling.
9. When `Theia` is in light or dark mode, the `styled` demos should automatically use the corresponding stock PrimeReact light or dark theme.
10. Styled-mode evaluation may rely on normal Theia theme switching at runtime rather than requiring separate dedicated light-theme and dark-theme review flows.

## 6. Data model

No production data model changes are required for this work package.

The demos may use lightweight sample view models sufficient to represent:

- form field values
- table rows and columns at scales sufficient to exercise scrolling and density
- tree nodes at scales sufficient to exercise scrolling and density
- hierarchical row structures for `TreeTable` at scales sufficient to exercise scrolling and density
- card/list item datasets sufficient to assess non-tabular presentation density and selection states
- mode-toggle state for themed versus unthemed rendering

The sample data approach should be mixed, combining:

- generic demo-friendly examples for neutral control evaluation
- loosely Studio-shaped examples where that helps assess practical fit inside the Theia shell

## 7. Interfaces & integration

1. The demos shall be hosted within the existing Theia shell experience.
2. The integration should expose the temporary demo pages through the `View` menu using the same general pattern as the existing home page rather than making them visible by default.
3. The demos may use mock data or in-memory data sources because backend integration is not required for the UX research goal.
4. The styled/unstyled comparison should preferably be driven by runtime UI state rather than a rebuild-only configuration step.
5. `.github/instructions/primereact.instructions.md` should be treated as a non-normative reference source for PrimeReact implementation guidance, component-selection suggestions, and links to official documentation.

## 8. Observability (logging/metrics/tracing)

Formal observability changes are not required for this temporary UX research work.

Lightweight diagnostic logging may be used only if needed to confirm that the runtime mode toggle or demo-page activation behaves as expected during development.

## 9. Security & compliance

1. The work package should avoid introducing unnecessary backend integration or privileged operations.
2. Sample data used by the demos shall be non-sensitive.
3. Because the demos are temporary research assets, security scope should remain minimal and proportionate.

## 10. Testing strategy

1. Validation for this work package should focus on manual UX review inside Theia.
2. Reviewers should confirm that the demo pages can be opened reliably.
3. Reviewers should confirm that the themed/unthemed checkbox or toggle works on each page where implemented.
4. Reviewers should confirm that the form, table, and tree demonstrations are sufficient to support a decision on PrimeReact suitability.
5. Reviewers should confirm that the demo pages feel like embedded content inside Theia rather than conflicting with the shell.

## 11. Rollout / migration

1. Introduce the temporary PrimeReact demo pages only for evaluation.
2. Review the demos with stakeholders and capture the conclusions.
3. Decide whether PrimeReact should be adopted for future page-level experiences inside Theia.
4. Remove the temporary demo pages after the research outcome is known.

## 12. Open questions

None at present. Remaining minor implementation choices may use sensible defaults provided they preserve the documented priorities of broad control coverage, like-for-like styled/unstyled comparison, and temporary/removable demo scope.
