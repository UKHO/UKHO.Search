# Work Package: `074-primereact-research` — PrimeReact showcase page desktop-app tidy-up

**Target output path:** `docs/074-primereact-research/spec-frontend-primereact-showcase-desktop-tidy-up_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft for a focused tidy-up of the PrimeReact showcase page only.
- `v0.01` — Recorded the requirement to make the showcase feel like a compact desktop/workbench surface rather than a spacious web page.
- `v0.01` — Recorded the requirement to use PrimeReact scaling guidance to reduce typography and spacing to a more desktop-like density.
- `v0.01` — Recorded the requirement to remove visually distracting boxed feature sections and prefer cleaner, flatter composition.
- `v0.01` — Recorded the requirement to establish single-owner scrolling so either Theia or the individual PrimeReact controls scroll, but never both for the same content region.

## 1. Overview

### 1.1 Purpose

This specification defines a focused UX refinement of the temporary PrimeReact showcase page inside the Theia-based Studio shell.

The purpose is to keep the showcase page useful as a PrimeReact evaluation surface while making it feel substantially more like a desktop application view and much less like a marketing-style or dashboard-style web page.

The showcase page should read as a dense workbench surface:

- compact typography
- restrained spacing
- minimal decorative chrome
- strong information density
- clear pane ownership
- predictable scroll behavior

This is a tidy-up and rebalancing exercise, not a redesign of the wider temporary PrimeReact research package.

### 1.2 Scope

This specification covers only the combined PrimeReact showcase page and the local styling, layout, and control sizing decisions needed to improve its desktop-app feel.

This specification includes:

- compacting the showcase page typography and spacing
- applying PrimeReact scaling guidance so component sizing feels more like a desktop application than a web page
- removing unnecessary boxes, cards, panels, and other visually heavy framing around feature areas
- simplifying the page composition so it behaves like a workbench document rather than a long landing page
- defining clear scroll ownership so the page avoids double-scroll regions and competing scrollbars
- refining the layout of the tree, grid, and detail regions so larger controls use their own scrollbars where appropriate

This specification does not cover:

- changes to the other PrimeReact research demo pages
- introduction of new PrimeReact components beyond what the showcase already uses
- wider Theia shell navigation or menu changes
- backend integration or production workflow behavior
- redesign of the overall PrimeReact research work package

### 1.3 Stakeholders

- Studio shell developers
- reviewers evaluating PrimeReact inside Theia
- UX and product stakeholders assessing whether PrimeReact can support application-style surfaces
- maintainers of the temporary PrimeReact research package

### 1.4 Definitions

- `desktop-app feel`: a UI presentation closer to an IDE or workbench tool window than to a content-heavy website, using smaller scale, tighter rhythm, and less decorative framing
- `showcase page`: the combined PrimeReact demo page that places hierarchy, grid, and detail/edit surfaces together inside one view
- `page chrome`: non-essential visual framing such as large hero sections, heavy cards, excessive padding, shadows, and boxed feature containers
- `single-owner scrolling`: a layout rule where each content region has one obvious scroll container, avoiding nested competing scrollbars for the same interaction path
- `PrimeReact scale`: the component sizing approach described by PrimeReact theming guidance, typically applied through theme/root scale rather than ad hoc per-component size overrides

## 2. System context

### 2.1 Current state

The current showcase page succeeds in putting multiple PrimeReact controls together on one surface, but it still presents more like a web page than a desktop workbench view.

The current implementation characteristics include:

- generous outer page padding
- large section gaps
- relatively large heading and body text
- repeated bordered surface containers
- visible card-like framing around many feature areas
- visual emphasis on boxed sections rather than on the data and controls themselves
- large minimum heights that can make the page feel oversized
- a layout that reads like stacked or boxed content regions rather than docked application panes

The current page therefore has several UX drawbacks for the research goal:

- it wastes too much space
- it lowers visible information density
- it makes the showcase feel more like a demo microsite than a real desktop tool surface
- it introduces too much decorative framing around content
- it risks creating awkward vertical and horizontal overflow combinations when the host area is constrained

The current research direction for the wider PrimeReact work package is already styled PrimeReact only. This tidy-up therefore applies to the styled showcase experience and should not reintroduce unstyled/styled comparison requirements.

### 2.2 Proposed state

The showcase page shall be reworked into a compact, flatter, more workbench-like surface that prioritises dense utility over decorative presentation.

In the proposed state:

- PrimeReact sizing shall be reduced using the documented PrimeReact scaling approach rather than by scattered one-off component tweaks
- headings, labels, controls, paddings, and gaps shall all move to a smaller and more consistent compact baseline
- large boxed feature sections shall be removed or greatly reduced
- `Panel`-style framing shall not be used as the primary visual structure for the page
- `Divider` may be used sparingly where a visual break is needed, but it shall not add large amounts of whitespace
- the page shall read as a set of practical working panes rather than a sequence of cards
- the hierarchy, grid, and detail regions shall have deliberate size ownership so large datasets scroll in the relevant control rather than forcing both the page and the control to scroll for the same content
- the preferred outcome is that the showcase fits within the Theia content area as a stable page layout and that internal controls own scrolling where necessary

The result should feel closer to a desktop review tool, with information and actions immediately visible and less visual noise between them.

### 2.3 Assumptions

- The showcase page remains a temporary research surface hosted inside Theia.
- The page continues to use full styled PrimeReact only.
- The existing showcase content model remains broadly valid: hierarchy, grid, and detail/edit regions remain the core of the page.
- The work should prefer structural simplification over decorative polish.
- The page is primarily reviewed in desktop/workbench widths rather than mobile widths.
- PrimeReact theming guidance provides a supported way to reduce overall component scale, and that mechanism should be preferred over piecemeal manual shrinking.
- Minor page text and section labels may be simplified if that helps the page feel more tool-like.
- The best outcome is a stable non-scrolling page shell with internal scroll ownership delegated to the larger tree and grid/detail regions.

### 2.4 Constraints

- The change must focus solely on the showcase PrimeReact demo page.
- The wider demo set and menu structure are out of scope.
- The tidy-up must not introduce a second competing design language on top of styled PrimeReact.
- The page must avoid decorative spacing and framing that works against a desktop-app feel.
- The implementation must avoid dual scroll ownership for the same region.
- Where a choice is required, compactness and clarity take precedence over showcase-style visual flourish.

## 3. Component / service design (high level)

### 3.1 Components

1. `Showcase page shell`
   - the outer layout container for the combined page
   - should behave like a workbench document surface rather than a landing page
   - should use a compact outer margin and avoid oversized hero treatment

2. `PrimeReact density/scaling layer`
   - the page-scoped application of PrimeReact scaling guidance
   - should set a smaller baseline for text and component sizing
   - should be the primary mechanism for achieving a desktop feel

3. `Hierarchy pane`
   - the tree-oriented region of the showcase
   - should look like a working navigation/content pane, not a boxed feature card
   - should own its own vertical scrolling if the node set exceeds the available height

4. `Grid pane`
   - the data table region of the showcase
   - should use the available height efficiently and keep headers/controls compact
   - should own its own scrolling for larger datasets when required

5. `Detail pane`
   - the edit/detail region associated with the current selection
   - should be visually integrated into the page layout without heavy container framing
   - should remain compact and readable with smaller form controls and less padding

6. `Lightweight separators`
   - optional use of `Divider` or equivalent subtle separation
   - should support readability only where needed
   - should not recreate boxed sections or wide whitespace bands

7. `Scroll ownership contract`
   - the page-level rule set defining which region may scroll
   - should ensure Theia and a child control do not both scroll for the same interaction path
   - should prefer internal control scrolling once the shell layout is established

### 3.2 Data flows

#### Layout/render flow

1. the user opens the showcase page from Theia
2. the page applies the compact PrimeReact scale and compact page rhythm
3. the page renders a stable multi-region layout for hierarchy, grid, and detail content
4. the user interacts with the controls without the page feeling oversized or over-framed
5. large data regions scroll within their owning controls when content exceeds available space

#### Resize/overflow flow

1. the Theia content area changes size
2. the showcase layout reflows within the available workspace area
3. the page shell should remain stable and avoid introducing unnecessary outer scrolling where possible
4. if a region cannot show all of its content, the owning control or pane provides the scrollbar
5. a single region must not require both outer-page scrolling and inner-control scrolling to reach the same content set

### 3.3 Key decisions

- The tidy-up applies to the showcase page only.
- Styled PrimeReact remains the only supported presentation mode for this work item.
- PrimeReact scale must be reduced through the documented theming/scale mechanism rather than ad hoc random overrides.
- The showcase should stop presenting as a series of boxed demo sections.
- `Panel`-like framing and large card surfaces should not be the default structure of the page.
- `Divider` may be used, but only as a minimal separator.
- Internal panes and large controls should normally own their own scrolling.
- The page should look and behave more like an IDE tool view than a promotional demo page.

## 4. Functional requirements

### 4.1 Scope and page focus

1. The implementation shall focus solely on the PrimeReact showcase demo page.
2. The implementation shall not require redesign of the other PrimeReact demo pages.
3. The existing showcase content categories shall remain present: hierarchy, grid, and detail/edit content.
4. The page may simplify or shorten supporting explanatory text where that helps the page feel more like a tool surface.

### 4.2 Density and scale

5. The showcase page shall adopt a smaller visual scale using the PrimeReact theming/scaling approach referenced by the PrimeReact documentation.
6. The compact scale shall apply consistently across typography, component sizing, spacing, and rhythm for the showcase page.
7. The implementation shall prefer a single page-level scale adjustment over many isolated per-control size hacks.
8. Headings, labels, helper text, and body text shall be reduced from the current oversized presentation to a compact desktop-like baseline.
9. Outer page padding and section gaps shall be reduced materially from the current spacious layout.
10. The page shall avoid oversized hero treatment, oversized summary regions, or oversized headline typography.

### 4.3 Visual structure and chrome reduction

11. The showcase page shall stop using boxes as the dominant visual separator between features.
12. Decorative card-like surfaces, shadows, and bordered feature wrappers shall be removed or substantially reduced.
13. The layout shall prefer clean alignment, proximity, and subtle separation over boxed framing.
14. PrimeReact `Panel` shall not be used merely to draw a box around a feature area when a flatter structure is sufficient.
15. PrimeReact `Divider` may be used where a visual break is necessary, but it shall be used sparingly and without adding large whitespace.
16. The resulting page shall feel visually quieter and more compact than the current implementation.

### 4.4 Workbench-style layout

17. The showcase page shall be laid out as a practical application surface rather than as a vertically stacked web page.
18. The hierarchy, grid, and detail areas shall have clear pane roles and stable placement.
19. The layout should favour simultaneous visibility of multiple working regions over large decorative introductory sections.
20. Large controls shall be given space in proportion to their working value rather than their demo value.
21. The layout should fit within the normal Theia page area without requiring the user to treat the whole showcase like a long scrolling article.

### 4.5 Scroll ownership and overflow behaviour

22. For any major content region, scrolling shall have one clear owner.
23. The page shall not require both Theia page scrolling and internal control scrolling to reach the same content area.
24. Where the tree, data table, or detail region contains more content than can be displayed, the relevant pane or control shall own the scrollbar.
25. The preferred behaviour shall be for the showcase page shell to fit inside the Theia content area and for long data regions to scroll internally.
26. If full page fitting is not possible for a given viewport, the implementation shall still ensure that each region avoids double-scroll behaviour.
27. Horizontal scrolling, if needed, shall be owned by the relevant control region and not duplicated by the outer page.
28. Vertical scrolling, if needed, shall be owned either by Theia or by the relevant pane/control for that region, but never by both for the same region.
29. Splitter sizing, min-heights, wrappers, and overflow settings shall be adjusted so they reinforce single-owner scrolling rather than fighting it.

### 4.6 Control-specific expectations

30. The tree region shall remain usable at compact density and shall scroll within its own area when node content exceeds available height.
31. The data table region shall remain usable at compact density and shall scroll within its own area when row content exceeds available height.
32. The detail/edit region shall remain usable at compact density without large gaps between labels, controls, and actions.
33. Toolbars, filters, and compact metadata around the main controls shall use restrained spacing and shall not dominate the page.
34. The page shall preserve the existing ability to review combined PrimeReact interactions, but in a denser and less decorative layout.

## 5. Non-functional requirements

1. The page should feel immediately closer to a desktop application than to a web page on first open.
2. The compact density should improve information visibility without making controls unreadable.
3. The page should remain responsive to typical Theia workbench resizing.
4. The implementation should avoid fragile one-off CSS overrides where a supported PrimeReact scale approach can achieve the result more cleanly.
5. The visual treatment should remain aligned with styled PrimeReact and the Theia host theme.
6. The page should minimise non-essential shadows, borders, and radius treatments that increase visual noise.
7. The updated layout should reduce wasted space while keeping interaction targets practical.
8. The page should remain easy to review and easy to remove as part of the temporary research package.

## 6. Data model

No production or demo data model changes are required for this work item.

The tidy-up affects presentation and layout behaviour only.

Existing showcase data may continue to support:

- tree nodes
- grid rows
- selection state
- detail/edit form state
- scenario state

## 7. Interfaces & integration

1. The showcase page shall remain hosted inside the existing Theia shell integration.
2. The showcase entry point and menu exposure model are unchanged by this specification.
3. The implementation may adjust page-local CSS, layout containers, splitter configuration, and PrimeReact component sizing for the showcase page.
4. The implementation should use the PrimeReact-supported theming/scale approach as the primary sizing mechanism for the page.
5. The implementation shall not introduce new backend interfaces for this tidy-up.

## 8. Observability (logging/metrics/tracing)

Formal observability changes are not required.

If temporary diagnostic logging already exists for showcase interactions, it may remain, but this specification does not require new telemetry. Validation is primarily visual and behavioural.

## 9. Security & compliance

1. This work item should not introduce any new privileged operations.
2. This work item should not require any sensitive data.
3. Security scope remains unchanged because the tidy-up is a presentation-only refinement of an existing temporary demo page.

## 10. Testing strategy

1. Validation shall focus on the showcase page only.
2. Reviewers shall confirm that the page opens as before from Theia.
3. Reviewers shall confirm that the page uses visibly smaller text and tighter spacing than the current version.
4. Reviewers shall confirm that the page no longer feels dominated by cards, panels, or boxed feature regions.
5. Reviewers shall confirm that the page reads as a workbench-style surface rather than a web page.
6. Reviewers shall confirm that the tree, grid, and detail regions remain usable at the compact density.
7. Reviewers shall confirm that, for each major region, scrolling has one clear owner.
8. Reviewers shall confirm that no major region requires double scrolling to access the same content.
9. Reviewers shall confirm that any necessary horizontal overflow is handled by the relevant control region rather than duplicated by the page shell.
10. Where automated frontend tests exist for showcase activation, they should continue to pass unchanged or be updated only if layout-specific identifiers or structure changes require it.

## 11. Rollout / migration

1. Apply the tidy-up only to the showcase page within the existing temporary PrimeReact research area.
2. Review the updated showcase page in the normal Theia shell.
3. Compare the updated page against the prior version specifically for density, chrome reduction, and scroll behaviour.
4. Keep the rest of the temporary PrimeReact research package unchanged unless follow-up work is explicitly requested.

## 12. Open questions

None at present.

If later tuning is needed after implementation, it should be treated as small iterative adjustment within this same desktop-density direction rather than as a return to spacious boxed layouts.
