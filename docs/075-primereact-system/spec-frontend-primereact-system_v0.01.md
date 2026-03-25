# Specification: PrimeReact Theia System Foundation

**Target output path:** `docs/075-primereact-system/spec-frontend-primereact-system_v0.01.md`

**Version:** `v0.01` (`Draft`)

## 1. Overview

### 1.1 Purpose

Define a shared PrimeReact UI foundation for the Theia Studio shell so all current and future PrimeReact-based pages, tabs, windows, and work areas use one standard visual system and one standard desktop-style layout/resizing model.

This work package is intended to move the current PrimeReact research implementation away from page-local styling and toward a curated reusable foundation that can be refined over time and reused by later Studio surfaces and, where practical, by future projects.

### 1.2 Scope

In scope:

- a shared PrimeReact/Theia styling foundation for Theia-hosted PrimeReact surfaces
- a shared desktop-style layout contract for page sizing, splitter behavior, overflow ownership, and resize behavior
- a simple authoring model for creating new PrimeReact pages and windows from one working starter pattern without copying large amounts of bespoke CSS
- a detailed `wiki` page describing the standard, required setup, file locations, and extension guidance
- a preference for one general-purpose page host/setup approach rather than multiple rigid template pages unless templates prove unavoidable
- migration guidance for existing PrimeReact demo/research pages onto the shared baseline

Out of scope:

- backend, services, APIs, or data model changes unrelated to frontend layout/systemization
- broad product redesign outside the PrimeReact/Theia design foundation
- immediate implementation of every future page that might use the system
- hard commitment to reusable packaging across other repositories before the local Theia foundation is validated

### 1.3 Stakeholders

- Studio shell maintainers
- UI/UX reviewers for Theia-based PrimeReact surfaces
- future feature developers building new Studio pages and windows
- repository maintainers responsible for documentation and consistency

### 1.4 Definitions

- **PrimeReact system foundation**: the shared tokens, styling rules, layout contract, and authoring guidance for PrimeReact inside the Theia shell
- **desktop-style layout contract**: the required container and overflow behavior so pages resize like a workbench application rather than a long-scrolling web page
- **page host**: the common root page/container pattern used by PrimeReact pages in the shell
- **local exception**: page-specific styling or layout only permitted when the shared system cannot reasonably cover the requirement

## 2. System context

### 2.1 Current state

The current PrimeReact research work uses the `PrimeReact Showcase Demo` as the main proving ground for layout and density refinements.

The current implementation demonstrates that:

- compact Theia-aligned density can be achieved
- desktop-like splitter and inner-scroll behavior can be achieved
- page-local CSS can correct presentation defects quickly

However, the current approach still has significant structural limitations:

- much of the styling is scoped to the `Showcase` tab or specific demo pages
- page behavior and visual standards are not yet governed by one shared cross-page contract
- developers could still create future pages that behave like web pages rather than desktop workbench surfaces unless guided explicitly
- there is not yet a strong central `wiki` page defining the shared system, file locations, and expected setup
- the system is not yet framed for reuse in other projects later on

### 2.2 Proposed state

Introduce one shared PrimeReact/Theia foundation that all PrimeReact pages in the Studio shell use by default.

The proposed state should provide:

- one standard shared styling baseline for PrimeReact controls and supporting page chrome
- one standard layout/resizing contract for all PrimeReact pages
- one simple authoring model for new UI pages/windows/work areas that gives a developer a working page as the starting point
- one strongly curated `wiki` page that documents the system and points to canonical CSS and any common page-host code
- minimal reliance on page-specific template pages, with preference given to one flexible page host plus instructions/guidance
- explicit allowance for narrow, justified page-specific exceptions only when necessary

### 2.3 Assumptions

- PrimeReact remains the preferred component library for this Theia research area
- Theia remains the host shell and its workbench behavior should be the primary UX reference
- future PrimeReact pages are expected to be added in this repository
- the first implementation pass should optimize for this Studio repository first, while keeping later reuse possible for other UKHO Theia projects
- the current `Showcase` tab is the best available behavioral baseline for desktop-style resizing and overflow ownership
- documentation in `wiki` is an important part of the long-term operating model, not optional polish

