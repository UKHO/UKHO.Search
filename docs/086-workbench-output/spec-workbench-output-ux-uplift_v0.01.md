# Work Package: `086-workbench-output` — Output text-area UX uplift

**Target output path:** `docs/086-workbench-output/spec-workbench-output-ux-uplift_v0.01.md`

**Version:** `v0.01` (`Draft`)

**Related specification:** `docs/086-workbench-output/spec-workbench-output_v0.01.md`

## Change Log

- `v0.01` — Initial draft created to define the UX uplift for the Workbench output text area so it behaves visually like a read-only editor while retaining the current output content model and existing toolbar scope.

## 1. Overview

### 1.1 Purpose

This specification defines a UX uplift for the Workbench output text area.

Its purpose is to make the visible output surface feel like a read-only editor by improving typography, spacing, density, selection behaviour, and folding affordances without changing the current output content model, current line content format, or the output toolbar scope.

This uplift is intended to bridge the current Workbench output implementation toward a future editor-backed experience while avoiding premature introduction of Monaco in this work package.

### 1.2 Scope

This specification includes:

- uplift of the output text-area presentation only
- editor-like typography, line spacing, density, and text selection behaviour
- removal of visible in-row action chrome that conflicts with a read-only-editor feel
- a chrome-less folding affordance that preserves current detail-oriented output behaviour
- preservation of the current displayed output content format
- a subtle severity gutter treatment suitable for an editor-like surface
- forward-compatible design constraints so a future Monaco-based implementation can adopt similar behaviour with minimal UX drift

This specification excludes:

- redesign of the output toolbar
- Monaco adoption in this work package
- changes to the current output entry data model
- changes to the current displayed header-line content format
- changes to the output stream service contracts
- changes to status-bar behaviour already defined in the existing output specification
- editing capabilities, command input, or any writable editor features

### 1.3 Stakeholders

- Workbench platform developers
- UX and product stakeholders responsible for desktop-style Workbench behaviour
- module authors who will later consume a shared editor abstraction in the Workbench platform
- developers using the output panel for diagnostics and runtime feedback
- repository maintainers responsible for consistency between interim shell UX and future Monaco-backed editor components

### 1.4 Definitions

- `output text area`: the visible scrollable output surface inside the Workbench output panel, excluding the toolbar and panel frame
- `editor-like`: a presentation style matching a read-only code editor in font, spacing, line density, selection behaviour, and folding affordances
- `fold triangle`: a small disclosure triangle shown at the left of a foldable output line, without visible button chrome
- `severity gutter marker`: a subtle coloured marker in the left gutter used to indicate output severity without changing the displayed text format
- `detail lines`: additional folded text lines rendered inline beneath the main output line when expanded

## 2. System context

### 2.1 Current state

The current Workbench output panel already exists and renders structured output entries with timestamps, sources, summaries, and optional details.

The current implementation achieves the required functional behaviour, but the output text area does not yet feel like a read-only editor. The main UX problems are visual and interaction-oriented:

- row spacing is too loose
- the current disclosure control reads as a button instead of an editor fold affordance
- the output surface does not yet fully match editor-like line height and monospace expectations
- explicit in-row copy affordances compete with the expected editor-style text selection model
- the text area currently feels like an interactive list rather than a dense, selectable output surface

The current toolbar remains outside the text-area concern for this uplift and should not be changed as part of this specification.

### 2.2 Proposed state

The Workbench output text area shall present output as a dense, read-only, editor-like surface.

The text area shall preserve the current displayed content format, but its visual treatment shall shift to:

- true editor-like single-line density
- a monospaced text presentation, with `Consolas` preferred for Windows-first deployment
- native-feeling text selection and standard clipboard behaviour through selection rather than explicit in-row copy controls
- a faint left gutter region containing only a subtle severity marker and a chrome-less fold triangle where applicable
- inline detail expansion as indented text lines directly below the main line

This shall remain a shell-owned output component rather than a Monaco-based editor in this work package. However, the visual model should move closer to an eventual editor-backed implementation so future Monaco adoption does not require a major UX reset.

### 2.3 Assumptions

- this Workbench runs predominantly on Windows workstations
- `Consolas` is an acceptable preferred output font for the first uplift
- the user values editor-like text presentation more than list-style structured-row chrome
- the current output content line format is acceptable and should remain unchanged in this work package
- output folding remains desirable to preserve the existing summary/detail mental model
- the output toolbar will be uplifted separately and is therefore out of scope here
- remaining presentation questions not explicitly clarified should use sensible editor-like defaults rather than requiring more clarification rounds

