# Work Package: `086-workbench-output` — Workbench output panel

**Target output path:** `docs/086-workbench-output/spec-workbench-output_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created to define a desktop-style bottom output panel for the Workbench shell, including shell placement, toggle behavior, compact foldable output rows, developer-oriented output levels, toolbar actions, and migration of module-loading diagnostics away from the status bar.

## 1. Overview

### 1.1 Purpose

This specification defines a standard Workbench `Output` panel for notifications, general logging, and developer-oriented diagnostics.

Its purpose is to provide a read-only, terminal-like output surface that behaves like an IDE output window rather than a command shell. The panel must support compact chronological reporting, startup and module-loading diagnostics, warnings and errors, and expandable details without introducing terminal input or command execution behavior.

### 1.2 Scope

This specification includes:

- a full-width bottom `Output` panel rendered above the status bar
- a leftmost status-bar toggle for showing and hiding the output panel
- default output-panel sizing relative to the centre pane when first opened
- compact read-only output rendering with expandable detail rows
- a toolbar for `Clear` and scroll-related behaviors
- an append-only output-entry model suitable for developer tooling and general reporting
- output levels including `Debug`, `Info`, `Warning`, and `Error`
- migration of module-loading and similar diagnostic text away from the status bar into the output panel
- shell-level service and state contracts for publishing and rendering output entries
- use of the existing Workbench grid and splitter approach for layout integration

This specification excludes:

- command input, terminal emulation, or shell execution features
- editable output content
- a full log-search, query, or saved-history experience in the first slice
- external log shipping or remote diagnostics aggregation
- redesign of tool or module content outside the shell chrome needed to host the output panel
- replacement of all status-bar content, where some concise persistent status may still remain appropriate

### 1.3 Stakeholders

- Workbench platform developers
- module and tool authors publishing runtime diagnostics or user-facing output
- UX and product stakeholders responsible for desktop-style shell behavior
- repository maintainers responsible for Workbench shell consistency
- developers using the Workbench as a diagnostic and integration host

### 1.4 Definitions

- `output panel`: a read-only bottom shell pane that displays a chronological stream of output entries
- `output entry`: a single immutable record written to the output stream
- `summary`: the compact single-line text shown when an entry is collapsed
- `details`: optional extended text shown when an entry is expanded
- `level`: the severity or intent of an output entry, such as `Debug`, `Info`, `Warning`, or `Error`
- `source`: the subsystem, module, tool, or shell area that emitted the output entry
- `status bar`: the persistent shell footer used for compact current-state indicators and shell affordances rather than historical output
- `folding`: inline expand and collapse behavior for an entry's optional detailed body, intended to feel like editor-style code folding rather than large card expansion

## 2. System context

### 2.1 Current state

The current Workbench shell renders a full-width status bar as the bottom shell surface.

The current shell composes status-bar contributions from host and active-tool contributions and also renders fixed context values there.

The current host bootstrap already produces structured module-loading diagnostics through `ILogger` and separately buffers startup notifications for later display through the interactive shell.

The current shell does not yet provide a dedicated historical output stream for developer-facing startup traces, module-loading progress, general reporting, or compact diagnostic review.

As a result, some information that is chronological or diagnostic in nature is either shown transiently through notifications or represented in the status bar even though the status bar is better suited to persistent short state.

### 2.2 Proposed state

The Workbench shall expose a standard bottom `Output` panel that behaves like a read-only IDE output window.

The panel shall be hidden by default and shall be toggled from the far-left side of the status bar through a dedicated `Output` affordance with a directional expand-collapse indicator.

When first opened, the output panel shall appear between the current centre working area and the status bar and shall span the full shell width.

The initial vertical size shall give the output panel approximately `1*` height for every `4*` height of the remaining centre pane, or the nearest equivalent supported by the shell layout implementation.

The panel shall render compact one-line entries by default and shall allow inline folding open of optional details without large card spacing.

The panel shall support developer-oriented output levels, including `Debug`, so module-loading, startup diagnostics, and similar shell traces can move out of the status bar and into a chronological stream.

The status bar shall remain in place but shall focus on concise current-state and shell affordance presentation rather than acting as a historical reporting area.

### 2.3 Assumptions

- the Workbench should continue moving toward a desktop-like IDE shell rather than a web-page-style application shell
- the bottom diagnostic surface should feel closer to an editor output window than to browser notifications or a terminal emulator
- developer tooling is a first-class goal for the output panel, so `Debug` output is in scope from the first version
- the existing Workbench grid and splitter primitives are the preferred way to add the panel to the shell layout
- the Radzen Material appearance should remain the visual baseline where practical
- the output panel is read-only and append-only from the user perspective in the first slice
- some current status-bar content may still remain appropriate if it represents concise current state rather than historical output

### 2.4 Constraints

- the implementation shall remain shell-owned and must not require module-specific layout workarounds
- the menu bar must remain full-width above the working area
- the output panel must render above the status bar and below the centre working area
- the first implementation must remain compact and low-friction rather than introducing a heavyweight logging console
- the visual density should remain editor-like and avoid large padded row designs
- existing module-loading diagnostics currently surfaced through status-bar-adjacent shell behavior should be moved into the output stream as part of this feature definition
- the shell should continue to use project responsibilities already established for `UKHO.Workbench`, `UKHO.Workbench.Services`, `UKHO.Workbench.Infrastructure`, and `WorkbenchHost`

## 3. Component / service design (high level)

### 3.1 Components

The following existing and new areas are affected:

- `WorkbenchHost` layout components that render the shell chrome, status bar, and centre working area
- shell layout models and splitter configuration that define the initial output-panel height and resizing behavior
- new shared output contracts and models in `UKHO.Workbench`
- new output management services in `UKHO.Workbench.Services`
- host-level and module-level producers that write output entries during startup and runtime
- the current startup-notification and module-loading pathways in `WorkbenchHost`

### 3.2 Data flows

User-visible and system flow after the change:

1. The Workbench starts with the output panel collapsed.
2. Host startup and runtime services may publish output entries even while the panel is not visible.
3. The status bar renders a dedicated leftmost `Output` toggle with a directional indicator.
4. When the user opens the panel for the first time, the shell inserts a full-width bottom pane above the status bar using the default `1* : 4*` output-to-centre proportion.
5. The panel renders the current output stream in chronological order using compact rows.
6. Clicking a row disclosure affordance expands optional details inline without leaving the panel.
7. Toolbar actions such as `Clear`, `Auto-scroll`, and `Scroll to end` operate on the current panel session.
8. Startup and module-loading diagnostics that previously cluttered the status bar are instead available as `Debug` or higher-level output entries.

### 3.3 Key decisions

- Use a dedicated shell output panel rather than overloading the status bar.
- Make the panel read-only and append-only rather than a terminal or command console.
- Add `Debug` from the initial slice because developer tooling is an explicit goal.
- Use compact collapsed rows plus optional inline details rather than large notification-style cards.
- Keep the output-entry data model slightly richer than `text + level` by including timestamp and source metadata.
- Keep panel UI state separate from the immutable output-entry records.
- Move module-loading and similar shell diagnostics into the output stream instead of keeping them as status-bar text.

## 4. Functional requirements

### 4.1 Shell placement and visibility

- **FR-001** The Workbench shall provide a bottom `Output` panel that spans the full shell width.
- **FR-002** The output panel shall render above the status bar and below the centre working area.
- **FR-003** The output panel shall be hidden by default when the Workbench first loads.
- **FR-003a** The output panel shall remain collapsed on startup even when startup or module-loading entries already exist in the output stream.
- **FR-004** The status bar shall expose a dedicated `Output` toggle at the far-left edge.
- **FR-005** The toggle shall visually indicate whether the panel is collapsed or expanded by using a directional expand-collapse cue such as a chevron or arrow.
- **FR-005a** When the output panel is hidden, the `Output` toggle shall show a pending severity indicator when unseen output entries exist.
- **FR-005b** The pending severity indicator shall reflect the most severe unseen output level currently pending rather than only the latest unseen entry.
- **FR-005c** Opening the output panel shall reset the unseen severity indicator immediately.
- **FR-006** Activating the toggle shall show the output panel when hidden and hide it when visible.
- **FR-006a** The output panel shall not auto-open when new `Debug`, `Info`, `Warning`, or `Error` entries are written.
- **FR-007** Showing or hiding the output panel shall not disturb the menu bar, activity rail, explorer, centre tab strip, or status bar ordering.

### 4.2 Initial sizing and resize behavior

- **FR-008** When first opened in a session, the output panel shall use an initial height equivalent to approximately `1*` output height for every `4*` height of the remaining centre pane.
- **FR-009** The shell shall use the existing grid and splitter approach to support the output-panel layout boundary.
- **FR-010** The user shall be able to resize the boundary between the centre working area and the output panel after the panel is opened.
- **FR-011** Once resized during the current session, the shell should preserve the user-adjusted output height while the panel remains part of the shell layout.
- **FR-011a** When the output panel is closed and reopened within the same Workbench session, it shall reopen at the last user-adjusted height rather than resetting to the default split.
- **FR-012** This first slice does not require cross-session persistence of output-panel height unless the existing shell layout persistence model already supports it naturally.

### 4.3 Output toolbar

- **FR-013** The output panel shall contain a compact toolbar.
- **FR-014** The toolbar shall include a `Clear` action that removes all current output entries from the visible stream.
- **FR-014a** The first version of `Clear` shall remove all entries, including `Warning` and `Error` entries, without a confirmation prompt.
- **FR-014b** Invoking `Clear` shall also reset the hidden-panel unseen severity indicator.
- **FR-014c** Invoking `Clear` shall leave the output stream empty and shall not write a synthetic `Output cleared` entry.
- **FR-014d** When the output stream is empty, the panel shall not display a dedicated empty-state message or hint.
- **FR-015** The toolbar shall include an `Auto-scroll` toggle controlling whether new entries automatically move the view to the newest entry.
- **FR-015a** Manual upward scrolling away from the newest entries shall automatically disable `Auto-scroll`.
- **FR-016** The toolbar shall include a `Scroll to end` action that jumps to the latest output entry.
- **FR-016a** Invoking `Scroll to end` shall also re-enable `Auto-scroll`.
- **FR-017** The toolbar may include a `Wrap` toggle if needed to preserve readability for longer entries without changing the compact default density.
- **FR-017a** The first version shall include a visible `Wrap` toggle in the output-panel toolbar.
- **FR-017b** When `Wrap` is disabled, the output panel shall support horizontal scrolling.
- **FR-018** Toolbar controls shall be visually compact and aligned with the desktop-style output-window feel.
- **FR-018a** The first version shall not require visible source or level filtering controls in the output-panel UI.
- **FR-018b** The first version should include a copy action for the selected or expanded output entry only.
- **FR-018c** The first version shall not show a visible total-entry count in the output-panel toolbar.
- **FR-018d** The first version shall not require a separate selected-row state independent from entry expansion.

### 4.4 Output entry model and levels

- **FR-019** The output stream shall support immutable append-only output entries.
- **FR-019a** The output panel shall render entries in chronological order with the newest entries at the bottom.
- **FR-020** Each output entry shall include a timestamp.
- **FR-020a** The collapsed row presentation shall show the timestamp for every output entry by default.
- **FR-021** Each output entry shall include a level.
- **FR-022** Each output entry shall include a source.
- **FR-022a** The collapsed row presentation shall show the source for every output entry by default.
- **FR-023** Each output entry shall include a compact summary line.
- **FR-024** Each output entry may include optional details.
- **FR-025** Each output entry may include an optional stable identifier or event code when useful for diagnostics or repeated system messages.
- **FR-025a** Optional event identifiers or codes shall be shown in expanded details only and shall not appear in the default collapsed row presentation.
- **FR-026** The first supported output levels shall be `Debug`, `Info`, `Warning`, and `Error`.
- **FR-027** The output model shall support both user-facing reporting and developer-oriented diagnostics without requiring separate panel types.
- **FR-027a** The output-entry model shall retain source and level metadata so filtering can be added in a later version without redesigning the core data contracts.
- **FR-027b** `Debug` entries shall be visible by default in the first version.

### 4.5 Rendering and interaction model

- **FR-028** Output entries shall render as compact rows by default.
- **FR-029** Collapsed rows shall favour density similar to an editor or IDE output surface rather than notification cards.
- **FR-030** Output rows shall support inline expansion for optional details using a folding-style disclosure affordance.
- **FR-030a** Expanding or collapsing an entry shall be triggered by the disclosure affordance rather than by clicking anywhere on the row.
- **FR-031** Expanded details shall render beneath the summary line within the same row context.
- **FR-031a** The panel shall allow multiple output rows to remain expanded at the same time.
- **FR-032** Details shall preserve whitespace and line breaks appropriate for log-like or diagnostic text.
- **FR-032a** Expanded details shall follow the global `Wrap` toggle rather than using a separate wrapping mode.
- **FR-033** The output surface shall use read-only presentation and shall not provide text input or command submission.
- **FR-034** Visual severity cues such as iconography or subtle colour treatment shall distinguish levels without increasing row height materially.
- **FR-034a** Collapsed rows shall use a compact visual level marker without showing full level text in the default row presentation.
- **FR-035** The output surface should use monospace or similarly output-appropriate typography for details and may use the same typography for summaries where that best fits the shell design.

### 4.6 Status-bar simplification and migration of existing diagnostics

- **FR-036** The status bar shall no longer be the primary surface for historical startup or module-loading diagnostics.
- **FR-037** Existing module-loading messages currently represented through shell startup/status behavior shall be emitted into the output stream.
- **FR-038** Module-loading success messages should be written at `Debug` level by default.
- **FR-039** Module-loading failures shall appear in the output stream at `Warning` or `Error` level according to severity while user-safe notifications may still be raised when appropriate.
- **FR-040** Current shell context values that are presently shown in the status bar shall move into the output stream as output events rather than remaining as persistent right-aligned status-bar content.
- **FR-041** Host- or tool-contributed readiness messages that are better understood as historical events than current state shall move from status-bar contributions into output entries.
- **FR-041a** User-facing toast or notification messages shall also be written into the output stream.

### 4.7 Service behavior and producer integration

- **FR-042** The Workbench shall expose a central output service responsible for writing, clearing, and publishing output entries to the UI.
- **FR-043** The service shall allow host startup code, shell services, and tools/modules to append output entries without direct coupling to the UI implementation.
- **FR-044** The service shall notify the shell UI when the output stream changes.
- **FR-045** Output-entry records shall remain immutable after publication.
- **FR-046** Panel UI state such as visibility, auto-scroll, wrapping, and expanded rows shall be managed separately from the immutable output-entry records.
- **FR-047** The service should support a bounded retention limit so the in-memory output stream does not grow indefinitely during long-running sessions.
- **FR-047a** The first version shall maintain a single shell-wide output stream for the current Workbench session rather than switching output history by active tool.

## 5. Non-functional requirements

- **NFR-001** The Workbench output panel shall reinforce the desktop-like shell direction rather than a web-style notification tray.
- **NFR-002** The panel shall remain visually close to the stock Radzen Material theme where practical.
- **NFR-003** The panel shall feel compact and information-dense, avoiding large padded rows or card-like presentation.
- **NFR-004** The output stream shall be readable and useful for both end-user reporting and developer diagnostics.
- **NFR-005** The UI shall remain responsive when appending many output entries within the bounded retention window.
- **NFR-006** The shell shall not require module-specific CSS or layout workarounds to host the output panel.
- **NFR-007** The output feature shall be safe to use when the panel is collapsed; writing entries shall not depend on the panel being open.
- **NFR-008** The clear and scroll actions shall operate predictably without introducing duplicate or reordered entries.
- **NFR-009** The output panel shall remain keyboard- and pointer-operable where the chosen components support those interaction paths.

## 6. Data model

No domain model is required, but the Workbench shall introduce a shared shell output model.

Recommended core types for the first implementation are:

### 6.1 `OutputEntry`

A shared immutable record or model containing:

- `Id` or equivalent stable key
- `TimestampUtc`
- `Level`
- `Source`
- `Summary`
- `Details` (optional)
- `EventId` or `Code` (optional)

### 6.2 `OutputLevel`

An enum or equivalent bounded value set containing:

- `Debug`
- `Info`
- `Warning`
- `Error`

### 6.3 `OutputPanelState`

A shell UI state model separate from entry data, containing only UI/session state such as:

- `IsVisible`
- `AutoScroll`
- `WordWrap`
- current height or splitter position
- expanded-entry identifiers
- most-severe unseen level for the hidden-panel activity indicator

When the panel is closed and reopened in the same session, expanded-entry state shall be reset so all rows reopen collapsed by default.

### 6.4 Retention policy

The in-memory output stream should retain the most recent bounded set of entries.

The default retention count for the first version shall be `250` entries.

When the retention limit is exceeded, the oldest entries shall be discarded first so the output stream always preserves the most recent `250` entries.

## 7. Interfaces & integration

### 7.1 Internal interfaces

Expected internal touchpoints include:

- new output contracts and models in `src/Workbench/server/UKHO.Workbench`
- new output service abstractions and implementation in `src/Workbench/server/UKHO.Workbench.Services`
- shell layout integration in `src/Workbench/server/WorkbenchHost`
- shell startup integration in `src/Workbench/server/WorkbenchHost/Program.cs`
- any shell or module producer that currently surfaces diagnostic text through status-bar-oriented pathways

A likely first interface shape is:

- `IWorkbenchOutputService`
  - append/write an entry
  - clear entries
  - expose the current bounded entry set
  - notify subscribers when entries change

### 7.2 External interfaces

No new external service or public API is required.

The output panel is an internal Workbench shell capability.

Future work may add bridges from broader logging infrastructure, but such integration is not required in this first specification.

## 8. Observability (logging/metrics/tracing)

The output panel is itself an observability surface for the Workbench shell.

The feature shall complement existing structured `ILogger` usage rather than replace it.

The host and shell should continue to emit structured logs through normal logging abstractions while selectively projecting user-appropriate and developer-appropriate entries into the output stream.

Module-loading diagnostics should remain available in structured logs and should also be surfaced in the output stream so the shell provides an immediately visible developer trace.

## 9. Security & compliance

This feature does not change authentication or authorization boundaries.

Output entries shown in the UI shall remain appropriate for local developer and operator use within the Workbench and shall avoid exposing sensitive values unnecessarily.

User-facing startup or failure messages shown in the output stream shall remain safe and understandable, while highly technical implementation detail may remain primarily in structured logs unless intentionally projected into `Debug` output.

## 10. Testing strategy

The implementation should be verified with targeted Workbench tests rather than a full test-suite run.

Recommended coverage includes:

- shell rendering tests confirming the status bar renders an `Output` toggle on the far left
- shell rendering tests confirming the output panel is hidden by default
- layout tests confirming the panel appears above the status bar and below the centre pane when opened
- layout tests confirming the initial open ratio is approximately `1* : 4*` output-to-centre
- shell tests confirming the output toolbar exposes `Clear`, `Auto-scroll`, and `Scroll to end`
- service tests confirming entries append in chronological order and remain immutable
- service tests confirming `Clear` removes entries without corrupting state notifications
- rendering tests confirming entries render compact collapsed rows and can expand inline for details
- tests confirming module-loading diagnostics are written to the output stream as `Debug` entries
- tests confirming warnings and errors remain distinguishable visually and semantically

Where practical for UI verification in this repository, prefer Playwright end-to-end coverage over component-only tests.

## 11. Rollout / migration

This work package is an in-place shell enhancement.

No data migration is required.

Implementation should migrate shell-startup and module-loading diagnostics currently represented through status-bar-oriented patterns into the output stream.

Status-bar content should be reviewed and reduced to concise current-state indicators and shell affordances, leaving chronological reporting to the output panel.

The output feature should be defined in full at the specification stage and may then be split into delivery work packages by the planning workflow.

## 12. Open questions

The hidden-panel toggle activity indicator shall show the most severe unseen output level currently pending.

Opening the output panel shall reset the unseen severity indicator immediately.

The first version shall not require a keyboard shortcut for toggling or focusing the output panel.

No blocking functional questions remain for this specification.

Any remaining look-and-feel refinements may use sensible implementation defaults in the first version and can be revised in a later spec update if needed.

Potential future enhancements that are intentionally out of scope for this initial specification include:

- filtering by level or source
- copy/export actions
- pinned or bookmarked entries
- cross-session persistence of output history
- broader projection of `ILogger` events into the output stream
