# Work Package: `068-home-page` — Restore Studio home landing page

**Target output path:** `docs/068-home-page/spec-home-page_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft covering restoration of the Studio home landing page, branded header treatment, introductory guidance content, and common-task jump points.
- `v0.01` — Clarified that common-task jump points should use task-focused labels rather than only broad area names.
- `v0.01` — Clarified that the `Home` page should be always available and should also be the default initial Studio page.
- `v0.01` — Clarified that the initial `Home` page should remain static and should not include a summary or status area.
- `v0.01` — Fixed the default task-focused jump points to `Start ingestion`, `Manage rules`, and `Browse providers`.
- `v0.01` — Clarified that `Home` should open as a normal tab document and be re-openable from the Theia `View` menu.
- `v0.01` — Clarified that jump points should follow the normal Theia interaction model for their destination rather than forcing a custom open behavior.
- `v0.01` — Clarified that reopening `Home` from the Theia `View` menu may either focus an existing tab or open a new one, whichever fits the simpler normal Theia implementation.
- `v0.01` — Clarified that the `Home` tab should be closable like a normal document tab.
- `v0.01` — Clarified that the logo asset should be copied from `docs/` into the appropriate application asset location and displayed at a reduced default size.

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for restoring a `Home` landing page to the Studio experience.

The original Studio skeleton included a landing page. That page should be reintroduced so users have a clear starting point when they first enter Studio, with lightweight branding, a short introduction, and obvious navigation to common tasks.

The page is intended to provide:

- an immediate sense of place and product identity
- a brief orientation to what `Search Studio` is for
- a small set of quick navigation actions to commonly used Studio areas
- a more polished first-run and return-to-home experience than dropping users directly into a blank or task-specific work surface

### 1.2 Scope

This specification covers:

- restoring a Studio `Home` landing page
- using `docs/ukho-logo-transparent.png` as the page header graphic
- copying `docs/ukho-logo-transparent.png` into the appropriate application/static asset location for runtime use
- presenting the logo appropriately on the expected dark background
- displaying the logo at a reduced size suitable for the landing page layout
- adding short introductory or instructional copy for `Search Studio`
- adding common-task jump points for likely destinations such as ingestion and other primary Studio areas
- defining the expected placement, interaction pattern, accessibility baseline, and theme compatibility for the page

This specification does not cover:

- redesigning the wider Studio information architecture
- introducing new backend APIs
- finalizing production-quality marketing copy
- changing the core structure of existing provider, rules, or ingestion workflows beyond adding navigation entry points to them
- broader branding redesign outside the restored `Home` page itself

### 1.3 Stakeholders

- Studio/tooling developers
- engineering leads shaping Studio UX
- operational users entering Studio for routine workflows
- new or infrequent users who benefit from a clear starting page

### 1.4 Definitions

- `Home page`: the Studio landing page intended as the primary orientation surface
- `jump point`: a prominent UI action that takes the user directly to a common workflow or area
- `header graphic`: the branded top-of-page visual treatment using the supplied UKHO logo asset
- `dark background`: the expected visual context in which the supplied transparent logo remains legible and on-brand

## 2. System context

### 2.1 Current state

The Studio shell previously had a `Home` landing page during the early skeleton phase, but that landing page is no longer present.

The current Studio direction already includes distinct task-oriented areas such as provider-focused views, rules tooling, and ingestion tooling. Without a landing page, users are taken more directly into workbench surfaces without an obvious introductory entry point.

The repository already provides the required logo asset at:

- `docs/ukho-logo-transparent.png`

The supplied logo is transparent and intended to be viewed against a dark background.

### 2.2 Proposed state

Studio shall again provide a `Home` landing page as a lightweight welcome and orientation surface.

The restored page shall:

- display the supplied UKHO logo prominently near the top of the page
- use a copied application-served asset derived from `docs/ukho-logo-transparent.png` rather than referencing the repository docs location directly at runtime
- use a dark-compatible presentation so the transparent logo remains visually correct
- render the logo at a reduced default size because the source image is too large for direct unscaled use
- include concise placeholder intro/help text for `Search Studio`
- provide a short list of common-task jump points that take users into key Studio workflows

The initial content may remain intentionally light and practical. The priority is to restore the page structure and usefulness, not to perfect the wording in this work package.

### 2.3 Assumptions

- Studio continues to target a dark-oriented visual environment
- the supplied transparent UKHO logo should be reused directly rather than recreated
- short placeholder copy is acceptable in the first restored version
- task-focused jump points are more useful on the `Home` page than broad area-only links
- the page should feel like a useful operational landing page rather than a decorative splash screen
- restoring the page should not require new backend contracts

### 2.4 Constraints

- the supplied logo asset must remain visually legible on the chosen background treatment
- the implementation must not depend on serving the logo directly from the repository `docs/` location if that is not the normal application asset path
- the page must remain lightweight and fast to render
- the home content must not become the sole navigation path to Studio features
- jump points should use existing Studio navigation destinations where possible
- the logo must be rendered at a reduced size appropriate to the landing page composition
- the work package should remain focused on restoring and polishing the landing page rather than expanding into a broader homepage productization exercise

## 3. Component / service design (high level)

### 3.1 Components

1. `Home` page container
   - the top-level landing surface within Studio
   - responsible for layout, spacing, and dark-compatible presentation

2. Branded header section
   - uses a copied application asset sourced from `docs/ukho-logo-transparent.png`
   - provides the main visual identity for the page
   - applies reduced-size presentation suitable for the page layout

3. Introductory content block
   - provides short explanatory or instructional text
   - helps users understand what Studio is for and where to begin

4. Common-task jump-point section
   - exposes obvious quick actions to key Studio workflows
   - supports direct navigation to existing Studio areas

### 3.2 Data flows

#### Home page entry flow

1. user enters Studio or navigates to the `Home` page
2. Studio renders the home layout
3. the header graphic, intro text, and jump points are shown
4. the user either remains on the page for orientation or selects a jump point

The `Home` page shall be the default initial page presented on Studio entry, shall open in the editor area as a normal tab document, and shall remain available for later re-opening through the Theia `View` menu.

#### Jump-point navigation flow

1. user selects a common-task jump point
2. Studio routes the user to the matching existing work area or destination
3. the destination opens using that destination's normal Theia interaction pattern
4. Studio does not introduce a custom one-off navigation behavior just for `Home` jump points

### 3.3 Key decisions

- **Restore a dedicated `Home` landing page**
  - rationale: users benefit from an obvious starting point and a clearer first impression

- **Use the supplied UKHO transparent logo asset directly**
  - rationale: the user explicitly requested use of the existing asset and it already matches the intended dark-background presentation

- **Keep introductory copy brief and pragmatic**
  - rationale: the user is not currently concerned with polished final wording, so the first draft should prioritize clarity over detailed content design

- **Provide jump points to common tasks**
  - rationale: the `Home` page should help users start useful work quickly rather than act only as a passive welcome screen

- **Favor an operational landing-page style over a marketing splash page**
  - rationale: Studio is a working tool and the page should orient users toward action

## 4. Functional requirements

### FR-001 Restore the Studio `Home` page

Studio shall provide a restored `Home` landing page as part of the Studio shell experience.

### FR-001a Default initial page

The restored `Home` page shall be the default initial page shown when the user enters Studio.

### FR-001b Re-openable home page

The restored `Home` page shall remain available after initial entry so users can return to it through the Theia `View` menu.

### FR-001c Home as tab document

The restored `Home` page shall open in the Studio editor area as a normal tab document.

### FR-002 Branded header graphic

The `Home` page shall display `docs/ukho-logo-transparent.png` in a prominent header position.

### FR-002a Copy source asset into application location

The implementation shall copy the source logo file from `docs/ukho-logo-transparent.png` into the appropriate application asset location for normal runtime serving.

### FR-002b Do not rely on docs path at runtime

The `Home` page shall not rely on the repository `docs/` path as the runtime-served asset location unless that is already the normal application asset mechanism.

### FR-003 Responsive logo scaling

The logo shall be scaled appropriately for the available page width so it remains prominent without overwhelming the page or appearing distorted.

### FR-003a Reduced default logo size

The default rendered logo size shall be materially smaller than the source image's natural size because the source asset is too large for direct display on the landing page.

### FR-004 Dark-compatible logo presentation

The `Home` page shall use a visual treatment that preserves legibility of the transparent logo on the expected dark background.

### FR-005 Introductory text

The `Home` page shall include short text that introduces `Search Studio` or gives brief usage guidance.

### FR-006 Placeholder copy is acceptable

The initial introductory text may be lightweight placeholder content so long as it gives users a reasonable sense of the page purpose and how to begin.

### FR-007 Common-task jump points

The `Home` page shall provide a small set of prominent jump points for common tasks.

### FR-008 Existing-destination navigation

Each jump point shall route to an existing Studio destination rather than to a placeholder destination invented only for the page.

### FR-008a Normal destination behavior

Each jump point shall use the normal Theia interaction behavior for its target destination rather than forcing a custom open mode solely because navigation started from `Home`.

### FR-009 Ingestion jump point

At least one jump point shall provide direct navigation toward ingestion-related workflow entry.

### FR-010 Additional primary-area jump points

The page shall provide the following default task-focused jump points, routed to the closest matching existing Studio destinations:

1. `Start ingestion`
2. `Manage rules`
3. `Browse providers`

### FR-011 Clear action labelling

Jump points shall use labels that make the target destination or task obvious to users without requiring prior hidden knowledge.

### FR-012 Keyboard and pointer interaction

The page and its jump points shall be fully usable with standard pointer and keyboard interaction.

### FR-013 Home page readability

The page shall remain readable and visually balanced on typical Studio viewport sizes.

### FR-014 Non-blocking design

The restored `Home` page shall not prevent users from reaching task-oriented Studio areas through existing navigation patterns.

### FR-015 Static first version

The initial restored `Home` page shall remain a static landing page containing branding, introductory text, and task-focused jump points only.

### FR-016 No summary/status area in first version

The initial restored `Home` page shall not include dashboard-style status summaries, counts, recent activity, or other dynamic summary content.

### FR-017 View-menu reopening

Studio shall provide a `View` menu entry or equivalent Theia `View` menu action that reopens the `Home` tab document.

### FR-018 Reopen behavior may follow normal Theia simplification

If the `Home` tab is already open, invoking the `View` menu action may either focus the existing `Home` tab or open a new `Home` tab, provided the chosen behavior follows the simpler normal Theia implementation approach and remains consistent.

### FR-019 Closable home tab

The restored `Home` page shall be closable like a normal document tab and shall not be pinned or made non-closable in the first version.

## 5. Non-functional requirements

- The page should load with no noticeable delay beyond normal Studio shell rendering.
- The layout should respect the active dark-oriented Studio theme and remain visually coherent.
- The logo should remain crisp and undistorted at supported sizes.
- The copied application asset should be managed in the appropriate place for the Studio application's normal static asset pipeline.
- The page should remain usable on common desktop viewport sizes without horizontal scrolling in normal conditions.
- The page should maintain sufficient contrast for text and interactive elements.
- The page should feel lightweight and operational rather than heavy or promotional.

## 6. Data model

No backend data model or API contract change is required for this work package.

The page content may be implemented using static UI content and existing Studio routing/navigation constructs.

If local configuration is needed, it should be limited to lightweight presentation or route metadata rather than introducing new business data structures.

## 7. Interfaces & integration

This work package shall integrate with:

- the existing Studio shell routing/navigation model
- the Theia `View` menu command contribution model
- the existing Studio destinations for major work areas
- the existing Studio theme/styling system
- the repository asset located at `docs/ukho-logo-transparent.png`
- the application's normal static asset location used to serve the copied logo at runtime

No new external service integration is expected.

## 8. UX and content guidance

### 8.1 Page intent

The page should feel like a practical welcome surface for a working tool.

It should help users answer:

- where am I?
- what is this tool for?
- what should I click first?

### 8.2 Content tone

The first version should use concise, calm, operational language.

It does not need polished product copy at this stage. Simple wording such as a welcome line, a one-paragraph introduction, and short task labels is acceptable.

### 8.3 Jump-point presentation

Jump points should be visually obvious and scan-friendly.

A card, button, or action-list treatment is acceptable provided it:

- reads clearly as clickable navigation
- gives each task enough context to be understood quickly
- avoids visual clutter

### 8.4 Suggested default jump points

The page should expose the following task-focused quick navigation:

1. `Start ingestion`
2. `Manage rules`
3. `Browse providers`

These labels should be treated as the baseline labels for the first implementation.

### 8.5 Summary/status content

The first version of the `Home` page should remain intentionally simple.

It should not include a summary strip, recent activity area, counts, health indicators, or other dashboard-like status content.

If such content is desired later, it should be introduced as a deliberate follow-up enhancement rather than folded into this restore work.

## 9. Acceptance criteria

- A Studio `Home` landing page is present again.
- The page displays the supplied UKHO logo asset in a suitable header treatment.
- The page remains visually correct on the expected dark background.
- The page includes short introductory/help text.
- The page includes a small set of common-task jump points.
- One jump point leads users toward ingestion.
- Jump points navigate to existing Studio destinations.
- The page is usable with keyboard navigation as well as pointer interaction.

## 10. Testing and validation

The implementation should be validated through:

- visual verification of the page in the expected dark Studio theme
- verification that the supplied logo renders correctly and scales cleanly
- verification that the logo asset has been copied from `docs/ukho-logo-transparent.png` into the appropriate runtime asset location
- verification that the rendered logo uses a reduced default size appropriate to the landing page
- navigation checks confirming each jump point reaches the intended destination
- accessibility-oriented checks for focus visibility, keyboard access, and readable contrast
- responsive checks across the desktop viewport sizes normally used for Studio

## 11. Open questions / pending decisions

No open clarification questions remain in the current draft beyond implementation-level decisions.