### 2.4 Constraints

- the uplift must apply only to the text area and not redesign the toolbar in this work package
- the shell must remain close to the stock Radzen Material theme overall, but the output text area should prioritise editor-like readability over generic component chrome
- the output panel must remain a shell-owned structured Workbench component
- the current displayed header-line content format must not change in this specification
- folding must remain available without visible button chrome
- the implementation should avoid introducing Monaco until a concrete editing scenario requires it
- the specification should not assume editor features beyond read-only presentation, selection, and folding

## 3. Component / service design (high level)

### 3.1 Components

This uplift affects the shell-owned output rendering surface only:

- `WorkbenchHost` output text-area markup and styling
- shell-owned output row rendering and fold affordance treatment
- text-area-specific CSS and minimal interaction behaviour needed for folding and selection
- host rendering tests covering text-area UX expectations

The following remain intentionally unchanged in this work package:

- output toolbar feature set and placement
- output service contracts and state model shape
- shell layout placement of the output panel
- the current output text content format shown in collapsed rows

### 3.2 Data flows

1. Output entries continue to be written to the shared shell-wide output stream.
2. The shell renders those entries using the existing content model.
3. The output text area presents each entry using editor-like typography and line density.
4. If an entry contains details, the line shows a fold triangle in the gutter area.
5. Activating the fold triangle expands detail lines inline beneath the main line.
6. Users select visible text directly in the text area and use normal clipboard copy behaviour rather than an explicit in-row copy command.

### 3.3 Key decisions

- Keep the current content format and improve presentation only.
- Keep folding, but make it feel like editor folding rather than button-driven expansion.
- Prefer native text selection over explicit row-copy commands in the text area.
- Use a Windows-first monospace stack headed by `Consolas`.
- Preserve a subtle severity cue through a left gutter marker instead of changing the textual line format.
- Do not introduce Monaco in this work package, but keep the UX compatible with a future editor-backed direction.

## 4. Functional requirements

### 4.1 Scope and preservation of current content

- **FR-UX-001** The output text-area uplift shall preserve the current displayed output content format.
- **FR-UX-002** The uplift shall not redesign or re-scope the output toolbar in this work package.
- **FR-UX-003** The uplift shall not change the underlying output-entry service contracts or shell-wide output behaviour.
- **FR-UX-004** The uplift shall focus on presentation, spacing, typography, selection behaviour, and fold affordances within the visible text area.

### 4.2 Editor-like text presentation

- **FR-UX-005** The output text area shall feel like a read-only editor rather than an interactive list.
- **FR-UX-006** The output text area shall use true editor-like single-line density rather than a merely “editor-inspired” approximation.
- **FR-UX-007** The output text area shall use a monospaced font family.
- **FR-UX-008** The preferred font stack shall favour `Consolas` for Windows-first deployment, followed by sensible monospace fallbacks.
- **FR-UX-009** The output text area shall minimise vertical padding so line height, row height, and text spacing read like a code editor or output window.
- **FR-UX-010** Output rows shall avoid card-like spacing, pill-like affordances, or button-heavy per-row presentation.
- **FR-UX-011** The text area shall support natural mouse-driven text selection across main lines and expanded detail lines.
- **FR-UX-012** The text area shall rely on standard clipboard copy from selected text rather than requiring an explicit text-area copy affordance.

### 4.3 Folding and fold affordance

- **FR-UX-013** Folding shall remain supported for entries with details.
- **FR-UX-014** Expanded details shall render inline as additional indented text lines directly beneath the main line.
- **FR-UX-015** The fold affordance shall appear as a simple disclosure triangle positioned to the left of the text.
- **FR-UX-016** The disclosure triangle shall not visually read as a button.
- **FR-UX-017** The disclosure triangle shall not show button chrome such as borders, filled backgrounds, raised styling, or oversized padding.
- **FR-UX-018** The disclosure triangle shall be roughly the visual size of a single text character.
- **FR-UX-019** Hovering the disclosure triangle shall show a pointer cursor.
- **FR-UX-020** The disclosure triangle shall change orientation between collapsed and expanded states.
- **FR-UX-021** The fold affordance shall remain compact enough that the line still reads primarily as text.
- **FR-UX-022** Multiple folded regions may remain expanded at the same time unless future editor-backed behaviour intentionally changes that model.

### 4.4 Gutter and severity treatment

