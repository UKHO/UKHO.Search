# Work Package: `076-primereact-theme-uplift` — PrimeReact generic density uplift and tab focus chrome cleanup

**Target output path:** `docs/076-primereact-theme-uplift/spec-frontend-primereact-theme-uplift_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for the next PrimeReact theme refinement pass.
- `v0.01` — Records the requirement to promote currently `Showcase`-local compact styling for grid and tree-style controls into the generic UKHO/Theia theme.
- `v0.01` — Records the requirement to remove the PrimeReact blue focus border around tab content while retaining the selected-tab underline and preserving keyboard usability.

## 1. Overview

### 1.1 Purpose

This specification defines a focused follow-on theme work package for the existing PrimeReact Theia system.

The purpose is to take styling decisions that currently exist only inside the `Showcase` surface and make them part of the shared UKHO/Theia PrimeReact theme so the same compact visual language is available across retained PrimeReact pages.

This work package also corrects an unwanted PrimeReact tab focus presentation in which a blue border appears around the tab content area. The desired end state is to keep the useful tab underline/selection treatment while removing the blue content-border chrome from the themed experience.

### 1.2 Scope

This specification includes:

- identifying compact density customisations that currently live in `Showcase` but are generic enough to belong in the shared theme
- moving those generic styling rules into the UKHO/Theia PrimeReact theme source
- removing or reducing the duplicated `Showcase`-specific styling once the theme owns it
- refining themed `TabView` or equivalent PrimeReact tab styling so focused tab content does not render an unwanted blue border around the panel content
- preserving the selected-tab underline treatment
- preserving an accessible visible keyboard-focus indication on the tab header itself rather than on the tab content container
- verifying the outcome across retained PrimeReact pages rather than only on `Showcase`

This specification does not include:

- new backend, service, or domain behavior
- a wider typography redesign beyond what is required to preserve the current accepted UKHO/Theia baseline
- layout-contract changes unless a narrow correction is required to separate layout styling from theme styling cleanly
- introduction of page-named selectors into shared theme source

### 1.3 Stakeholders

- Studio shell developers
- maintainers of the UKHO/Theia PrimeReact theme
- reviewers assessing visual consistency across retained PrimeReact pages
- future contributors creating additional PrimeReact pages inside the Studio shell

### 1.4 Definitions

- `generic theme rule`: a styling rule that belongs to reusable component presentation across multiple pages rather than to one named page
- `Showcase-local styling`: page-level CSS currently used only within the retained `Showcase` surface
- `compact density`: the tighter spacing, row height, padding, and control chrome that better matches the Theia desktop workbench feel
- `tab content focus border`: the blue border or outline that PrimeReact applies around the tab panel/content area in focused states
- `tab underline`: the visible underline or equivalent active indicator shown on the selected tab header

## 2. System context

### 2.1 Current state

The repository already contains the UKHO/Theia PrimeReact theme pipeline and separate layout contract described in the existing PrimeReact system work package.

The retained `Showcase` surface still contains customisations that make some controls feel more condensed and workbench-like than their equivalents on other retained pages. Those customisations are useful, but because they are still local to `Showcase`, the overall PrimeReact experience is not yet fully consistent across pages.

The controls most visibly affected by this issue are data-dense controls such as:

- grid/data-table surfaces
- tree and tree-table surfaces
- related headers, cells, row padding, node spacing, and nearby compact chrome where those rules are genuinely generic

The current tab styling also shows an unwanted PrimeReact blue focus border around the tab content area. That focus treatment reads as browser-style chrome rather than Theia-style themed UI. The selected-tab underline is still desirable and should remain.

### 2.2 Proposed state

In the proposed state, the shared UKHO/Theia theme becomes the authority for the compact density rules that are currently only visible in `Showcase`.

After implementation:

- generic density improvements for grid-, tree-, and other evidence-led component surfaces are available across retained PrimeReact pages by default
- `Showcase` no longer needs to carry those generic component-density overrides locally
- shared theme source remains generic and reusable rather than page-named
- the blue focus border around tab content is removed from the themed experience
- the selected-tab underline remains intact
- keyboard users still receive a visible focus cue on the tab header itself, or through another theme-appropriate header-level focus treatment, rather than through a border around the content panel

### 2.3 Assumptions

- the current `Showcase` compact treatment contains styling that is suitable for broader reuse when expressed as generic component-level theme rules
- the current accepted typography baseline from the PrimeReact system work remains valid and does not need a broad reset for this slice
- the blue border around tab content is a theme concern rather than a layout concern
- the desired tab experience is to keep active-state underlining while removing panel-border chrome that appears when tabs or tab panels receive focus
- accessibility must still be preserved, so removing the content border must not eliminate visible keyboard focus altogether

### 2.4 Constraints

- shared theme source must remain generic and must not accumulate `Showcase`-named selectors
- only genuinely reusable styling should move from `Showcase` into the theme
- layout mechanics must remain separate from the theme layer
- changes must work for both UKHO/Theia light and dark themes
- implementation work derived from this specification must comply fully with `./.github/instructions/documentation-pass.instructions.md`
- verification must cover retained pages beyond `Showcase` so the uplift does not overfit to one page

## 3. Component / service design (high level)

### 3.1 Components

1. `Shared UKHO/Theia PrimeReact theme source`
   - reusable light/dark theme source fragments
   - owner of generic component density, spacing, and focus chrome rules

2. `Generated theme outputs`
   - compiled UKHO/Theia light/dark theme assets consumed by Studio
   - must reflect the new condensed component rules and tab-focus refinement

3. `Showcase page-local styling`
   - current holder of some condensed component rules
   - should retain only page-specific layout or truly local exceptions after uplift

4. `Retained PrimeReact pages`
   - `Forms`, `Data View`, `Data Table`, `Tree`, `Tree Table`, and `Showcase`
   - validation surfaces for shared theme consistency

5. `PrimeReact tab styling`
   - active tab header treatment should keep the underline
   - focused content-panel border chrome should be removed
   - visible keyboard focus should remain on the header interaction surface

### 3.2 Data flows

#### Theme uplift flow

1. a contributor identifies compact component rules currently expressed in `Showcase`
2. those rules are classified as either generic theme concerns or page-local exceptions
3. generic rules are moved into shared theme source
4. the UKHO/Theia light and dark themes are rebuilt and deployed
5. retained pages render the condensed controls through the theme by default
6. `Showcase` keeps only the local styling that does not belong in the shared theme

#### Tab focus styling flow

1. a user navigates to a PrimeReact tab surface
2. the selected tab continues to show the expected underline
3. when focus moves onto the tab control, the theme suppresses the blue border around the tab content area
4. focus remains visibly understandable through the tab header treatment rather than through a border around the content panel

### 3.3 Key decisions

- styling authority for condensed generic controls belongs in the shared UKHO/Theia theme, not in `Showcase`
- only evidence-led, reusable component styling should be uplifted
- page-local layout behavior remains outside the theme
- the selected-tab underline remains part of the intended tab experience
- the content-panel blue focus border is undesirable themed chrome and should be removed
- visible keyboard focus must still be preserved in a header-level or equally clear theme-appropriate way

## 4. Functional requirements

### 4.1 Generic component density uplift

1. The implementation shall review the current `Showcase` compact styling and identify rules that are genuinely reusable across retained PrimeReact pages.
2. The implementation shall move reusable condensed styling for data-dense component families into the shared UKHO/Theia theme source.
3. The implementation shall treat grid-, data-table-, tree-, and tree-table-related compact rules as primary candidates for uplift where evidence shows they are generic.
4. The implementation shall consider related component chrome such as headers, body cells, row padding, node spacing, filter/control spacing, and similar density-affecting details when those rules are generic.
5. The implementation shall avoid moving page-structure or page-layout rules into the theme.
6. The implementation shall avoid adding `Showcase`-named selectors to shared theme source.
7. Once generic rules are owned by the theme, the implementation shall remove or reduce the corresponding duplicated `Showcase`-local styling.
8. `Showcase` shall continue to render acceptably after the generic rules are removed from its local styling because the theme now provides them.
9. The uplifted component styling shall be available to retained PrimeReact pages beyond `Showcase` without requiring page-specific opt-ins.

### 4.2 Tab focus chrome refinement

10. The implementation shall remove the blue border, outline, or equivalent focus chrome that PrimeReact applies around tab content or tab panel content in focused states.
11. The implementation shall retain the selected-tab underline or equivalent active-state tab-header treatment.
12. The implementation shall preserve a visible focus indicator for keyboard users on the tab header itself, or through another theme-appropriate tab-level focus treatment that does not draw a border around the content panel.
13. The implementation shall ensure that removing the tab content border does not make tab keyboard focus ambiguous.
14. The tab focus styling shall be implemented through the shared theme so the result is consistent across retained PrimeReact tab surfaces.

### 4.3 Theme ownership and reuse

15. The shared theme source shall remain the authority for reusable PrimeReact component styling.
16. The implementation shall keep page-local CSS only for layout mechanics or truly narrow page-specific exceptions.
17. The uplift shall work in both the UKHO/Theia light and UKHO/Theia dark theme variants.
18. The implementation shall rebuild and redeploy generated theme outputs after the theme source changes.

### 4.4 Cross-page verification

19. Verification shall include `Showcase` plus at least the retained data-dense surfaces affected by the uplift, including `Data Table`, `Tree`, and `Tree Table`.
20. Verification shall also include at least one non-data-dense retained page such as `Forms` or `Data View` to confirm the generic theme uplift does not make other pages worse.
21. Verification shall confirm that the condensed styling reads coherently with the surrounding Theia shell rather than as a `Showcase`-only look.
22. Verification shall confirm that the tab underline remains visible and that the content-panel blue border no longer appears.

## 5. Non-functional requirements

1. The resulting UI should feel more consistent across retained PrimeReact pages because shared component density no longer depends on `Showcase`-local CSS.
2. The generic theme should remain maintainable and avoid page-specific selectors.
3. The refined tab styling should feel more Theia-aligned and less like browser-default chrome.
4. Keyboard accessibility should remain understandable after the tab content border is removed.
5. The light and dark theme variants should remain visually coherent after the uplift.
6. The change should remain narrowly scoped to real generic component styling and tab-focus chrome rather than reopening a broad visual redesign.

## 6. Data model

No domain, persistence, or API data-model changes are required.

The only affected assets are frontend theme and styling assets, including:

- shared UKHO/Theia PrimeReact theme source
- light and dark theme variant source
- generated theme outputs
- retained page-local CSS where duplicated generic rules are removed
- verification tests or checks related to theme output and runtime behavior

## 7. Interfaces & integration

### 7.1 Repository integration points

Expected implementation areas include:

- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/shared/`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-light/`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-dark/`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/`
- `src/Studio/Server/search-studio/src/browser/primereact-demo/search-studio-primereact-demo-widget.css`
- retained PrimeReact page or tab styling only where duplicated generic rules must be removed from `Showcase`

### 7.2 Behavioral integration

- retained PrimeReact pages should receive the condensed component styling through theme loading rather than through page-local overrides
- PrimeReact tab surfaces should receive consistent tab-header and tab-panel focus styling through the shared theme
- the existing theme build, deploy, and runtime-loading flow remains the mechanism by which the new styling becomes active in Studio

## 8. Observability (logging/metrics/tracing)

No new product telemetry is required for this slice.

Implementation should preserve any existing logging or verification output already used by the theme build and deploy workflow. If regression checks exist for generated theme outputs or runtime theme selection, they should be updated or extended rather than replaced.

## 9. Security & compliance

No new security boundary or compliance model is introduced.

Accessibility remains the main quality concern for this slice. Removing the blue tab content border must not remove visible keyboard-focus feedback entirely. The implementation should keep a clear focus indication on the interactive tab header surface or an equivalent theme-appropriate focus treatment.

## 10. Testing strategy

The implementation derived from this specification should use focused regression coverage plus practical visual verification.

Expected verification includes:

- rebuilding generated UKHO/Theia light and dark themes
- validating that generated outputs contain the intended shared theme refinements
- validating runtime loading of the updated themes in Studio
- checking retained pages including `Showcase`, `Data Table`, `Tree`, `Tree Table`, and at least one of `Forms` or `Data View`
- confirming that the selected-tab underline remains present
- confirming that the blue border around tab content no longer appears in focused states
- confirming that keyboard focus is still visually understandable on the tab control
- running the standard frontend tests and browser build used by the existing PrimeReact system workflow

## 11. Rollout / migration

This work is a small refinement pass within the existing PrimeReact system and does not require a staged user rollout.

Expected execution sequence:

1. identify reusable condensed `Showcase` rules
2. move those rules into shared theme source
3. remove duplicated local `Showcase` rules
4. implement the tab content focus-border removal in the theme while retaining the active underline and visible keyboard focus
5. rebuild and deploy the light/dark themes
6. run tests and browser build
7. visually verify the retained pages inside Studio

## 12. Open questions

1. The implementation should confirm exactly which remaining `Showcase` compact rules are truly generic and which should stay local to `Showcase`.
2. If the current active-tab underline alone is not sufficient as a keyboard-focus indicator for inactive but focused tabs, the implementation should define a subtle header-level focus treatment that remains visible without reintroducing panel-border chrome.
3. The implementation should confirm whether any additional compact component families beyond data table and tree controls warrant uplift in the same pass once the affected retained pages are reviewed.
