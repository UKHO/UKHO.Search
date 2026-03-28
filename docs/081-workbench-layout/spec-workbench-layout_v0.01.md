# Work Package: `081-workbench-layout` — Workbench layout splitter integration

**Target output path:** `docs/081-workbench-layout/spec-workbench-layout_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for integrating resizable splitter support into the existing `UKHO.Workbench.Layout` grid system.
- `v0.01` — Records the intent to extend the lifted `UKHO.Workbench.Layout`-style layout model in `UKHO.Workbench.csproj` so it can support draggable row and column splitters using concepts proven in `BlazorSplitGrid`.
- `v0.01` — Confirms that the implementation target is the existing `Layouts` namespace in `UKHO.Workbench`, with an optional `Layouts.Splitters` sub-namespace if useful.
- `v0.01` — Confirms that all implementation code must remain inside `UKHO.Workbench.csproj` rather than introducing a separate reusable package or project.
- `v0.01` — Confirms that the enhancement is intended to become the foundation for layout behavior in a future `WorkbenchHost` work package.
- `v0.01` — Confirms that splitter support is required for both rows and columns.
- `v0.01` — Confirms that only track definitions using fixed pixel sizes or star sizing are eligible for resize behavior.
- `v0.01` — Confirms that `Auto`-sized rows and columns are explicitly out of scope for splitter resizing because the underlying split-grid library does not support them.
- `v0.01` — Captures the preferred consumer-facing markup direction as either a dedicated splitter definition element or an `IsSplitter` flag on a normal row/column definition, with the final choice to be driven by implementation fit.
- `v0.01` — Confirms that the existing `UKHO.Workbench.Tests` suite must be fully augmented to cover the new behavior and should absorb the useful behavioural coverage patterns currently present in `BlazorSplitGrid.Tests.csproj`.
- `v0.01` — Confirms that resized track sizes are runtime-only state for the active rendered grid instance and that persistence or restoration is out of scope for this work package.
- `v0.01` — Confirms that unsupported splitter configurations involving `Auto` rows or columns must fail fast with an explicit error rather than degrading silently.
- `v0.01` — Confirms that the specification does not mandate a single consumer-facing splitter syntax and may allow either dedicated splitter definitions or an `IsSplitter` flag, with implementation free to choose the simpler fit.
- `v0.01` — Confirms that this work package must expose public resize lifecycle and size-changed callbacks or events, similar in spirit to the event model offered by `BlazorSplitGrid`.
- `v0.01` — Confirms that `UKHO.Workbench` must provide built-in default splitter styling: transparent at rest with the appropriate resize cursor, and blue on hover.
- `v0.01` — Confirms that min/max size constraints are out of scope for this work package and should be deferred rather than required now.
- `v0.01` — Confirms that grids may contain multiple splitters and that complex layouts with more than one splitter in the same grid must be supported.
- `v0.01` — Confirms that a single `Grid` instance may combine both row splitters and column splitters in the same layout.
- `v0.01` — Confirms that resize lifecycle and size-changed notifications must expose both pixel measurements and the raw resolved CSS `grid-template` track string.
- `v0.01` — Confirms that splitter interaction may be mouse-only in this work package, with keyboard accessibility documented as a follow-on concern rather than a current requirement.
- `v0.01` — Confirms that pointer and touch interaction are out of scope for this work package and that desktop mouse interaction is sufficient for the initial delivery.
- `v0.01` — Confirms that resized star-sized tracks do not need a prescribed runtime representation after drag; implementation may choose the simplest correct approach.
- `v0.01` — Confirms that the notification API does not require a prescribed row/column event shape, provided row/column context is included in the exposed event data.
- `v0.01` — Confirms that snap-to-minimum resize behaviour is implementation-defined for this work package and may be included if straightforward, but is not required.
- `v0.01` — Confirms that existing content spanning rules should remain unchanged and that no new splitter-specific spanning constraints are required in this work package.
- `v0.01` — Confirms that resize notifications should be raised continuously during dragging, with completion notifications also allowed where useful.
- `v0.01` — Confirms that no completed implementation code may remain under `./scratch`; all production implementation must be incorporated into `UKHO.Workbench` when the work is complete.
- `v0.01` — Confirms that the JavaScript-side implementation approach is not prescribed, provided the final adopted code and assets live inside `UKHO.Workbench`.
- `v0.01` — Confirms that the Workbench layout developer documentation must be updated as part of this work package, including splitter usage, supported patterns, and constraints.
- `v0.01` — Confirms that explicit attribution or provenance notes for adapted third-party code are not required by this work package beyond any existing legal or repository obligations.
- `v0.01` — Confirms that the specification must include explicit performance safeguards for high-frequency drag notifications, such as debouncing, throttling, or equivalent guidance and controls.
- `v0.01` — Confirms that nested splitter-enabled grids must be supported, including scenarios where a `Grid` using splitters is hosted inside another `Grid`.
- `v0.01` — Confirms that splitter behaviour must coexist cleanly with existing `RowGap` and `ColumnGap` support.
- `v0.01` — Confirms that when gaps are configured, splitter track size is additional to the configured `RowGap` or `ColumnGap` rather than replacing the gap.
- `v0.01` — Confirms that splitter tracks count as ordinary grid tracks in developer-facing row and column indexing for content placement.
- `v0.01` — Confirms that the splitter API should preserve the existing `ColumnDefinition` / `RowDefinition` naming style rather than introducing a new shorter `Column` / `Row` family.
- `v0.01` — Confirms that splitter tracks are reserved as non-content gutters and are not intended to host normal grid content.
- `v0.01` — Confirms that explicit identifiers or names for splitter-capable definitions are implementation-defined and not mandatory, provided diagnostics remain usable.
- `v0.01` — Confirms that invalid splitter configurations should fail by throwing an exception during component initialization or rendering rather than degrading into a non-functional grid.
- `v0.01` — Confirms that within the preserved definition model, the preferred splitter declaration shape is dedicated definitions such as `SplitterColumnDefinition` and `SplitterRowDefinition`.
- `v0.01` — Confirms that the specification does not require a shared base abstraction or contract for ordinary and splitter definitions and leaves that internal type design to implementation.
- `v0.01` — Confirms that resize notifications and related diagnostics should include affected splitter or track index information.
- `v0.01` — Confirms that splitter assets and initialization should be automatic so using the component is sufficient without manual host-page includes.
- `v0.01` — Confirms that splitter-enabled grids may still contain `Auto` rows or columns in non-resizable positions, provided no splitter attempts to resize those tracks.
- `v0.01` — Confirms that splitter gutter thickness should remain fixed by the declared splitter width or height and does not require a separate minimum thickness setting.
- `v0.01` — Confirms that splitter cursor behaviour should be fixed by direction, using the appropriate resize cursor rather than allowing consumer cursor overrides.
- `v0.01` — Confirms that splitter dragging should remain continuous only in this work package and does not need a configurable drag interval or drag granularity setting.
- `v0.01` — Confirms that double-click reset behaviour is out of scope for this work package and should not be included.
- `v0.01` — Confirms that affected splitter or track indices exposed in notifications and diagnostics should use `1-based` numbering to match the existing grid authoring model.
- `v0.01` — Confirms that mixed resizable adjacent track pairs such as `fixed + star` are supported.
- `v0.01` — Confirms that splitter definitions must sit between two resizable tracks and are not allowed at the outer edges of a grid.
- `v0.01` — Confirms that built-in default splitter styling is sufficient for this work package and that no additional consumer styling hooks are required.
- `v0.01` — Confirms that continuous resize notifications are sufficient on their own and that separate drag-start and drag-end notifications are not required.
- `v0.01` — Confirms that resize notifications should use a single unified surface with explicit row/column direction information rather than separate row-specific and column-specific notification surfaces.
- `v0.01` — Confirms that a built-in default splitter thickness should be provided when a consumer omits an explicit splitter width or height.
- `v0.01` — Confirms that the built-in default splitter thickness is `4px` when a consumer omits an explicit splitter width or height.
- `v0.01` — Confirms that the built-in blue hover highlight should remain the default, while consumers may still override the hover highlight colour.
- `v0.01` — Confirms that the current draft is sufficiently resolved and no further clarification questions are required before implementation.
- `v0.01` — Confirms that the notification event model may choose whether drag-start and drag-end notifications are exposed separately, provided the implemented event model is documented clearly.
- `v0.01` — Confirms that resize notification payload scope is implementation-defined, provided the chosen convention is documented clearly.
- `v0.01` — Confirms that numbering for affected splitter or track indices in notifications and diagnostics is implementation-defined, provided the convention is documented clearly.
- `v0.01` — Confirms that the internal abstraction model for ordinary and splitter definitions is implementation-defined, provided the public API remains coherent.

## 1. Overview

### 1.1 Purpose

This specification defines the next increment of the Workbench layout foundation by extending the existing WPF-style Blazor grid wrapper in `UKHO.Workbench.Layout` with splitter-based resizing.

The purpose is to preserve the current developer-friendly XAML-style grid authoring model while enabling draggable resizing of selected rows and columns so future Workbench screens can support richer desktop-style layouts without abandoning CSS Grid.

### 1.2 Scope

This specification currently includes:

- enhancing the existing `UKHO.Workbench.Layout` grid abstraction to support draggable splitter tracks
- supporting resizable column and row scenarios
- defining authoring rules for which tracks may participate in splitter behavior
- integrating the relevant concepts from the `BlazorSplitGrid` implementation into the existing Workbench layout code rather than adding a separate UI layout library
- updating the Workbench test suite to cover the new layout and splitter behavior thoroughly
- updating the Workbench layout developer documentation to describe splitter usage and constraints
- keeping the resulting layout API approachable for developers with a WPF background

This specification currently excludes:

- introducing a separate `BlazorSplitGrid` project into the solution as a direct runtime dependency
- leaving completed implementation code under `./scratch`
- supporting splitter resizing on `Auto` rows or columns
- adding persistence, serialization, or user-profile storage for resized track sizes
- broader `WorkbenchHost` shell composition work beyond the layout foundation itself
- moving layout code outside `UKHO.Workbench.csproj`

### 1.3 Stakeholders

- Workbench platform developers
- UKHO developers building future Workbench screens
- maintainers of `UKHO.Workbench` and `UKHO.Workbench.Tests`
- future `WorkbenchHost` feature teams that will rely on this layout foundation

### 1.4 Definitions

- `UKHO.Workbench.Layout`: the existing WPF-style grid authoring model that has already been lifted into the `Layouts` namespace in `UKHO.Workbench`
- `BlazorSplitGrid`: a Blazor wrapper over `split-grid` used here as the reference implementation for CSS Grid splitter behavior
- `splitter track`: a row or column definition whose rendered purpose is to act as a draggable gutter between adjacent content tracks
- `fixed track`: a row or column whose size is expressed as a fixed pixel value
- `star track`: a row or column whose size is expressed using WPF-style star sizing such as `*` or `2*`
- `auto track`: a row or column whose size is determined by content and which is not eligible for splitter resizing in this work package

## 2. System context

### 2.1 Current state

The repository already contains an in-repo copy of the `UKHO.Workbench.Layout` concepts under `src/Workbench/server/UKHO.Workbench/Layout`. That layout model brings WPF-like grid authoring to Blazor by allowing developers to define rows, columns, and positioned content with a familiar mental model.

This is considered a strong fit for the Workbench direction because many expected contributors come from a desktop WPF background and benefit from a layout model that resembles `Grid`, row definitions, column definitions, and span-based placement.

The current limitation is that the lifted layout implementation does not support draggable resizing through splitters. Separately, the repository contains source and tests for `BlazorSplitGrid` under `scratch/BlazorSplitGrid`, which demonstrates how `split-grid` can add draggable gutters on top of CSS Grid.

### 2.2 Proposed state

After this work package:

- the existing `UKHO.Workbench.Layout` grid system continues to be the canonical Workbench layout API
- the layout system supports optional splitter behavior for both rows and columns
- consumers can declare splitter participation directly in grid definitions using a Workbench-native API shape
- only fixed-width/fixed-height and star-sized tracks participate in resize calculations
- `Auto` tracks remain usable in the general layout system but cannot be made resizable through splitters
- the implementation reuses the proven ideas from `BlazorSplitGrid` while remaining packaged and named as part of `UKHO.Workbench`
- the `UKHO.Workbench.Tests` suite contains full behavioural coverage for the new feature set
- resized splitter positions exist only as runtime state for the active rendered grid instance and are not persisted by this work package
- invalid splitter configurations involving `Auto` tracks are surfaced explicitly so developers receive immediate feedback during authoring or initialization
- the specification may permit either a dedicated splitter definition syntax or an `IsSplitter` flag syntax, with implementation choosing the simpler native fit for the lifted layout model
- the layout surface exposes public notifications for splitter drag lifecycle and resulting size changes so consumers can react to user-driven resize activity
- the layout surface provides default splitter styling so splitters are discoverable without consumer CSS, remaining transparent at rest and highlighting blue on hover
- min/max size constraint support is deferred and is not required for the first splitter-enabled Workbench layout increment
- a single grid may contain multiple splitter tracks so more complex Workbench layouts can be composed without switching to nested grids purely for resize support
- a single grid may combine both row and column splitters so two-dimensional Workbench layouts can be resized within one `Grid` instance
- resize notifications expose both track sizes in pixels and the raw resolved CSS grid-template string so consumers can react at either semantic level
- splitter interaction is mouse-driven in this work package and does not yet require keyboard adjustment support
- splitter interaction is limited to desktop mouse input in this work package, with touch and broader pointer-device support deferred
- resized star-sized tracks may be represented however the implementation finds simplest after drag, provided the visible resize behaviour and notification outputs remain correct
- the notification surface may use either separate or unified events as long as row/column context is explicit in the payload
- snap-to-minimum behaviour is optional in this work package and may be included only if it falls out naturally from the chosen implementation
- existing grid content spanning rules remain in force even when splitter tracks are present, with no new splitter-specific spanning model required
- consumers can react to resize progress continuously during drag rather than only after drag completion
- the JavaScript-side implementation may adapt existing `BlazorSplitGrid` support code or use a fresh Workbench-native approach, provided the final adopted assets live in `UKHO.Workbench`
- the in-repo Workbench layout README is updated so developers can discover splitter syntax, supported scenarios, and unsupported constraints directly from the layout documentation
- the work package does not add an explicit attribution or provenance-note requirement beyond normal repository and licensing expectations
- high-frequency drag notifications are expected to include performance safeguards or guidance so consumers are not forced into naïve expensive handlers
- splitter-enabled grids can be composed inside other grids without placing nested scenarios out of scope for the feature
- splitter-enabled layouts must continue to work correctly when existing grid gaps are configured
- configured gaps remain in effect when splitters are present, with splitter track size contributing additional spacing rather than consuming the gap itself
- splitter tracks participate in grid indexing, so row and column coordinates refer to the full declared track list including splitter tracks
- the preferred public API shape stays aligned with the lifted Workbench layout naming model based on `ColumnDefinition` and `RowDefinition`
- splitter tracks are dedicated gutter tracks and are not part of the normal content-placement surface
- explicit identifiers for splitter-capable definitions are optional rather than required, so long as diagnostics still provide usable context
- invalid splitter configuration is surfaced immediately through component failure rather than only through passive diagnostics
- the preferred declaration model uses dedicated splitter definition types rather than overloading ordinary definitions with an `IsSplitter` flag
- the internal relationship between ordinary and splitter definition types is not prescribed by the specification
- resize notifications and diagnostics identify affected splitter or track indices so handlers can correlate activity with a specific part of the grid
- splitter asset wiring and initialization are automatic, with no manual host-page script or stylesheet include required for normal use
- `Auto` rows or columns may still exist in the same grid when they are not part of a splitter-resized track pair
- splitter gutter thickness is controlled solely by the declared splitter size rather than an additional minimum-thickness setting
- splitter cursors are fixed by splitter direction so column splitters use a column-resize cursor and row splitters use a row-resize cursor
- splitter dragging is continuous rather than interval-based and does not require a configurable drag-step setting
- double-click reset of adjacent tracks back to originally declared sizes is not included in this work package
- affected splitter or track index numbering in notifications and diagnostics uses `1-based` numbering so it aligns with existing developer-facing grid coordinates
- adjacent resizable tracks may mix supported sizing kinds, including `fixed + star`
- splitter definitions are only valid when placed between two resizable tracks and are not valid at the outer grid edges
- built-in splitter styling is sufficient for this work package and explicit consumer styling hooks are not required
- continuous resize notifications are sufficient without requiring separate drag-start and drag-end notifications
- resize notifications are exposed through a unified direction-annotated surface rather than separate row and column notification families
- splitter definitions may omit an explicit width or height and fall back to a built-in default thickness
- the built-in default splitter thickness is `4px`
- the built-in blue hover highlight remains the default, but consumers may override the hover highlight colour
- the event model may choose whether drag-start and drag-end notifications are separate from continuous resize notifications, provided the chosen model is documented clearly
- resize notifications may report either only the directly adjacent tracks or the full affected track set for that direction, provided the chosen payload scope is documented clearly
- the internal type hierarchy for ordinary and splitter definitions is left to implementation rather than prescribed by the specification

### 2.3 Assumptions

- the current lifted layout code is already the intended long-term authoring model for Workbench screens
- `split-grid` style behaviour can be adapted into the existing implementation without replacing the base grid abstraction entirely
- developer ergonomics for WPF-oriented authors are a primary design driver
- splitter tracks may need a visually distinct definition shape even if the backing implementation still maps to row/column definitions internally
- thorough automated tests are required because this feature affects the foundational layout primitive for future Workbench UI work
- the tests in `scratch/BlazorSplitGrid` are suitable as reference coverage but must be translated into the `UKHO.Workbench.Tests` structure rather than mirrored blindly
- resized track values do not need a bindable or persisted state model in this work package

### 2.4 Constraints

- all production implementation code must reside in `UKHO.Workbench.csproj`
- no completed implementation code may reside under `./scratch`
- all relevant tests must reside in `UKHO.Workbench.Tests.csproj`
- the layout API must support both row and column resizing
- only fixed-size and star-size tracks may participate in splitter resizing
- `Auto` tracks must not be supported for resize behavior
- unsupported `Auto`-track splitter configurations must fail fast with an explicit error
- the enhancement should feel native to the existing `Layouts` namespace and not like a bolted-on separate control family
- a dedicated `Layouts.Splitters` sub-namespace may be introduced only if it improves clarity without fragmenting the developer experience

## 3. Component / service design (high level)

### 3.1 Components

1. `Grid authoring surface`
   - the existing `Grid` component and its row/column definition authoring model
   - remains the primary consumer entry point

2. `Track definition model`
   - the row and column definition types currently used by the Workbench layout system
   - extended so some tracks can act as splitter gutters or declare splitter behaviour

3. `Splitter integration layer`
   - an internal Workbench layout concern that translates eligible row/column definitions into split-grid compatible runtime configuration
   - responsible for enforcing unsupported cases such as `Auto`

4. `Client-side interop assets`
   - JavaScript and any supporting styles needed to enable dragging and CSS Grid track recalculation
   - remain owned by `UKHO.Workbench`

5. `Workbench layout tests`
   - component, rendering, and behaviour tests in `UKHO.Workbench.Tests`
   - cover authoring rules, runtime rendering expectations, invalid scenarios, and resized outcomes where testable

### 3.2 Data flows

#### Authoring flow

1. A Workbench developer declares a `Grid` using the existing WPF-style row and column concepts.
2. The developer marks a specific row or column definition as a splitter track using the final chosen API shape.
3. The grid validates that neighbouring or participating tracks are eligible for resizing.
4. The grid renders CSS Grid markup plus the required splitter hooks.

#### Runtime resize flow

1. The rendered grid initializes the splitter integration over eligible CSS Grid tracks.
2. The user drags a row or column splitter.
3. The client-side splitter logic recalculates the affected grid template track sizes.
4. The grid reflects the resized state in the rendered layout for the active component instance.

### 3.3 Key decisions

- the Workbench layout model remains the primary abstraction; splitter support is an enhancement, not a replacement
- the implementation should borrow proven behaviour from `BlazorSplitGrid` but present it through a Workbench-native API
- the consumer API may use either dedicated splitter definitions or a splitter flag on existing definitions, depending on which integrates more cleanly with the lifted layout model
- `Auto` sizing is intentionally excluded from splitter support rather than partially supported with unpredictable behaviour
- tests from both the lifted layout system and `BlazorSplitGrid` must inform the final `UKHO.Workbench.Tests` coverage baseline

## 4. Functional requirements

1. The system shall preserve the existing `Grid`-based WPF-style layout authoring experience in `UKHO.Workbench.Layout`.
2. The system shall support draggable splitter behaviour for columns.
3. The system shall support draggable splitter behaviour for rows.
4. The system shall provide a consumer-facing way to declare that a row or column definition acts as a splitter track.
5. The system may satisfy this declaration requirement using either dedicated splitter definitions or an `IsSplitter` flag on existing definitions, with the implementation choosing the simpler native fit.
6. The system shall permit splitter-enabled resizing only when the participating content tracks use fixed-size or star-size definitions.
7. The system shall fail fast with an explicit error when a consumer configures splitter-enabled resizing for an `Auto` row or column definition.
8. The system shall support authoring a splitter track using a fixed-size gutter width or height.
9. The system shall integrate the runtime splitter behaviour into the existing `Grid` rendering model rather than requiring consumers to switch to a separate top-level component family.
10. The system shall expose public callbacks or events for continuous resize notifications, with optional additional lifecycle notifications if the implementation chooses to provide them.
11. The system shall provide default splitter styling from `UKHO.Workbench`, with splitters transparent at rest, showing the appropriate resize cursor, and highlighting blue on hover.
12. The system may allow consumers to override the hover highlight colour while retaining the built-in blue as the default.
13. The system shall support multiple splitter tracks within the same grid.
14. The system shall support combining row splitters and column splitters within the same grid.
15. The system shall expose both pixel measurements and the raw resolved CSS `grid-template` string in resize lifecycle and size-changed notifications.
16. The system may limit splitter interaction to mouse-driven behaviour in this work package.
17. The system may limit splitter interaction to desktop mouse input in this work package.
18. The system may choose the simplest correct runtime representation for resized star-sized tracks after drag.
19. The system shall use a unified notification model for row and column resize activity, with row/column direction included in the notification payload.
20. The system may include snap-to-minimum resize behaviour if it is straightforward in the chosen implementation, but snapping is not a mandatory requirement of this work package.
21. The system shall preserve the existing content spanning rules when splitter tracks are present and shall not require a new splitter-specific spanning model in this work package.
22. The system shall not leave completed implementation code under `./scratch`; all adopted splitter-related code shall be incorporated into `UKHO.Workbench`.
23. The system shall raise resize notifications continuously during dragging and may also raise completion notifications when dragging ends.
24. The system may use either an adapted `BlazorSplitGrid` support-code approach or a fresh Workbench-native JavaScript implementation, provided the final adopted code and assets live inside `UKHO.Workbench`.
25. The system shall update `src/Workbench/server/UKHO.Workbench/Layout/README.md` to document splitter authoring, supported usage patterns, emitted notifications, and unsupported constraints such as `Auto` sizing.
26. The system does not need to add explicit attribution or provenance notes for adapted third-party code as a requirement of this work package, beyond any existing legal or repository obligations.
27. The system shall provide explicit performance safeguards or guidance for high-frequency drag notifications, such as debouncing, throttling, or equivalent controls.
28. The system shall support nested splitter-enabled grids, including a splitter-enabled `Grid` hosted within another `Grid`.
29. The system shall support splitter behaviour alongside existing `RowGap` and `ColumnGap` configuration.
30. When splitters are used in a grid with configured gaps, splitter track width or height shall be additional to the configured gap rather than replacing that gap.
31. The system shall count splitter tracks as ordinary tracks in developer-facing row and column indexing for content placement.
32. The system shall preserve the existing `ColumnDefinition` / `RowDefinition` naming style for the splitter-capable API surface.
33. The system shall treat splitter tracks as non-content gutters and shall not support placing normal grid content into splitter tracks.
34. The system may expose explicit identifiers or names for splitter-capable definitions, but such identifiers are not mandatory provided diagnostics remain usable.
35. The system shall fail invalid splitter configurations by throwing an exception during component initialization or rendering.
36. The system shall prefer dedicated splitter definition types such as `SplitterColumnDefinition` and `SplitterRowDefinition` within the existing definition model.
37. The system shall not require a shared base abstraction or contract for ordinary and splitter definitions as part of this work package.
38. The system shall include affected splitter or track index information in resize notifications and related diagnostics.
39. The system shall automatically provide any required splitter assets and initialization when the component is used, without requiring manual host-page includes.
40. The system may contain `Auto` rows or columns in non-resizable positions, provided no splitter attempts to resize those tracks.
41. The system shall derive splitter gutter thickness solely from the declared splitter width or height and shall not require a separate minimum gutter thickness setting.
42. The system shall use direction-appropriate fixed resize cursors for splitters and shall not require consumer cursor overrides.
43. The system shall keep splitter dragging continuous in this work package and shall not require a configurable drag interval or drag granularity option.
44. The system shall not include double-click reset behaviour for splitter tracks in this work package.
45. The system shall use `1-based` numbering for affected splitter or track indices in notifications and diagnostics so they align with existing developer-facing grid numbering.
46. The system shall support mixed adjacent resizable track pairs such as `fixed + star`.
47. The system shall require every splitter definition to sit between two resizable tracks and shall not allow splitter definitions at outer grid edges.
48. The system may report either the directly adjacent tracks or the full affected track set in resize notifications, provided the chosen payload scope is documented clearly.
49. The system shall rely on built-in default splitter styling and shall not require additional consumer styling hooks in this work package.
50. The system shall not require separate drag-start and drag-end notifications, though implementations may add them if documented clearly.
51. The system shall use a unified notification surface for row and column resize activity, with direction information included in the payload.
52. The system shall provide a built-in default splitter thickness of `4px` when a consumer omits an explicit splitter width or height.
53. The system may allow consumers to override the hover highlight colour while retaining the built-in blue as the default.
54. The system shall keep all production code for this feature inside `UKHO.Workbench.csproj`.
55. The system shall keep all automated tests for this feature inside `UKHO.Workbench.Tests.csproj`.
56. The test suite shall extend the existing Workbench layout tests to cover the new splitter authoring model.
57. The test suite shall incorporate the relevant behavioural scenarios currently covered by `BlazorSplitGrid.Tests.csproj`.
58. The test suite shall verify both row and column splitter scenarios.
59. The test suite shall verify supported fixed-size and star-size resize scenarios.
60. The test suite shall verify mixed supported resize scenarios such as `fixed + star`.
61. The test suite shall verify unsupported `Auto` sizing scenarios, including explicit fail-fast error behaviour.
62. The test suite shall verify the public resize callbacks or events.
63. The test suite shall verify that resize notifications expose both pixel measurements and raw `grid-template` strings.
64. The test suite shall verify that the implemented notification payload scope is consistent and documented.
65. The test suite shall verify continuous resize notifications during drag, with completion notifications where implemented.
66. The test suite shall verify that separate drag-start and drag-end notifications are not required.
67. The test suite shall verify that row and column resize activity flows through a unified direction-annotated notification surface.
68. The test suite shall verify the `4px` default splitter thickness when explicit splitter size is omitted.
69. The test suite shall verify any supported hover-highlight colour override behaviour while retaining the built-in blue as the default.
70. The test suite shall verify any provided performance safeguards or guidance for high-frequency notification handling.
71. The test suite shall verify multiple splitter tracks within the same grid.
72. The test suite shall verify grids that combine both row and column splitters.
73. The test suite shall verify nested splitter-enabled grids.
74. The test suite shall verify splitter behaviour alongside existing `RowGap` and `ColumnGap` configuration.
75. The test suite shall verify row and column indexing behaviour when splitter tracks are present.
76. The test suite shall verify that splitter tracks are treated as non-content gutters.
77. The test suite shall verify that notifications and diagnostics identify affected splitter or track indices.
78. The test suite shall verify automatic splitter asset wiring and initialization without manual host-page setup.
79. The test suite shall verify continuous dragging behaviour without a configurable drag-interval setting.
80. The test suite shall verify that double-click reset behaviour is not included.
81. The test suite shall verify that edge splitter definitions are rejected.
82. The resulting layout model shall remain understandable and approachable for developers familiar with WPF grid authoring.

## 5. Non-functional requirements

1. The feature should feel like a natural extension of the existing Workbench layout model.
2. The API should optimize for readability and low surprise for WPF-oriented developers.
3. The implementation should minimize conceptual duplication between the lifted grid system and the adopted splitter behaviour.
4. The layout foundation should be suitable for reuse in future `WorkbenchHost` screen composition work.
5. The tests should provide strong regression protection because the feature affects a foundational UI primitive.

## 6. Data model

The feature is primarily UI- and component-model-focused.

Expected model changes are limited to layout definition metadata such as:

- whether a track is a splitter
- whether a track is resizable
- runtime mapping between declarative track definitions and rendered gutter configuration

No separate persisted data model is currently required.

Resized splitter positions are runtime-only and do not need to be restored across sessions or component re-creation in this work package.

Track min/max constraint metadata for splitter behaviour is deferred and not required in this work package.

## 7. Interfaces & integration

1. `Grid authoring interface`
   - the existing `Grid` syntax remains the primary authoring interface
   - the final API must support declaring splitter tracks in a way that fits naturally with the current row/column definition model

2. `Client-side integration`
   - the Workbench layout implementation must integrate the required JavaScript splitter behaviour over CSS Grid
   - any required static asset loading must remain internal to `UKHO.Workbench`

3. `Consumer notification interface`
   - the Workbench layout API must expose public notifications for drag progress and resulting row or column size changes
   - separate drag-start and drag-end notifications are optional rather than required
   - row and column resize activity should use a unified notification surface with direction included in the payload
   - the final event shape may be Workbench-native but should preserve the useful behavioural intent already demonstrated by `BlazorSplitGrid`
   - notification payloads must include both pixel measurements and the raw resolved CSS `grid-template` string

4. `Styling interface`
   - `UKHO.Workbench` must provide default splitter styling so splitters are discoverable without consumer customization
   - the default appearance must be transparent at rest, show the appropriate resize cursor, and highlight blue on hover
   - consumers may override the hover highlight colour, but the built-in default remains blue

5. `Test integration`
   - the Workbench test project must cover rendering, integration assumptions, and failure rules relevant to splitter behaviour

## 8. Observability (logging/metrics/tracing)

No dedicated logging, metrics, or tracing requirements are currently identified for this layout feature.

If diagnostics are added for invalid splitter configuration, they should remain lightweight and developer-focused.

## 9. Security & compliance

This feature does not currently introduce a distinct security boundary.

Any imported or adapted third-party code must remain compatible with repository licensing and internal review expectations.

## 10. Testing strategy

The implementation must be validated through targeted automated tests in `UKHO.Workbench.Tests`.

The test strategy shall include:

- existing layout behaviour regression coverage where splitter support interacts with current grid capabilities
- column splitter rendering and behaviour scenarios
- row splitter rendering and behaviour scenarios
- supported fixed-size track scenarios
- supported star-size track scenarios
- unsupported `Auto` track scenarios and their explicit error behaviour
- public resize lifecycle and size-changed notifications
- notification payloads that include both pixel measurements and raw `grid-template` strings
- continuous resize notifications during drag, with completion notifications where provided
- default splitter styling and hover-state behaviour
- multiple splitter tracks within the same grid
- mixed row-splitter and column-splitter scenarios within the same grid
- nested splitter-enabled grids
- splitter behaviour alongside existing `RowGap` and `ColumnGap` configuration
- splitter spacing as additional to configured grid gaps
- content placement indexing when splitter tracks are present
- non-content gutter treatment for splitter tracks
- affected splitter or track index information in notifications and diagnostics
- automatic splitter asset wiring and initialization
- coexistence of non-resizable `Auto` tracks elsewhere in splitter-enabled grids
- gutter thickness derived solely from declared splitter size
- direction-fixed splitter cursor behaviour
- continuous dragging without configurable drag granularity
- absence of double-click reset behaviour
- `1-based` numbering for affected splitter or track indices
- mixed supported adjacent resize pairs such as `fixed + star`
- rejection of splitter definitions at outer grid edges
- reliance on built-in default splitter styling without extra consumer styling hooks
- continuous resize notifications as the required notification surface, with optional extra lifecycle notifications
- unified direction-annotated notification surface for row and column resize activity
- built-in default splitter thickness when explicit size is omitted
- `4px` as the default splitter thickness
- blue hover highlight as the default, with consumer override allowed
- documented notification payload scope for affected tracks
- unchanged content spanning behaviour in the presence of splitter tracks
- any adapter or mapping logic introduced to translate Workbench definitions into splitter runtime configuration
- coverage inspired by the old `BlazorSplitGrid` tests where those scenarios remain relevant after adaptation into the Workbench model

## 11. Rollout / migration

This feature is an in-place enhancement to the current Workbench layout foundation.

No separate rollout stage is currently required beyond implementing the new capability, updating tests, and making the resulting API available to future Workbench consumers.

## 12. Open questions

No open questions currently remain. The draft is sufficiently resolved for implementation.