- **FR-UX-023** The text area shall include only minimal editor-like gutter treatment.
- **FR-UX-024** The gutter shall contain the fold triangle for foldable entries and a subtle severity indicator.
- **FR-UX-025** The severity indicator shall be shown as a very small coloured left gutter marker per line.
- **FR-UX-026** The severity gutter marker shall not alter the displayed line content format.
- **FR-UX-027** The text area shall not introduce line numbers in this work package.
- **FR-UX-028** The text area shall not introduce additional gutter chrome beyond the minimal space needed for the fold triangle and severity marker.

### 4.5 Interaction model

- **FR-UX-029** The visible text area shall prioritise text scanning and selection over row-level commands.
- **FR-UX-030** The text area shall avoid visible in-row copy controls as part of the uplift target state.
- **FR-UX-031** Clicking the fold triangle shall expand or collapse the corresponding detail lines.
- **FR-UX-032** Clicking ordinary text shall behave like a read-only text surface and shall not trigger row-wide expansion.
- **FR-UX-033** Expanded detail lines shall remain selectable as normal text.
- **FR-UX-034** The text area shall continue to respect existing wrap behaviour, but spacing and font treatment shall remain editor-like in both wrapped and non-wrapped states.

### 4.6 Forward compatibility

- **FR-UX-035** The uplift should move the output text area visually closer to a future editor-backed implementation without introducing Monaco in this work package.
- **FR-UX-036** The fold/detail mental model shall remain compatible with future folding support in an editor-backed implementation.
- **FR-UX-037** The text-area UX shall avoid introducing temporary visual patterns that would need to be unlearned when a future shared Workbench editor component is introduced.

## 5. Non-functional requirements

- **NFR-UX-001** The output text area shall feel dense, stable, and highly readable for long diagnostic sessions.
- **NFR-UX-002** The output text area shall remain performant within the existing bounded output retention window.
- **NFR-UX-003** The uplift shall remain accessible for pointer and keyboard users where the existing shell supports those interactions.
- **NFR-UX-004** The text area shall preserve good contrast and readability in both light and dark shell themes.
- **NFR-UX-005** The uplift shall minimise bespoke interaction logic and prefer simple, predictable read-only text behaviour.
- **NFR-UX-006** The text area shall be visually credible as an editor-like surface even before Monaco is introduced elsewhere in the platform.

## 6. Data model

No new data model is required for this UX uplift.

The uplift shall continue using the current output-entry and output-panel models defined in the existing Workbench output specification.

Any implementation changes should treat this work as a rendering-layer refinement rather than a data-contract redesign.

## 7. Interfaces & integration

### 7.1 Internal interfaces

No new platform-wide public interface is required for this specification.

Expected implementation touchpoints are limited to shell rendering concerns such as:

- output row markup
- fold-affordance rendering
- text-area CSS and layout classes
- rendering tests that verify the editor-like presentation contract

### 7.2 External interfaces

No external integration is required.

This specification intentionally defers Monaco integration to a future work package with a concrete editing scenario.

## 8. Observability (logging/metrics/tracing)

This UX uplift does not change what is logged or written to the output stream.

It changes only how the output text area presents that information so that developer diagnostics remain easy to scan, fold, and select.

## 9. Security & compliance

This specification introduces no new security boundary.

Because the uplift encourages normal text selection and clipboard copy, output content shown in the panel should continue to avoid including sensitive values unnecessarily, consistent with the existing Workbench output specification.

## 10. Testing strategy

Recommended focused verification includes:

- rendering tests confirming the output text area uses the intended monospace/editor-oriented classes
- rendering tests confirming the fold affordance no longer renders with visible button chrome
- rendering tests confirming the fold triangle remains present only for entries with details
- rendering tests confirming expanded details render inline as indented text lines beneath the main line
- rendering tests confirming the subtle severity gutter marker is present without changing displayed text format
- manual or automated verification confirming selected text can be copied using standard text selection behaviour
- visual verification confirming line density matches a true editor-like single-line presentation more closely than the current implementation

The toolbar should not be part of the acceptance focus for this specific UX uplift.

## 11. Rollout / migration

This specification is an in-place UX uplift of the existing output text area.

No data migration is required.

Implementation should be treated as a presentation refinement of the current shell output surface, not as a migration to Monaco.

A future work package may introduce a shared Workbench editor abstraction and later align this output surface with that component more directly.

## 12. Open questions

No blocking functional questions remain for this specification.

The following items are intentionally deferred:

- toolbar UX uplift
- Monaco adoption for output rendering
- broader editor-platform abstraction in `UKHO.Workbench`
- any later decision about line numbers or richer editor gutter behaviours
