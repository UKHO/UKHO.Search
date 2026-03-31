# Workbench Output Trimming Specification

- Version: `v0.01`
- Status: `Draft`
- Work Package: `087-workbench-output-trimming`
- Created: `2026-03-31`
- Related work: `docs/086-workbench-output/`

## Change Log

- `v0.01` - Initial specification for reducing output-pane noise, introducing severity filtering, and trimming repeated diagnostic output.
- `v0.01` - Clarified that repeated-line reduction may use whichever implementation approach is most efficient, detail lines remain visible for all shown entries, and clearly non-useful shell-state messages should be removed or suppressed.
- `v0.01` - Clarified that the output-level filter is session-only and does not need to persist per user between sessions.
- `v0.01` - Clarified that the output-level selector alone is sufficient and no separate quick reset-to-default action is required.
- `v0.01` - Clarified that entries hidden by the current filter do not contribute to unseen indicators or badges.
- `v0.01` - Clarified that exposing debug output through the minimum-level selector alone is sufficient and no dedicated debug toggle is required.
- `v0.01` - Clarified that repeated equivalent messages should be suppressed rather than rendered with visible repeat counts.
- `v0.01` - Clarified that repeated-message suppression applies only to consecutive equivalent messages rather than across the wider retained session history.
- `v0.01` - Clarified that repeated-message suppression should apply only to lower-noise severities rather than to all severities.
- `v0.01` - Clarified that repeated-message suppression applies to `Debug` and `Info` entries rather than to `Debug` only.
- `v0.01` - Clarified that `Debug` entries still show detail lines when `Debug` output is enabled.
- `v0.01` - Clarified that `Warning` and `Error` entries are also eligible for repeated-message suppression.
- `v0.01` - Clarified that repeated-message suppression for `Warning` and `Error` entries applies to all equivalent consecutive warnings and errors.
- `v0.01` - Clarified that repeat suppression requires the full visible entry content to match, including details and event code.

## 1. Overview

### 1.1 Purpose

Define changes to the Workbench output pane so it behaves as a readable, desktop-style operator surface rather than a raw internal trace stream. The specification focuses on reducing noise in normal operation, preserving access to deeper diagnostics when needed, and preventing repeated state messages from overwhelming the visible output.

### 1.2 Scope

This work package covers the Workbench output pane experience in `WorkbenchHost`, including:

- default visibility rules for output severities
- toolbar controls for output-level filtering
- treatment of detail lines
- reduction of repeated and low-value output entries
- display behavior for repeated context and status messages
- expectations for normal-operation versus diagnostic output

This work package does not cover:

- redesign of the overall Workbench shell layout outside the output pane
- changes to external logging sinks outside the Workbench output service
- persistent storage or export of historical output beyond the current in-session behavior
- broader telemetry platform changes

### 1.3 Stakeholders

- Workbench users who need a readable output pane during normal operation
- Developers using the Workbench output pane for diagnostics
- Maintainers of `WorkbenchHost` and the Workbench shell services
- Module authors whose runtime status and context messages are projected into the output pane

### 1.4 Definitions

- **Output pane**: The bottom Workbench panel that renders retained shell and runtime output.
- **Detail lines**: Additional lines shown beneath the summary line of an output entry.
- **Severity filter**: A UI control that limits visible entries to a chosen minimum level.
- **Normal operation**: Typical end-user usage where the output pane should emphasize actionable and meaningful information.
- **Diagnostic mode**: An operator or developer-driven mode where lower-level trace information such as `Debug` is intentionally surfaced.
- **Coalescing**: Combining repeated or equivalent entries into a single visible representation.

## 2. System context

### 2.1 Current state

The current Workbench output pane behaves as a retained chronological stream and displays a large volume of low-level entries during normal operation. In practice this results in:

- a very busy output pane
- high entry counts dominated by `Debug`-style diagnostic messages
- repeated state snapshots such as repeated `Tool surface ready: True` and `Active region: ToolSurface`
- detail lines contributing additional vertical noise when many entries are already present
- a user-facing surface that feels closer to an internal trace window than a curated output window

The present behavior is useful for implementation diagnostics, but it is too noisy for default day-to-day usage.

