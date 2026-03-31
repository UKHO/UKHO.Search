# Workbench Output Panel XtermBlazor Adoption Specification

**Target output path:** `docs/086-workbench-output/spec-workbench-output-xtermblazor-adoption_v0.01.md`

**Version:** `v0.01` (`Draft`)

**Work package:** `086-workbench-output`

**Related documents:**
- `docs/086-workbench-output/spec-workbench-output_v0.01.md`
- `docs/086-workbench-output/spec-workbench-output-ux-uplift_v0.01.md`
- `docs/086-workbench-output/plan-workbench-output-ux-uplift_v0.01.md`

## 1. Overview

This specification captures the agreed direction change for the Workbench output panel inside `src/workbench/server/WorkbenchHost`.

The current row-rendered output surface has not produced the desired UX. The new direction is to replace the row-oriented output rendering with a single read-only terminal-style surface powered by `xterm.js`, using the Blazor package [`XtermBlazor`](https://github.com/BattlefieldDuck/XtermBlazor).

The output panel remains a shell-owned rendering surface. The underlying output data contracts and state ownership do not change. `UKHO.Workbench` and `UKHO.Workbench.Services` continue to own the output entries and panel state, while `WorkbenchHost` becomes responsible for projecting that state into a read-only terminal experience.

This document is the single specification for the `XtermBlazor` adoption direction within work package `086-workbench-output`.

## 2. Goals

- Replace the current row-rendered output text area with a single read-only terminal-style display.
- Use `XtermBlazor` as the integration path for `xterm.js` in the Blazor Server Workbench shell.
- Keep output state ownership exactly where it is today in `IWorkbenchOutputService` and `OutputPanelState`.
- Keep a single toolbar immediately above the `xterm.js` surface inside the output panel rather than introducing another top-level shell toolbar.
- Support a dense, selection-friendly, append-oriented output experience better aligned with log and console usage.

## 3. Scope

### In scope

- Shell-level adoption of `XtermBlazor` for the Workbench output panel.
- Projection of existing `OutputEntry` instances into a read-only terminal stream.
- Preservation of existing output entry and output panel state contracts.
- Toolbar redesign implications needed to support a single panel-local toolbar directly above the terminal surface.
- Terminal-level concerns such as selection, copy, wrapping, auto-scroll, scroll-to-end, severity display, and theme integration.

### Out of scope

- Any change to the `OutputEntry` data model.
- Any migration of output state ownership away from existing shared services.
- Monaco adoption in this work item.
- A reusable module-editor wrapper abstraction for third-party terminal/editor components.
- Folding support, unless later reintroduced explicitly in a future revision.

## 4. Confirmed requirement baseline

### Confirmed decisions

- The `xterm.js` direction replaces the current row-rendered output design for this work package.
- The integration package is `XtermBlazor`.
- The output surface should be read-only.
- The output panel should use one toolbar directly above the terminal surface inside the panel.
- Output state remains owned by the existing shell/output service model.
- The terminal-style UX is preferred over the Monaco-based editor-style UX for now.
- Folding is dropped from this direction unless explicitly revisited later.
- Output `Details` and optional `EventCode` should always render inline in the terminal stream beneath the summary line.
- `Copy selected` is mandatory on the single top toolbar, and the remaining output actions should be included where they are solid to implement without hacks.
- The first delivery should include semantic colour/severity formatting for `Debug`, `Info`, `Warning`, and `Error`.
- The first `XtermBlazor` delivery should include terminal-side search/find support.
- `Wrap` should be included only if it is cleanly supported by the package; otherwise, wrap-specific toolbar items should be removed.
- The first `XtermBlazor` delivery should render the full retained output history from the shared output service.
- The terminal should follow the existing Workbench light/dark appearance toggle from the first delivery.
- Both `Auto-scroll` and `Scroll to end` should remain in the first delivery.
- Severity colouring should use whichever mechanism is cleaner with `XtermBlazor`, with no fixed preference between ANSI-style output and terminal API-driven styling.
- `Clear` should be included only if it is cleanly supported without hacks.
- Standard official `xterm.js` addons are acceptable in this work package when required for clean features such as fit and search.
- The terminal should allow normal keyboard focus and keyboard interaction from the first delivery.
- Output writes should continue to appear immediately in the terminal projection while the panel is hidden so reopening the panel shows the fully updated stream without replay delay.
- The first `XtermBlazor` delivery should preserve the existing hidden-panel unseen severity indicator on the shell toggle.
- The first `XtermBlazor` delivery should retain the current timestamp + source + summary text format on every projected summary line.
- `Auto-scroll` should disable automatically when the user scrolls away from the bottom, preserving the current behaviour.
- It is mandatory that the `xterm.js` surface sizes to fit the output panel it is hosted in, including after open and resize operations.
- The `Copy selected` toolbar action should be disabled when there is no active terminal selection.
- A minimal terminal context menu is acceptable in the first delivery if `XtermBlazor` supports it cleanly.
- The first `XtermBlazor` delivery should preserve the existing panel open/close behaviour and panel-height memory exactly as it works today.
- When `Find` is included, both the top-toolbar entry point and standard keyboard shortcut support such as `Ctrl+F` should work in the first delivery.
- Standard keyboard copy from an active selection, such as `Ctrl+C`, should also work in the first delivery alongside the `Copy selected` toolbar action.
- The first `XtermBlazor` delivery should preserve the current chronological output ordering, with newer entries appended at the bottom.
- The output toolbar should be visible only when the output panel is open because it lives inside the panel immediately above the terminal.

### Requirement summary

The first `XtermBlazor` delivery is expected to behave as a terminal-style replacement for the current row-rendered output area, while preserving all important existing shell state behaviour. The design is intentionally evolutionary at the service-contract level and intentionally disruptive only at the rendering-layer level.

### Known technical implications

- `WorkbenchOutputRow` is expected to become obsolete or significantly reduced.
- The output panel body is expected to become a panel-local toolbar plus a single `XtermBlazor` host instead of a per-entry Blazor render tree.
- Existing row-focused rendering tests will need to be replaced or substantially rewritten.
- `XtermBlazor` requires package integration, imports, CSS/script references, and terminal sizing behavior to be handled correctly in the host shell.
- The Workbench output experience will shift from structured per-row interaction to a projected terminal stream interaction model.

## 5. Architecture direction

### 5.1 Shell ownership

- `UKHO.Workbench` continues to define immutable output contracts.
- `UKHO.Workbench.Services` continues to own append-only output entries and shell panel state transitions.
- `WorkbenchHost` projects the retained output entries into terminal text and terminal commands.

### 5.2 Rendering model

The current row-based renderer is expected to be replaced by a single terminal component hosted in the shell layout. The terminal shall be treated as a view-only projection surface rather than as a source of truth.

### 5.3 Terminal projection model

The shell will require a projection layer that transforms:
- ordered `OutputEntry` values
- panel visibility and wrap state
- auto-scroll state
- theme state

into:
- terminal text writes or a rebuilt buffer
- terminal color/styling output
- line wrapping behavior
- reveal-to-bottom behavior

### 5.4 Addon strategy

Standard official `xterm.js` addons are permitted when needed for clean feature delivery. In particular, this specification anticipates the likely use of standard addons for terminal fit and terminal search where those produce a cleaner implementation than custom shell-specific workaround code.

## 6. Functional requirements

### 6.1 Output surface

- The visible output surface shall consist of a single panel-local toolbar directly above a read-only terminal-style surface.
- The output surface shall support standard text selection and clipboard copy behavior.
- The output surface shall support dense display of long-running append-only output.
- The output surface shall not expose per-row disclosure buttons or row-level action chrome.
- Output `Details` and optional `EventCode` shall render inline beneath the summary line when present.
- The output surface shall preserve chronological ordering, with newly appended output appearing at the bottom.
- The output surface shall remain current while the panel is hidden so reopening the panel shows the latest projected output immediately.

### 6.2 Toolbar

- Output actions shall be surfaced through a single toolbar hosted inside the output panel directly above the terminal surface.
- The Workbench shell shall not introduce another separate top-level toolbar for output actions.
- `Copy selected` shall be present on the single top toolbar.
- Additional output actions such as `Clear`, `Wrap`, `Auto-scroll`, `Scroll to end`, and `Find` should be included when they can be implemented cleanly without workaround-heavy integration.
- `Find` shall be included in the first delivery when integrated through a standard `xterm.js` facility rather than a custom workaround.
- `Wrap` shall be included only when it is cleanly supported through the package integration; if not, wrap-related toolbar affordances shall be omitted from the first delivery.
- Both `Auto-scroll` and `Scroll to end` shall remain available in the first delivery.
- `Clear` shall be included only when it is cleanly supported through the package integration; if not, it shall be omitted from the first delivery.
- `Copy selected` shall be disabled when there is no active selection.
- `Find` shall be available from the output toolbar and by standard keyboard shortcut support such as `Ctrl+F`.

### 6.3 State preservation

- Existing output entry contracts shall remain stable.
- Existing output panel state ownership shall remain stable.
- The terminal surface shall not become the source of truth for output entries, wrap state, or auto-scroll state.
- Existing panel open/close behavior shall remain stable.
- Existing panel-height memory behavior shall remain stable.
- Existing hidden-panel unseen severity indicator behavior shall remain stable.

### 6.4 Theme and display

- The terminal surface shall support theme-aware presentation.
- The terminal surface shall remain compatible with the broader Workbench shell styling direction.
- The terminal shall follow the existing Workbench light/dark appearance toggle from the first delivery.
- The exact terminal palette mapping remains to be confirmed.
- The terminal shall size to fit the panel it is hosted in.
- The terminal shall automatically fit and re-fit when the panel is opened or resized.

### 6.5 Severity rendering

- Output levels shall remain visually distinguishable in the terminal surface.
- The first delivery shall colour-code `Debug`, `Info`, `Warning`, and `Error` entries.
- The implementation may use ANSI-style formatting or terminal API-driven styling, whichever integrates more cleanly with `XtermBlazor`.

### 6.6 Keyboard and pointer interaction

- The terminal shall accept normal keyboard focus in the first delivery.
- Standard keyboard copy from an active selection, such as `Ctrl+C`, shall work alongside the `Copy selected` toolbar action.
- A minimal terminal context menu may be supported if `XtermBlazor` supports it cleanly.

### 6.7 Auto-scroll behavior

- `Auto-scroll` shall disable automatically when the user scrolls away from the bottom of the output.
- `Scroll to end` shall remain the explicit command to reveal the newest output again.
- The panel reopening path shall not require a replay delay to restore the current output view.

### 6.8 Output text projection format

- The current timestamp + source + summary text format shall remain the projected summary-line contract.
- When present, `Details` and optional `EventCode` shall render inline beneath the related summary line.

## 7. Technical requirements

- The host project shall integrate `XtermBlazor` according to package requirements.
- The Workbench shell shall host the terminal in an interactive Blazor Server context.
- The terminal host shall have deterministic sizing behavior.
- The terminal shall automatically fit and re-fit to the output panel size when the panel is opened or resized.
- The shell shall provide reliable append/rebuild behavior for projected output text.
- The design shall support auto-scroll and explicit scroll-to-end behavior.
- The design shall avoid duplicating output state between the terminal component and the shared output service.
- The implementation shall preserve the existing ownership boundary: `WorkbenchHost` renders and projects, while `UKHO.Workbench` and `UKHO.Workbench.Services` remain the owners of output data and output state.
- The implementation shall remove the dependency on per-row visual rendering for the output panel body.
- The implementation shall support clean integration of standard official `xterm.js` addons where required for fit and search.
- The implementation shall support keyboard-driven `Find` and keyboard-driven copy without relying solely on pointer interaction.

## 8. Risks and considerations

- Terminal components are optimized for stream-oriented display rather than strongly structured row semantics.
- Search/find capability may require explicit addon or custom integration work depending on the chosen terminal feature set.
- Terminal resizing and fit behavior may require addon usage and shell-specific layout coordination.
- The change invalidates assumptions in the current UX uplift spec and plan and will require follow-on document updates.
- Existing row-rendering tests and assumptions are expected to become obsolete once terminal projection replaces the row-oriented UI.
- The implementation must avoid introducing shell behavior regressions while replacing the renderer.

## 9. Validation and acceptance focus

- Verify the output panel renders a panel-local toolbar directly above the terminal and does not introduce another top-level shell toolbar.
- Verify new output is appended at the bottom and remains visible immediately after reopening the hidden panel.
- Verify keyboard focus, `Ctrl+F`, `Ctrl+C`, selection, and toolbar `Copy selected` all work as specified.
- Verify severity colour treatment is distinct for `Debug`, `Info`, `Warning`, and `Error`.
- Verify auto-fit works on open and resize and that the terminal always fills the output panel correctly.
- Verify `Auto-scroll`, `Scroll to end`, hidden unseen severity indication, panel visibility, and panel height memory all continue to behave as before.

## 10. Open questions

No further critical clarification questions are currently open.

## 11. Initial implementation impact areas

- `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`
- `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.cs`
- `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`
- `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.js`
- `src/workbench/server/WorkbenchHost/Components/Layout/WorkbenchOutputRow.razor`
- `src/workbench/server/WorkbenchHost/Components/Layout/WorkbenchOutputRow.razor.cs`
- `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`
- `test/workbench/server/WorkbenchHost.Tests/WorkbenchOutputRowRenderingTests.cs`