### 2.4 Constraints

- the system must work inside the current Theia shell architecture
- the system should prefer shared CSS and shared host patterns over repetitive page-local overrides
- the system should avoid forcing a large catalogue of rigid templates if one general approach is sufficient
- the initial architecture does not need to be extracted into a separate cross-project package immediately
- implementation work that touches code will need to comply with repository documentation and coding standards
- the resulting approach should be understandable and repeatable for future contributors

## 3. Component / service design (high level)

### 3.1 Components

High-level components expected in the solution:

1. **Shared PrimeReact styling foundation**
   - shared CSS variables/tokens
   - shared control density and typography rules
   - shared visual treatment for panels, tables, trees, tags, buttons, inputs, and pagination

2. **Shared PrimeReact desktop layout foundation**
   - page host/root contract
   - full-height behavior
   - splitter container behavior
   - inner scroll ownership rules
   - data-heavy region behavior

3. **PrimeReact page host/setup pattern**
   - one reusable page/container pattern that produces a working starter page for most future pages
   - optional smaller supporting layout helpers if required
   - preference for flexible composition over many rigid template pages

4. **Documentation and authoring guidance**
   - strong `wiki` page for contributors and reviewers
   - clear mapping to CSS and host files
   - extension instructions and exception rules

### 3.2 Data flows

At a high level:

1. A PrimeReact page is opened inside the Theia shell.
2. The page renders inside the shared PrimeReact page host/root container.
3. Shared layout rules establish desktop-style sizing, min-size behavior, and scroll ownership.
4. Shared styling rules apply the standard Theia-aligned PrimeReact density and control treatment.
5. Page-specific layout rules are applied only where the shared baseline does not fully address the scenario.
6. Contributors refer to the `wiki` page for the required setup and allowed extension points when adding new surfaces.

### 3.3 Key decisions

Current proposed decisions captured from stakeholder input:

- the rollout direction is Studio-first, with later reuse in other UKHO/Theia projects treated as desirable but not the primary constraint for the first implementation
- the first version should include light reusable layout helpers in addition to shared CSS and wiki guidance
- migration should be phased: introduce the shared system first, then migrate existing PrimeReact pages incrementally
- all new PrimeReact pages created after this work begins should be required to use the shared page host/layout contract immediately
- the dedicated PrimeReact/Theia UI system wiki page should be treated as the authoritative implementation guide, with `wiki/Tools-UKHO-Search-Studio.md` acting as the summary and entry point
- page-specific styling/layout deviations should be allowed where needed and handled case by case, with the wiki acting primarily as strong preferred guidance rather than hard prohibition
- the shared foundation should define a recommended canonical file and folder structure for shared CSS, layout helpers, and example host/setup code, while still allowing justified variation
- one existing page, most likely `Showcase`, should act as the documented reference implementation and prove the starter pattern works rather than introducing a separate rigid template page
- the system may distinguish between shared reusable rules and page-local exceptions pragmatically, without requiring a heavy formal layering model
- the practical goal is that either a developer or Copilot can create a new PrimeReact page from the shared foundation and get correct font sizing, weight, control look, spacing, and desktop-style layout behavior from the start
- the authoritative wiki guidance should focus on practical setup, starter-page composition, required layout rules, and shared asset locations rather than over-specifying low-level edge cases up front
- all PrimeReact pages should gradually converge on one shared standard set of CSS rather than each page carrying its own isolated styling rules
- all PrimeReact pages should behave like desktop application surfaces rather than vertically growing web pages
- the system should make it simple to add new windows/pages/work areas later
- the documentation model should use both a short summary in `wiki/Tools-UKHO-Search-Studio.md` and a dedicated detailed wiki page for the PrimeReact/Theia UI system
- the detailed wiki guidance must identify the location of shared CSS and any common page host/setup code
- the preferred direction is one flexible page/setup approach rather than a family of rigid template pages
- if instructions are still required for special cases, those instructions should be clearly documented in the `wiki`

## 4. Functional requirements