### 2.2 Proposed state

The output pane shall present a quieter default experience suitable for normal Workbench use, while still allowing deeper diagnostics when explicitly requested.

The proposed behavior is:

- show `Info` and above by default
- provide a toolbar control that allows the user to choose the minimum visible severity
- keep `Debug` output available, but make it opt-in rather than the default view
- retain detail lines for all visible entries, but ensure they no longer dominate the pane during normal operation because the visible entry set is quieter
- reduce repeated low-value output by removing useless debug messages at source rather than by deduplicating retained output entries
- treat the output pane as a meaningful operational surface rather than an unfiltered trace sink

### 2.3 Assumptions

- The current high volume is primarily caused by debug-level shell and context projection rather than by frequent warnings and errors.
- Users still need access to debug information during troubleshooting.
- Detail lines remain useful when the total number of visible entries is materially lower.
- The Workbench should feel desktop-like, so the filter control belongs naturally in the output-pane toolbar.
- The selected output-level filter is session-only.
- The retained output history can continue to exist even when some entries are hidden by the current filter.
- Duplicate retained entries may still be shown when they are emitted; readability improvement comes from reducing useless debug emission and from the default visible filter.

### 2.4 Constraints

- The solution must fit within the existing Workbench shell and output-panel model.
- The design should remain close to the current Radzen Material appearance.
- The output pane must remain understandable for non-developer users in normal operation.
- Changes must not remove the ability to inspect debug output when troubleshooting is required.
- Repeated shell context projection should not flood the pane with unchanged values.
- The solution must not deduplicate retained output entries in the pane.

## 3. Component / service design (high level)

### 3.1 Components

The following areas are in scope:

- `WorkbenchHost` output-pane toolbar and rendering behavior
- Workbench output projection logic in `MainLayout`
- `IWorkbenchOutputService` and related panel-state behavior as needed for filter support
- shell context and status projection paths that currently emit repeated debug-style entries

### 3.2 Data flows

1. Shell and module actions publish output entries.
2. Entries are retained in the Workbench output service.
3. The output pane applies a current minimum severity filter before rendering visible entries.
4. The user may change the filter using the output-pane toolbar.
5. Repeated or equivalent low-value entries are either not emitted repeatedly or are coalesced into a clearer visible representation.
5. Repeated low-value debug messages are reduced by not emitting useless entries in the first place.
6. Detail lines are shown for entries that remain visible under the chosen filter.

### 3.3 Key decisions

- The default visible threshold shall be `Info`.
- `Debug` shall remain available but not shown by default.
- A severity selector on the output-pane toolbar is required.
- Detail lines shall be retained as a capability because they remain useful once normal noise is reduced.
- Repeated shell context and status output shall be reduced by removing useless debug messages at source, not by deduplicating retained entries.

## 4. Functional requirements

1. The Workbench output pane shall default to displaying only `Info`, `Warning`, and `Error` entries.
2. The Workbench output pane shall hide `Debug` entries by default.
3. The output-pane toolbar shall include a control that lets the user select the minimum visible output level.
4. The toolbar filter shall support at least the following options:
   - `Error`
   - `Warning and above`
   - `Info and above`
   - `Debug`
5. The default selected toolbar filter shall be `Info and above`.
6. Changing the toolbar filter shall immediately update the visible output without requiring the pane to close or reopen.
7. The output system shall continue to support debug-level entries for troubleshooting scenarios.
8. Detail lines shall continue to be supported for all visible entries.
9. The output pane shall remain usable and readable during normal operation when the default filter is active.
10. The shell shall reduce repeated emission of unchanged context snapshots such as repeated `Active region` and `Tool surface ready` values.
11. The shell shall avoid repeatedly writing equivalent consecutive messages when the underlying state has not meaningfully changed.
12. The output-pane behavior shall not suppress, coalesce, or deduplicate retained entries once they have been written.
13. The output pane shall continue to show warnings and errors prominently without requiring the user to enable a diagnostic mode.
14. The output pane shall preserve access to meaningful details for important operational entries even after noise reduction changes are introduced.
15. The output-pane filtering behavior shall apply to shell-projected context/status output as well as other retained output entries.
16. Shell-projected messages that are not useful to normal users shall not be emitted repeatedly into the visible output stream.
17. Known examples of non-useful repeated shell-state messages include `Tool surface ready: True` and `Active region: ToolSurface`, and the same trimming rule shall apply to other similarly non-useful messages.
18. The selected output-level filter shall apply only to the current session and shall reset to the default `Info and above` behavior when a new session starts.
19. The output-pane toolbar shall not require a separate quick reset-to-default action, because returning to the default `Info and above` view through the main selector is sufficient.
20. Entries hidden by the current output-level filter shall not contribute to unseen indicators or badges while they remain below the active visible threshold.
21. The output pane shall expose debug output through the main minimum-level selector and shall not require a separate dedicated debug toggle.
22. When the user enables `Debug`, `Debug` entries shall still show detail lines.

