# Specification: PrimeReact Theia System Foundation

**Target output path:** `docs/075-primereact-system/spec-frontend-primereact-system_v0.02.md`

**Version:** `v0.02` (`Draft`)

**Supersedes:** `docs/075-primereact-system/spec-frontend-primereact-system_v0.01.md`

## Change Log

- `v0.02` - pivots the implementation strategy from a generic shared baseline toward a true UKHO/Theia PrimeReact theme built from upstream SASS theme source, adds the repeatable source-build-deploy cycle, and separates theme work from the shared desktop layout contract.
- `v0.01` - initial Studio-first shared PrimeReact/Theia system draft.

## 1. Overview

### 1.1 Purpose

Define a Studio-first PrimeReact system for the Theia shell that combines:

- a true UKHO/Theia PrimeReact light theme and dark theme built from upstream PrimeReact theme source
- a separate shared desktop-style layout contract so PrimeReact pages behave like workbench surfaces rather than long-scrolling web pages
- a repeatable developer workflow of modify theme source -> build theme artifacts -> deploy theme into Studio -> visually verify the result

This work package now treats the custom theme as a first-class implementation concern rather than merely a downstream override layer on top of the shipped built-in themes.

### 1.2 Scope

In scope:

- aligning the local PrimeReact SASS theme source to the exact PrimeReact version used by Studio
- creating UKHO/Theia light and dark themes from upstream PrimeReact theme source
- defining a repeatable source-build-deploy workflow for those themes within this repository
- wiring Studio to consume the built theme outputs
- separating theme work from the shared desktop layout contract and page-host pattern
- using the current `Showcase` work as the first proving ground for uplifting styling decisions into the theme source
- providing authoritative documentation and a practical checklist for future contributors

Out of scope:

- backend, service, domain, or persistence changes
- introducing automatic theme rebuilds into ordinary day-to-day Visual Studio startup if a manual script-based cycle is sufficient
- eliminating all future page-level exceptions in advance
- extraction into a separate cross-project package during the first Studio-focused implementation

### 1.3 Stakeholders

- Studio shell maintainers
- UI/UX reviewers for Theia-based PrimeReact surfaces
- developers creating future PrimeReact pages and windows
- repository maintainers responsible for documentation and consistency

### 1.4 Definitions

- **Theme source**: the upstream-compatible PrimeReact SASS source used to author UKHO/Theia light and dark themes
- **Theme build cycle**: modify source -> compile theme artifacts -> deploy outputs into Studio -> visually verify results
- **Desktop layout contract**: the shared container and overflow rules that ensure PrimeReact pages behave like desktop workbench surfaces
- **Reference implementation**: the existing `Showcase` page used to prove both the theme and layout contracts in practice

## 2. System context

### 2.1 Current state

The current Studio shell uses `primereact` `10.9.7`.

The current PrimeReact research work has already established a substantial amount of desirable styling and density behavior in the `Showcase` page through local CSS refinements. Those changes prove that:

- a more Theia-aligned PrimeReact density is achievable
- desktop-style resize and inner scroll ownership can be achieved in Studio
- the `Showcase` tab is the strongest current visual and behavioral reference surface

The repository now also contains local PrimeReact SASS theme source under `src/Studio/Theme`, including build scripts and the standard `theme-base` and `themes` structure. The copied theme source currently reports `primereact-sass-theme` version `10.8.5`. Based on current upstream availability, this appears to be the latest available `primereact-sass-theme` source release even though Studio uses `primereact` `10.9.7`, so the implementation should treat `10.8.5` as the practical upstream source baseline and validate it against the running `10.9.7` component library in Studio.

This creates the following issues that must be resolved before a proper custom theme can be trusted:

- the local theme source version does not numerically match the installed PrimeReact component version, so compatibility must be verified in Studio rather than assumed
- the current plan still assumes a mainly downstream shared-CSS approach rather than a true source-authored custom theme
- the current `Showcase` styling improvements have not yet been lifted into theme source
- Studio does not yet have a documented repeatable workflow for theme source build and deployment
- the current repository structure does not yet distinguish clearly between upstream/reference theme source and Studio-owned custom UKHO/Theia theme source

### 2.2 Proposed state

The proposed state is a Studio-integrated UKHO/Theia PrimeReact system with two explicit layers:

1. **A custom UKHO/Theia PrimeReact theme**
   - light and dark variants
   - built from the latest practical upstream-compatible PrimeReact SASS source baseline currently available in the repository (`primereact-sass-theme` `10.8.5`)
   - compiled through a repeatable script-driven build workflow
   - deployed into a Studio-consumed theme output location

