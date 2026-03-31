# Work Package: `085-workbench-style` — Workbench shell style and layout refinement

**Target output path:** `docs/085-workbench-style/spec-workbench-style_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created to define the first Workbench shell style refinement pass for pane sizing, edge-to-edge layout, toolbar simplification, fixed left rail behavior, and central tab-strip spacing.

## 1. Overview

### 1.1 Purpose

This specification defines a focused visual and layout refinement pass for the Workbench shell.

Its purpose is to make the Workbench feel more like a desktop-style tool host by improving startup proportions, removing unnecessary outer spacing, simplifying the top toolbar, fixing the far-left rail width, and tightening the central tab presentation.

### 1.2 Scope

This specification includes:

- startup width proportions for the far-left rail, explorer pane, and centre pane
- removal of outer shell padding so the Workbench sits flush with the browser viewport
- removal of the `ACTIVE TAB` label from the toolbar area
- relocation and renaming of the current `Overview` action to `Home`
- fixed-width, non-resizeable behavior for the far-left pane
- tooltip-based labeling for far-left pane items instead of always-visible text
- removal of unnecessary padding around the centre tab control so tabs sit flush to the Workbench content edges
- preservation of the tab overflow affordance anchored to the right side of the tab strip
- shell-level styling responsibility rather than module-specific layout fixes

This specification excludes:

- redesign of module content within the hosted panes
- new docking behavior or free-form pane arrangement
- changes to explorer information architecture
- changes to tab lifecycle, activation, or overflow selection rules already covered by `084-workbench-tabs`
- broad Workbench theming beyond the targeted refinements in this work package
- persistence redesign for pane dimensions beyond current shell behavior

### 1.3 Stakeholders

- Workbench platform developers
- UX and product stakeholders for the Workbench shell
- module and tool authors whose UI is hosted inside the shell
- repository maintainers responsible for shell consistency

### 1.4 Definitions

- `far-left pane`: the slim shell rail used for top-level Workbench areas or tool categories
- `explorer pane`: the navigation pane immediately to the right of the far-left pane
- `centre pane`: the main document or view-hosting region containing the tab strip and active view
- `toolbar`: the shell header area containing the menu and current quick actions
- `flush layout`: a layout with no decorative outer padding between the shell and the browser viewport

## 2. System context

### 2.1 Current state

The current shell direction is intentionally desktop-like and uses structured layout regions rather than web-page-style content spacing.

The current Workbench still contains visual spacing and sizing choices that make the outer shell and the centre tab host feel more padded than intended.

The current toolbar still shows an `ACTIVE TAB` label that does not add enough value to justify the space it consumes.

The far-left pane currently behaves more like a resizeable pane than a fixed shell rail, and its visible text reduces the compact desktop-like appearance.

### 2.2 Proposed state

The Workbench shall open with a strongly centre-weighted layout where the explorer is narrower than the centre pane.

The default startup proportion for the resizeable explorer-to-centre split shall be approximately `1* : 4*`, or the closest equivalent supported by the shell layout implementation, while preserving the fixed-width far-left rail.

The far-left pane shall become a constant-width icon rail of approximately `64px` and shall not expose resize behavior.

The outer shell shall render flush with the browser viewport, with no decorative padding around the full-screen Workbench surface.

The `ACTIVE TAB` label shall be removed from the toolbar area.

The current `Overview` action shall move into the vacated toolbar position and shall be renamed to `Home`.

The centre tab host shall remove its extra top, bottom, and left padding so the tabs align tightly with the main Workbench content area.

The overflow affordance for centre tabs shall remain anchored at the right edge of the tab strip.

Tooltip behavior for the far-left rail shall provide discoverable labels without reintroducing permanent text in that rail.

### 2.3 Assumptions

- the Workbench should continue moving toward an IDE-like desktop shell rather than a padded web-page layout
- the far-left pane is primarily a compact navigation rail and does not need user resizing
- the existing Workbench shell already has a clear distinction between the far-left rail, explorer pane, and centre pane
- retaining the stock Radzen Material look-and-feel as much as practical remains desirable
- tab overflow behavior defined in `084-workbench-tabs` remains valid and is not being redesigned here
- tooltip-based naming is sufficient for the far-left rail in the first style refinement pass
- startup sizing should be handled by the shell itself rather than by module-specific CSS workarounds

### 2.4 Constraints

- shell sizing fixes must be implemented in the Workbench shell and not in individual modules
- the menu bar must continue to span the full window above the content regions
- both upper and lower centre tab strips, where applicable, must remain visibly rendered
- the Workbench should remain close to the stock Radzen Material theme
- no workaround-based layout hacks should be introduced when a proper shell-level implementation is available
- this work package is limited to targeted style and layout refinement rather than a broader shell redesign

## 3. Component / service design (high level)

### 3.1 Components

The following existing areas are affected:

- `WorkbenchHost` layout components responsible for rendering the shell chrome
- Workbench shell state and layout models that define startup pane dimensions
- shell CSS controlling viewport fit, spacing, and tab-strip presentation
- tab host rendering that positions the tab overflow affordance
- far-left rail item rendering that must switch from visible text labels to tooltip-backed icon presentation

### 3.2 Data flows

User-visible flow after the change:

1. The Workbench loads edge-to-edge inside the browser viewport.
2. The far-left rail renders at a fixed width of approximately `64px` with icons only.
3. Hovering a far-left rail item shows its label through a tooltip.
4. The explorer pane loads narrower than the centre pane using the shell startup ratio.
5. The toolbar shows `Home` in the prior `ACTIVE TAB` area.
6. The centre tab strip renders flush with the top, bottom, and left edges of the centre host while keeping overflow actions anchored right.

### 3.3 Key decisions

- Use a fixed-width far-left rail instead of a resizeable pane.
- Use tooltip labeling instead of persistent text in the far-left rail.
- Remove shell-level outer padding rather than compensating inside hosted views.
- Keep the explorer resizeable relative to the centre pane, but start from a noticeably narrower default ratio.
- Rename `Overview` to `Home` and place it where the removed `ACTIVE TAB` label currently sits.
- Remove centre tab host padding without changing tab overflow semantics.

## 4. Functional requirements

### 4.1 Shell layout sizing

- **FR-001** The Workbench shall render the far-left pane as a fixed-width rail of approximately `64px`.
- **FR-002** The far-left rail shall not expose user resize behavior.
- **FR-003** The explorer pane and centre pane shall start with a default width ratio of approximately `1* : 4*` respectively, excluding the fixed-width far-left rail.
- **FR-004** The explorer pane shall remain visually narrower than the centre pane on initial load.
- **FR-005** Any resize affordance between explorer and centre shall apply only to that split and shall not affect the fixed-width far-left rail.

### 4.2 Edge-to-edge shell presentation

- **FR-006** The outer Workbench shell shall remove decorative padding around the full application surface.
- **FR-007** The shell shall appear flush with the browser window on the top, bottom, left, and right edges.
- **FR-008** Removing outer padding shall not require module-specific CSS changes inside hosted tools or views.

### 4.3 Toolbar simplification

- **FR-009** The toolbar shall no longer display the `ACTIVE TAB` label.
- **FR-010** The action currently labeled `Overview` shall be renamed to `Home`.
- **FR-011** The renamed `Home` action shall be positioned where the removed `ACTIVE TAB` label previously appeared.
- **FR-012** Renaming and relocation of `Home` shall not change the underlying navigation intent of the action unless separately specified in a later work package.

### 4.4 Far-left rail presentation

- **FR-013** The far-left rail shall display icons without always-visible text labels.
- **FR-014** Each far-left rail item shall expose its name through a tooltip on hover or focus.
- **FR-015** Tooltip behavior for far-left rail items should use the existing Radzen tooltip approach already preferred in the Workbench shell.
- **FR-016** Far-left rail items shall remain discoverable and operable using their icon plus tooltip presentation.

### 4.5 Centre tab host spacing

- **FR-017** The centre tab control shall remove unnecessary internal padding on the top edge.
- **FR-018** The centre tab control shall remove unnecessary internal padding on the bottom edge.
- **FR-019** The centre tab control shall remove unnecessary internal padding on the left edge.
- **FR-020** The centre tab control may retain any minimal spacing required on the right edge only where necessary to preserve the overflow affordance behavior.
- **FR-021** The tab overflow affordance shall remain anchored to the right side of the tab strip.
- **FR-022** The tab-strip spacing change shall not alter the existing tab activation, closing, overflow listing, or overflow selection rules.

## 5. Non-functional requirements

- **NFR-001** The Workbench shall continue to present a desktop-like shell rather than a web-page-style padded layout.
- **NFR-002** The shell should remain visually close to the stock Radzen Material theme.
- **NFR-003** Layout changes shall be implemented in the shell so hosted module UIs remain unaware of shell sizing mechanics.
- **NFR-004** Tooltip-based labeling shall remain accessible to pointer and keyboard users where the underlying component model supports it.
- **NFR-005** The style refinement shall avoid introducing horizontal overflow caused solely by outer padding removal or tab-host spacing changes.
- **NFR-006** The changes should be minimal, targeted, and low-risk to existing Workbench behavior outside the scope defined in this specification.

## 6. Data model

No new domain data model is required.

The implementation may adjust existing Workbench shell layout state to express:

- fixed width for the far-left rail
- default startup proportion for the explorer and centre panes
- toolbar action labeling for `Home`

If existing shell state already persists pane dimensions, the startup defaults defined by this specification apply when establishing the default shell layout and must not require a new persistence model.

## 7. Interfaces & integration

### 7.1 Internal interfaces

Expected internal touchpoints include:

- shell layout components in `WorkbenchHost`
- Workbench shell state or layout model types under `UKHO.Workbench`
- shell management services under `UKHO.Workbench.Services` if layout defaults are coordinated there
- Radzen-based tab and tooltip integration already used by the Workbench UI

### 7.2 External interfaces

No new external service integration is required.

No API surface change is required outside the Workbench shell UI layer.

## 8. Observability (logging/metrics/tracing)

No dedicated new observability requirement is introduced for this style-only work package.

If shell diagnostics already exist for layout initialization, they may continue to report startup layout state, but no new logging should be required solely for cosmetic rendering changes.

## 9. Security & compliance

This work package does not change authentication, authorization, data handling, or compliance boundaries.

Tooltip text for far-left rail items shall be derived from the same safe UI labels already available to the shell and shall not introduce new sensitive data exposure.

## 10. Testing strategy

The implementation should be verified with targeted Workbench shell tests rather than a full test-suite run.

Recommended coverage includes:

- shell rendering tests confirming the `Home` label is present and `ACTIVE TAB` is absent
- rendering tests confirming the far-left rail no longer shows persistent text labels
- rendering or UI tests confirming tooltip-backed labeling exists for far-left rail items
- rendering tests confirming the centre tab host no longer applies the removed padding classes or styles
- targeted layout tests confirming the far-left rail is fixed width and not resizeable
- targeted layout tests confirming the explorer starts narrower than the centre pane
- targeted UI verification confirming tab overflow remains anchored right

Where practical for UI verification in this repository, prefer Playwright end-to-end coverage over component-only tests.

## 11. Rollout / migration

This work package is an in-place refinement of the existing Workbench shell.

No data migration is required.

No user training material is strictly required, though the rename from `Overview` to `Home` should be reflected in any nearby Workbench documentation if such references exist.

## 12. Open questions

No open questions are currently blocking this specification.

If implementation reveals ambiguity in how persisted pane sizes interact with the new startup defaults, that follow-up should be captured in a later version of this specification rather than expanding scope during this work package.