## 5. Non-functional requirements

1. The output pane shall prioritize readability over exhaustive default verbosity.
2. The default output experience shall be appropriate for non-developer users operating the Workbench normally.
3. The filtered output view shall feel responsive when the user changes severity.
4. The design shall remain consistent with the Workbench’s desktop-like UI model.
5. The toolbar additions shall remain visually aligned with the existing output-pane toolbar.
6. Noise reduction must not make warnings, errors, or user-meaningful informational events harder to discover.
7. The solution should minimize unnecessary rendering churn caused by high-frequency low-value debug events.

## 6. Data model

The output-entry model conceptually continues to include:

- entry identifier
- timestamp
- severity level
- source
- summary
- optional details
- optional event code

Additional session-level UI state may be required for:

- current minimum visible output level
- current minimum visible output level

This work package does not require a new persisted domain model.

## 7. Interfaces & integration

- The output-pane toolbar will gain a severity-selection control.
- The Workbench output rendering path will need to respect the current minimum visible level.
- Shell context/status projection paths will need revised rules so useless unchanged state is not repeatedly written as separate low-value entries.
- Module and shell output sources should continue to use the shared Workbench output service rather than introducing parallel output paths.

## 8. Observability (logging/metrics/tracing)

The Workbench output pane should no longer be treated as a direct mirror of all internal trace-level activity during normal use.

Observability expectations:

- `Debug` remains available for troubleshooting
- `Info` and above becomes the normal user-facing default
- repeated unchanged useless context snapshots should be prevented at source rather than deduplicated after emission
- warnings and errors must remain clearly visible
- detail lines remain useful for important entries once overall output intensity is reduced

Examples of currently over-noisy patterns to address include repeated output such as:

- `Tool surface ready: True`
- `Active region: ToolSurface`

These repeated values, and other similarly non-useful shell-state lines, should not appear hundreds of times in the default user experience.

## 9. Security & compliance

No new security boundary is introduced by this change.

However:

- reducing default verbosity lowers the risk of exposing internal implementation detail unnecessarily in a user-facing pane
- debug information must remain intentionally accessible rather than accidentally prominent
- any future persistence of filter preferences must not introduce unsafe cross-user leakage of session state

## 10. Testing strategy

Testing should cover:

- default filter renders `Info` and above
- debug entries are hidden by default
- toolbar filter changes update visible output correctly
- warnings and errors remain visible under expected filter selections
- detail lines still render for visible entries
- repeated unchanged useless shell context messages are no longer emitted as designed
- normal operation produces materially fewer visible lines than the current implementation
- debug mode still exposes lower-level diagnostic entries when explicitly selected

Suggested test layers:

- Workbench host rendering tests for toolbar and visible output behavior
- service and projection tests for filtering logic
- targeted tests confirming that useless repeated debug messages are no longer emitted while genuine retained output entries are not deduplicated

## 11. Rollout / migration

This change should be introduced as a refinement of the existing output-pane behavior.

Rollout expectations:

1. Introduce severity filtering with `Info and above` as the default.
2. Reduce useless repeated shell context and status output at source.
3. Verify that normal operation is quieter while diagnostic access remains available through `Debug`.
4. Confirm that retained output, warnings, and errors remain intact and discoverable.

No external data migration is required.

## 12. Open questions

None at present.