2. **A separate shared desktop layout contract**
   - full-height page host behavior
   - inner scroll ownership
   - splitter and data-region sizing rules
   - reference implementation and guidance for future pages

The first theme cycle should specifically take the style decisions already learned in `Showcase` and promote the theme-appropriate parts of those decisions into the theme source itself, rather than continuing to maintain them mainly as downstream page-local CSS.

Typography cohesion is also an explicit part of the target state. The UKHO/Theia PrimeReact themes should, where practical, use the same UI font family as Theia so PrimeReact surfaces sit naturally inside the existing shell chrome rather than appearing typographically separate.

### 2.3 Assumptions

- PrimeReact `10.9.7` remains the correct runtime target version for this repository
- the authoritative custom-theme guidance from [PrimeReact theming documentation](https://primereact.org/theming/#customtheme) applies to this work
- the correct source authoring model is the upstream `primereact-sass-theme` SASS repository and toolchain, even if the latest available SASS source release trails the installed PrimeReact runtime version numerically
- a manual or explicit script-based theme build is acceptable for development, and ordinary day-to-day Visual Studio runs do not need to rebuild themes automatically
- `Showcase` should remain the first proving surface for theme verification and later serve as the documented reference implementation
- `src/Studio/Theme` should be treated as the upstream/reference SASS source workspace rather than the main home for Studio-specific UKHO/Theia customizations
- the implementation can rely on Theia's existing UI font family contract, including `--theia-ui-font-family`, to achieve typography cohesion without introducing a separate hosted font in the first iteration

### 2.4 Constraints

- the theme must be built from source, not maintained as hand-edited compiled CSS
- the local `src/Studio/Theme` source should be validated and normalized as the practical upstream/reference baseline before theme customization proceeds deeply
- theme concerns and layout concerns must remain separate in both documentation and implementation
- the implementation must remain understandable and repeatable for future contributors
- source code implementation work must comply fully with `./.github/instructions/documentation-pass.instructions.md`
- the repository should keep upstream/reference theme source separate from Studio-owned editable custom theme source and generated outputs

## 3. Component / service design (high level)

### 3.1 Components

1. **PrimeReact theme source workspace**
   - `src/Studio/Theme`
   - upstream/reference `theme-base` and `themes` structure
   - build scripts and package metadata

2. **Studio-owned custom theme source workspace**
   - `src/Studio/Server/search-studio/src/browser/primereact-theme/source`
   - `shared` source fragments for UKHO/Theia tokens, fonts, and extensions
   - `ukho-theia-light` and `ukho-theia-dark` source folders

3. **Generated theme output workspace**
   - `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`
   - build outputs consumed directly by Studio frontend assets

4. **Theme build and deployment workflow**
   - scriptable local build command
   - scriptable copy/deploy step into the Studio-consumed theme output location, or direct build to that location if appropriate
   - clear verification entry points

5. **Shared desktop layout contract**
   - Studio-side host/layout helpers and CSS
   - full-height and inner-scroll behavior
   - workbench-style splitter and pane rules

6. **Reference implementation and migration surfaces**
   - `Showcase` as the first themed proving surface
   - later migration of remaining PrimeReact pages

7. **Documentation and governance**
   - authoritative dedicated wiki page
   - Studio summary wiki page
   - practical checklist and file-location guidance

### 3.2 Canonical folder structure

The preferred folder structure for the first implementation is:

- `src/Studio/Theme`
  - upstream/reference PrimeReact SASS theme source
  - treated as the reference and toolchain workspace
  - not the primary home for Studio-specific UKHO/Theia edits
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/shared`
  - shared UKHO/Theia SASS fragments such as tokens, fonts, and extensions
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-light`
  - Studio-owned editable light theme source
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-dark`
  - Studio-owned editable dark theme source
- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/ukho-theia-light`
  - generated light theme CSS consumed by Studio
- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/ukho-theia-dark`
  - generated dark theme CSS consumed by Studio

### 3.3 Data flows

At a high level:

1. A contributor leaves `src/Studio/Theme` as the upstream/reference source baseline and modifies the UKHO/Theia theme source under `src/Studio/Server/search-studio/src/browser/primereact-theme/source`.
2. A repository-provided script builds the light and dark theme outputs using the upstream/reference toolchain and the Studio-owned source folders.
3. A repository-provided script or build step deploys those outputs to `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`, or builds directly there.
4. Studio runs and loads the generated themes.
5. The contributor visually verifies the result first in `Showcase` and then in later PrimeReact pages.
6. Separately, PrimeReact pages consume the shared desktop layout contract so theme and layout remain distinct but complementary.

### 3.4 Key decisions

- the primary theming strategy is now a true UKHO/Theia PrimeReact theme built from upstream theme source, not just a structured override layer on top of compiled built-in themes
- the local theme source should use the latest available upstream/reference `primereact-sass-theme` baseline (`10.8.5`) and be validated against the Studio runtime PrimeReact version (`10.9.7`) before deeper theme customization proceeds
- the repository should provide a repeatable theme-development cycle of source modification, build, deployment, and Studio verification
- the first theme cycle should lift the current `Showcase` styling learnings into the theme source wherever those decisions are genuinely theme concerns
- theme concerns and desktop layout concerns must remain separate
- the `Showcase` page remains the first visual proving ground and reference implementation
- a manual or explicitly invoked build script is acceptable; the ordinary Visual Studio run loop does not need to rebuild theme assets automatically
- `src/Studio/Theme` should be treated as the upstream/reference source and the editable UKHO/Theia custom theme source should live under the Studio frontend area
- the preferred structure is to keep Studio-owned custom theme source under `src/Studio/Server/search-studio/src/browser/primereact-theme/source` and generated outputs under `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`
- the build workflow should document how to bootstrap tooling when it is not present, including running `npm install` under `src/Studio/Theme` before the first build if required
- the build workflow should provide explicit command examples for first-time bootstrap, theme build, and Studio deployment so contributors do not need to infer the process
- the first UKHO/Theia theme iteration should prefer using Theia's UI font family rather than introducing a separate custom-hosted font, unless later evidence shows that a dedicated font asset is required
- the dedicated PrimeReact/Theia wiki page remains the authoritative implementation guide

## 4. Functional requirements

1. The system shall validate and normalize `src/Studio/Theme` as the practical upstream/reference SASS source baseline used to build the custom Studio themes.
2. The system shall define UKHO/Theia light and dark PrimeReact themes authored from upstream-compatible SASS source rather than hand-edited compiled theme output.
3. The system shall provide a repeatable repository-local workflow for theme source build and deployment into Studio.
4. The system shall make the build and deployment workflow explicit through scripts or other documented repository automation.
5. The system shall keep `src/Studio/Theme` as the upstream/reference source workspace and place editable UKHO/Theia custom theme source under the Studio frontend tree.
6. The system shall build generated theme outputs into a Studio-consumed location, or copy them there through an explicit documented step.
7. The system shall document how to bootstrap the theme build toolchain if it is not already available locally, including the required package install step.
8. The system shall allow contributors to verify built theme outputs visually inside Studio without requiring an opaque or manual copy process.
9. The first theme cycle shall uplift the current `Showcase` styling decisions into theme source wherever those decisions concern typography, density, spacing, component chrome, or other theme-level behavior.
10. The UKHO/Theia PrimeReact themes shall, where practical, use Theia's UI font family to align PrimeReact typography with the host shell.
11. The first iteration shall prefer reusing Theia's existing UI font contract rather than shipping a separate custom font through `_fonts.scss`, unless later requirements justify hosted font assets.
12. The system shall keep desktop page layout behavior as a separate shared concern from the theme itself.
13. The system shall continue to provide a shared desktop-style layout contract for full-height behavior, pane sizing, and scroll ownership.
14. The system shall use `Showcase` as the first reference implementation and visual proving surface for the UKHO/Theia theme plus layout contract.
15. The system shall document the canonical reference source location, custom source location, generated output location, build process, deploy process, and verification process for the custom theme.
16. The authoritative wiki guidance shall include practical visual verification guidance for at least the initial `Showcase` cycle.
17. Future PrimeReact pages shall consume the built UKHO/Theia theme and the shared layout contract by default.
16. The system shall document the expected first-time bootstrap commands for the theme toolchain when local tooling or dependencies are not already present.
17. The system shall document the expected day-to-day build commands for regenerating and deploying the Studio-consumed theme outputs.

## 5. Non-functional requirements

1. The theme-development workflow should be repeatable and understandable by both developers and Copilot.
2. The source-build-deploy cycle should minimize manual error and reduce reliance on editing compiled CSS.
3. The custom theme should remain maintainable as PrimeReact evolves, starting from an exact version match.
4. Theme changes should be visually verifiable in Studio before broader page migration proceeds.
5. The system should avoid accidental coupling between theme styling and desktop layout mechanics.
6. The documentation should be practical enough that a contributor can re-run the theme cycle later without rediscovering the process.
7. The file and folder structure should make it obvious which theme assets are upstream/reference, which are Studio-owned source files, and which are generated outputs.
8. The documented build process should be simple enough that a contributor can bootstrap and rerun it without reverse-engineering the toolchain.

## 6. Data model

No domain/business data model changes are expected.

Configuration and asset concerns expected at the UI-system level:

- upstream/reference theme source under `src/Studio/Theme`
- Studio-owned custom theme source under `src/Studio/Server/search-studio/src/browser/primereact-theme/source`
- generated light and dark theme artifacts under `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`
- Theia UI font-family integration through the host shell CSS variable contract
- repository scripts for build and deployment
- Studio-side configuration for loading generated theme outputs
- shared layout host and helper configuration in the Studio frontend

## 7. Interfaces & integration

Expected integration points:

- `src/Studio/Theme`: upstream/reference PrimeReact SASS theme source workspace and toolchain
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source`: Studio-owned UKHO/Theia custom theme source
- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`: generated Studio-consumed theme outputs
- `src/Studio/Server/search-studio`: Studio frontend consuming generated theme outputs and the shared layout contract
- `wiki/PrimeReact-Theia-UI-System.md`: authoritative theming and layout guidance
- `wiki/Tools-UKHO-Search-Studio.md`: summary and entry point
- PrimeReact theming guidance at [PrimeReact custom theme documentation](https://primereact.org/theming/#customtheme)

Expected documented command flow:

- first-time bootstrap if tooling or dependencies are not present:
  - `npm --prefix .\src\Studio\Theme install`
- theme build:
  - `npm --prefix .\src\Studio\Theme run build`
- if build and Studio deployment are separated, the repository documentation must also define the explicit deploy/copy command for moving generated CSS into the Studio-consumed output location

## 8. Observability (logging/metrics/tracing)

No special production observability changes are expected.

Developer-facing diagnostics should focus on:

- build success or failure for the theme source
- copy/deploy success or failure for the generated theme outputs
- missing-tooling or first-time setup failures such as absent local `node_modules` for the theme workspace
- clear verification instructions rather than runtime telemetry-heavy behavior

## 9. Security & compliance

No special security or compliance changes are expected.

Standard frontend dependency and licensing review remains applicable for theme-source usage.

## 10. Testing strategy

The implementation should verify:

- the local theme source under `src/Studio/Theme` is treated as the accepted upstream/reference baseline for the current Studio implementation
- the repository build/deploy workflow produces usable light and dark theme outputs
- Studio can load and use the generated themes
- the generated UKHO/Theia themes correctly use Theia's UI font family or its host-shell font contract equivalent
- `Showcase` visually reflects the first theme-source uplift correctly
- shared desktop layout behavior still works correctly when the new theme is applied
- regression tests continue to protect shared page-host usage and key PrimeReact demo behavior where practical
- first-time build instructions are sufficient for a developer who does not yet have the theme toolchain installed locally

## 11. Rollout / migration

Likely rollout approach:

1. validate `src/Studio/Theme` as the accepted upstream/reference baseline and document that `10.8.5` is the latest practical SASS source currently available for the `10.9.7` runtime target
2. formalize the folder structure so `src/Studio/Theme` remains the reference source, Studio-owned custom theme source lives under the frontend tree, and generated outputs have a stable Studio asset location
3. establish the repository-local source-build-deploy workflow, including first-time toolchain bootstrap instructions if tooling is not already present
4. wire Studio to consume the generated UKHO/Theia light and dark theme outputs
5. configure the first UKHO/Theia theme iteration to use Theia's UI font family for typography cohesion
6. prove the cycle works end to end in `Showcase`
7. uplift the existing `Showcase` theme-relevant styling decisions into the theme source
8. verify the result visually in Studio
9. continue with shared layout-contract work and later page migration under the new theme strategy
10. update the authoritative wiki and summary wiki guidance

## 12. Open questions

No further clarification is currently required for the `v0.02` strategy draft.

Implementation should proceed on the basis that:

- theme source and theme build tooling are now part of the planned system
- the first theme cycle should prove source -> build -> deploy -> verify using `Showcase`
- theme work and layout-contract work remain separate but coordinated
- the build workflow may be explicit and script-driven rather than automatic on every Visual Studio run