1. The system shall define a shared PrimeReact/Theia styling baseline that can be applied consistently across all PrimeReact pages in the Studio shell.
2. The system shall define a shared desktop-style layout contract so all PrimeReact pages resize and scroll like workbench surfaces rather than conventional web pages.
3. The system shall make it straightforward to create new PrimeReact pages, tabs, and windows without copying large amounts of page-specific CSS.
4. The system shall provide a working starter page or starter page-host pattern that already applies the shared typography, density, control styling, spacing, and desktop-style layout behavior.
5. The system shall prefer one general-purpose page host/setup pattern over multiple rigid template pages unless clear evidence shows multiple templates are necessary.
6. The system shall document the canonical location of shared CSS, tokens, helper code, and page-host code.
7. The system shall provide explicit authoring guidance describing how new PrimeReact pages must be set up to inherit the shared styling and layout behavior.
8. The system shall require the authoritative wiki guidance to include a short practical checklist for creating a compliant new PrimeReact page or window.
9. The system shall define what types of page-local overrides are allowed and what types should instead be promoted into the shared baseline.
10. The system shall provide migration guidance for existing PrimeReact research/demo pages so they can be aligned with the shared baseline over time.
11. The system shall require a detailed `wiki` page for contributor onboarding, reviewer expectations, and future extension.
12. The system shall support future reuse of the design approach in other projects by separating generic concepts from repository-specific details where practical.

## 5. Non-functional requirements

1. The shared foundation should minimize duplication and drift across PrimeReact pages.
2. The shared foundation should be maintainable by future contributors without requiring deep knowledge of each legacy page.
3. The system should be predictable enough that new pages inherit the expected Theia-style density and resize behavior by default.
4. The system should reduce regression risk by centralizing common behavior in one place.
5. The `wiki` guidance should be clear enough that a new contributor can add a compliant PrimeReact page with minimal guesswork.
6. The system should avoid unnecessary architectural complexity and should not introduce abstraction layers that provide little practical value.
7. The shared foundation should be practical enough that a developer can start from the standard page pattern and obtain correct behavior without resolving many low-level design questions first.

## 6. Data model

No domain/business data model changes are currently expected.

Configuration/state likely needed at the UI-system level:

- shared CSS token definitions
- shared page host/layout contracts
- documentation of allowed extension points

## 7. Interfaces & integration

Expected integration points:

- existing Theia-hosted PrimeReact demo/widget/page infrastructure
- shared CSS under the Studio frontend extension
- `wiki/Tools-UKHO-Search-Studio.md` or a related companion page for detailed guidance
- future PrimeReact pages and windows that must adopt the shared host/setup model

## 8. Observability (logging/metrics/tracing)

No special observability requirements are currently expected for the styling/layout foundation itself.

If implementation introduces reusable layout helpers with runtime behavior, logging expectations should remain minimal and diagnostic-focused.

## 9. Security & compliance

No special security or compliance changes are currently expected.

Standard frontend security practices remain applicable.

## 10. Testing strategy

The eventual implementation should verify:

- shared layout contracts are present and reused
- representative PrimeReact pages render inside the shared host/setup model
- regression coverage exists for desktop-style resizing/overflow expectations where practical
- documentation and reviewer guidance are updated alongside implementation

## 11. Rollout / migration

Likely rollout approach:

1. define the shared PrimeReact/Theia foundation for this Studio repository first
2. introduce the shared page host/setup contract
3. migrate the current `Showcase` page onto the shared baseline with minimal behavior change
4. migrate the other existing PrimeReact demo/research pages
5. update the `wiki` with canonical guidance and file locations
6. document `Showcase` as the reference implementation for the starter page pattern
7. require future PrimeReact pages in this repository to adopt the shared system by default
8. evaluate later extraction/reuse once the Studio-first baseline is stable

## 12. Open questions

No further clarification is currently required for the initial draft.

The implementation should proceed on the basis that:

- the shared foundation must give developers a working starter page or starter host pattern
- the authoritative wiki must provide practical setup guidance, shared file locations, and a short checklist for new pages
- future page-level deviations will be handled case by case rather than over-designed in advance
