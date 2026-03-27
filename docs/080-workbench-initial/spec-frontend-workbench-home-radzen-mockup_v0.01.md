# Work Package: `080-workbench-initial` — `WorkbenchHost` Radzen home page mock-up

**Target output path:** `docs/080-workbench-initial/spec-frontend-workbench-home-radzen-mockup_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for a temporary Radzen-based visual mock-up on `WorkbenchHost` `Home.razor`.
- `v0.01` — Captures the requirement to follow the official Radzen get-started guidance for introducing Radzen into `WorkbenchHost`.
- `v0.01` — Captures the requirement to remove the temporary `Hello UKHO Workbench` content and the temporary listing code from `Home.razor`.
- `v0.01` — Captures the requirement for a purely visual mock-up focused on look and feel rather than real content or behavior.
- `v0.01` — Captures the requirement for left and right edge-anchored icon strips with expandable sidebars and a simple `hello` main content area.
- `v0.01` — Captures the requirement that no tests are needed for this mock-up work.
- `v0.01` — Confirms that each side behaves independently, with one open panel per side at a time and repeat-click closing the active panel.
- `v0.01` — Confirms that opening either sidebar pushes and resizes the middle area rather than overlaying it.
- `v0.01` — Confirms that both sidebars should be user-resizable using Radzen splitters, with the left sidebar resizing rightwards, the right sidebar resizing leftwards, and a maximum width of `300`.
- `v0.01` — Confirms that both sidebars are closed by default on initial page load.
- `v0.01` — Confirms that the left and right icon strips should use a Workbench or tool-style visual treatment with icon-only buttons, clear active and hover states, no visible text labels, and optional tooltip names similar to Theia activity bars.
- `v0.01` — Confirms that both left and right icon strips should be full-height activity bars running from top to bottom.
- `v0.01` — Confirms that opened left and right sidebar panels should also run full height.
- `v0.01` — Confirms that the main area should remain visually neutral with just `hello`, and should not introduce panels or cards unless explicitly requested.
- `v0.01` — Confirms that the mock-up should work in both light and dark themes.
- `v0.01` — Confirms that the main area should include a toggle button alongside the `hello` text.
- `v0.01` — Confirms that the central toggle button should switch the page theme between light and dark.
- `v0.01` — Confirms that theme choice should not persist after refresh because this is temporary look-and-feel code focused on visual behavior.
- `v0.01` — Confirms that sidebar initial open width should use the Radzen default behavior if available and can be refined later.
- `v0.01` — Confirms that the mock-up should not include a top header or command bar.
- `v0.01` — Confirms that the left and right activity bars should use asymmetric icon counts, with two icons on one side and three on the other.
- `v0.01` — Confirms that the left activity bar should use three icons and the right activity bar should use two icons.
- `v0.01` — Confirms that icon groups in both full-height activity bars should be top-aligned.
- `v0.01` — Confirms that sidebar content areas should use subtle separators against the main area.
- `v0.01` — Records that the supplied screenshot is general visual direction only, while bottom panel treatment and panel contents remain out of scope.
- `v0.01` — Confirms that only the left activity bar should include a bottom-anchored utility icon, using a gear visual.
- `v0.01` — Confirms that the left bottom utility icon is separate from the main icon count, giving the left side `3 + gear`, while the right side has `2` icons and no utility icon.
- `v0.01` — Confirms that bottom utility icons are visual only for this temporary mock-up and do not open sidebars.
- `v0.01` — Confirms that the mock-up should use subtle open and close animation only.
- `v0.01` — Confirms that opened sidebars should show a minimal title or header.
- `v0.01` — Confirms that the minimal sidebar header should include a close button.
- `v0.01` — Confirms that each activity-bar icon should open visibly different placeholder content rather than reusing the same mock structure.

## 1. Overview

### 1.1 Purpose

This specification defines a temporary visual shell update for `WorkbenchHost` so the home page can be used to refine the Workbench look and feel before real Workbench features are built.

The purpose of this work is to replace temporary placeholder content with a Radzen-based mock-up that demonstrates sizing, edge anchoring, panel expansion, and general shell mechanics while keeping all displayed content deliberately arbitrary.

### 1.2 Scope

This specification currently includes:

- introducing Radzen into `WorkbenchHost` by following the official Radzen get-started guidance
- removing the temporary `Hello UKHO Workbench` content from `Home.razor`
- removing the temporary listing content from `Home.razor`, including temporary role-related listings and similar placeholder output
- creating a temporary visual mock-up on `Home.razor`
- providing a left-edge icon strip with two or three icons and per-icon expandable sidebar content
- providing a mirrored right-edge icon strip with expandable sidebar panels that open leftwards from the right edge
- leaving the central content area intentionally minimal with `hello`
- focusing on layout, sizing, expansion, contraction, and look and feel rather than real workflows, data, or business behavior
- omitting new automated tests for this mock-up work

This specification currently excludes:

- real Workbench features, commands, workflows, or module composition
- meaningful content design inside the sidebars
- production-ready information architecture for left or right panels
- persistence of layout or panel state
- new automated tests, UI tests, or test scaffolding for this mock-up

### 1.3 Stakeholders

- Workbench developers shaping the shell experience
- contributors responsible for Blazor and UI composition in `WorkbenchHost`
- stakeholders reviewing the visual direction of the future Workbench shell

### 1.4 Definitions

- `Radzen`: the UI component library to be introduced into `WorkbenchHost` for this mock-up
- `icon strip`: a narrow edge-anchored vertical button area containing a small number of icons
- `sidebar`: an expandable panel associated with an icon strip button
- `visual mock-up`: a layout-first implementation intended to validate appearance and interaction feel rather than business behavior

## 2. System context

### 2.1 Current state

`WorkbenchHost` currently exposes a temporary home page used as an initial placeholder.

That placeholder still contains temporary content, including `Hello UKHO Workbench` and listing-oriented output that was only intended for short-term scaffolding.

A temporary visual shell is now required so layout and interaction feel can be explored before real Workbench functionality is implemented.

### 2.2 Proposed state

In the proposed state after this work:

- `WorkbenchHost` uses Radzen on the home page
- `Home.razor` no longer contains the temporary placeholder greeting or the temporary listing output
- the page presents a left edge-anchored icon strip with two or three icons
- clicking left-side icons expands a sidebar region with arbitrary panel-specific content, with one open panel at a time on that side and repeat-click closing the active panel
- the left sidebar is user-resizable through a Radzen splitter and grows rightwards up to a maximum width of `300`
- the page presents a right edge-anchored icon strip with the same concept mirrored
- clicking right-side icons expands sidebar content leftwards while keeping the icon strip pinned to the outer right edge, with one open panel at a time on that side and repeat-click closing the active panel
- the right sidebar is user-resizable through a Radzen splitter and grows leftwards up to a maximum width of `300`
- opening either sidebar pushes and resizes the central area rather than overlaying it
- both sidebars are closed on initial page load
- the left and right icon strips use an activity-bar style visual treatment with icons only, clear hover and active feedback, and tooltip names rather than visible labels
- the left and right icon strips run full height from top to bottom like activity bars
- opened sidebar panels also run full height
- sidebar content areas are visually separated from the main area using subtle separators rather than strong dividers
- the left activity bar includes a bottom-anchored gear icon separate from the top-aligned main icon group
- the left bottom gear icon is counted separately from the main left icon group
- the left bottom gear icon is decorative for now and does not open panels, and the right activity bar has no bottom utility icon
- sidebars use subtle open and close animation only
- opened sidebars include a minimal header area
- the sidebar header includes a close button
- each activity-bar icon opens visibly different placeholder content
- the mock-up should work in both light and dark themes
- the central area remains visually neutral and simple, includes a theme toggle button with the `hello` text, and does not use panels or cards
- the implementation remains explicitly mock-up focused and does not introduce real Workbench logic
- no new automated tests are added for this work

### 2.3 Assumptions

- `Home.razor` is the intended page for the temporary shell mock-up
- the official Radzen get-started guidance is the baseline for introducing Radzen packages, services, assets, and host wiring as needed by `WorkbenchHost`
- arbitrary placeholder content is acceptable inside left and right expandable sidebars
- the primary success criteria are visual structure, panel mechanics, and overall feel rather than content semantics
- a simple `hello` text is sufficient for the main content region in this iteration
- each side manages its own active panel independently
- each side allows only one expanded panel at a time
- clicking the currently active icon closes that side's panel
- expanding sidebars should reduce the available width of the main content region
- both sidebars use Radzen splitter-based resizing rather than fixed widths
- sidebar resizing direction should match the relevant screen edge
- sidebar width must not exceed `300`
- the initial state presents the mock-up with both sidebars closed
- the icon strips should resemble tool or activity bars rather than text-based navigation
- the icon strips should occupy the full page height rather than only wrapping their icon content
- opened sidebar panels should occupy the full page height as part of the shell layout
- the central area should avoid framed panel or card styling unless explicitly requested later
- the visual treatment should hold up in both light and dark themes
- the central toggle is intended to demonstrate light and dark theme switching
- theme persistence is intentionally out of scope for this temporary mock-up
- exact initial sidebar width tuning is intentionally deferred in favor of Radzen defaults for now
- the shell mock-up should stay focused on the side bars and center area without introducing a top chrome region
- the asymmetric icon counts should make the left side feel slightly more prominent than the right
- the activity bar control placement should follow typical workbench expectations by starting at the top
- a supplied screenshot provides only the general shell direction, excluding any bottom panel treatment and excluding any specific panel content expectations
- a left-side bottom gear icon is part of the desired activity-bar feel, separate from the primary navigation icons
- the primary left icon count and the left gear icon should be treated as distinct layout groups
- the left gear icon should contribute to shell feel only and should not introduce extra behavior in this iteration
- motion should remain restrained and limited to opening and closing rather than adding broader animated behavior
- sidebar headers should stay minimal and should not become top command bars
- a close affordance in the sidebar header supports shell mechanics review without introducing real feature behavior
- placeholder panel content should differ clearly enough per icon to help assess shell mechanics and visual variety
- the left gear icon tooltip can use the conventional `Settings` label
- the main area controls should sit at the top left rather than being centered
- the main area should feel like a flush workbench canvas rather than an inset content zone
- the theme toggle visual can remain implementation-led for now as long as it fits the shell feel
- independent left and right sidebar behavior includes allowing both sides to stay open together
- preserving simultaneous left and right sidebar visibility is more important than protecting a minimum center width in this temporary mock-up
- the page should behave as a true edge-to-edge shell rather than a contained application page
- this mock-up is for a desktop shell and should not behave like a responsive web page
- dark-first presentation best matches the intended workbench feel for the initial mock-up
- icon-only treatment should be confined to Theia-like sidebar elements rather than applied across the whole page
- the specific non-close header icon is not important as long as the header remains icon-only
- the close affordance should still remain discoverable through a conventional `Close` tooltip
- the main area may keep visible text because the icon-only rule is not global
- sidebar content can use visible text because the icon-only rule applies to shell chrome rather than placeholder body content
- placeholder content patterns may be mixed or chosen freely because exact content style is not important in this iteration
- manual visual verification is sufficient for this mock-up phase
- no new tests are required for this work item

### 2.4 Constraints

- the output must remain a single specification document in `docs/080-workbench-initial`
- the implementation must start using Radzen in `WorkbenchHost`
- the temporary `Hello UKHO Workbench` content must be removed from `Home.razor`
- temporary listing code on `Home.razor` must be removed
- the left and right icon strips must remain anchored to the absolute window edges
- each side must support expandable and contractible sidebar behavior from icon clicks
- each side must allow only one expanded panel at a time
- clicking the active icon on a side must close that side's panel
- opening a sidebar must push and resize the middle area rather than overlaying it
- both sidebars must be user-resizable with Radzen splitters
- the left sidebar must resize rightwards and the right sidebar must resize leftwards
- each sidebar must have a maximum width of `300`
- both sidebars must be closed by default on initial page load
- the icon strips must not display visible text labels
- the icon strips must provide clear hover and active states
- icon names may be shown only by tooltip
- both icon strips must run the full height of the page
- opened sidebars must run the full height of the page
- the main content area must remain visually neutral and must not introduce panel or card framing unless explicitly requested
- the page should support both light and dark themes
- the main content area must include a toggle button with the `hello` text
- the central toggle button must switch between light and dark themes
- the selected theme must reset on refresh and does not need browser persistence
- initial sidebar width should use the Radzen default behavior if available
- the page must not introduce a top header or command bar
- the left and right icon strips together must use two icons on one side and three icons on the other
- the left icon strip must use three icons and the right icon strip must use two icons
- icon groups in both activity bars must be top-aligned
- sidebar content areas must use subtle separators against the main area and must not use heavy borders
- only the left activity bar must include a bottom-anchored gear icon
- the left bottom gear icon must be separate from the main left icon total
- the left bottom gear icon must remain visual only and must not open a sidebar in this iteration
- the right activity bar must not include any bottom utility icon
- the left bottom gear icon must expose a `Settings` tooltip
- sidebars must use subtle open and close animation only
- opened sidebars must include a minimal title or header area
- opened sidebars must include a close button in the header area
- each main activity-bar icon must open visibly different placeholder content
- the main `hello` text and theme toggle must be positioned at the top left of the main area
- the main area must use no padding
- the exact visual treatment of the theme toggle may be chosen based on what looks best
- the left and right sidebars must be allowed to remain open at the same time
- when both sidebars are open, the main area may become very narrow
- the home page must fill the full browser viewport horizontally and vertically with no outer margins or padding
- the shell must keep its desktop layout under narrow widths and must not auto-hide UI regions
- the initial theme on first load must be dark
- sidebar headers must not display visible text and may only use tooltip text where needed
- each opened sidebar header must show a non-text icon followed by an icon-only close button
- the sidebar header close button must expose a `Close` tooltip
- the icon-only rule applies only to sidebar elements and does not forbid visible text in the main area
- visible text is permitted within sidebar content areas
- placeholder sidebar content style may use implementation discretion
- the right sidebar behavior must be visually reversed so expansion occurs leftwards from the right edge
- the main content area must contain `hello`
- no automated tests are required for this work

## 3. Component / service design (high level)

### 3.1 Components

1. `WorkbenchHost`
   - hosts the Blazor UI and Radzen integration for the temporary mock-up

2. `Home.razor`
   - becomes the visual shell mock-up page
   - contains the left sidebar region, main content region, and right sidebar region

3. `Left shell controls`
   - provide a small icon strip on the left edge
   - reveal different arbitrary content depending on the selected icon

4. `Right shell controls`
   - provide a small icon strip on the right edge
   - reveal different arbitrary content depending on the selected icon
   - expand leftwards while remaining edge anchored

5. `Main content region`
   - remains intentionally minimal and displays `hello`

### 3.2 Data flows

#### Runtime interaction flow

1. the user opens the `WorkbenchHost` home page
2. the page renders the left and right edge icon strips with the central content region
3. the user selects an icon on either side
4. the associated sidebar expands or contracts to present arbitrary placeholder content
5. the central area continues to display `hello`

### 3.3 Key decisions

- this work is visual-first and does not depend on real Workbench functionality
- Radzen is the chosen component baseline for the mock-up
- left and right side interactions are intentionally symmetrical in concept
- content inside the sidebars is intentionally arbitrary and disposable
- automated tests are intentionally out of scope for this mock-up

## 4. Functional requirements

1. `WorkbenchHost` shall start using Radzen according to the official Radzen get-started guidance.
2. `Home.razor` shall remove the temporary `Hello UKHO Workbench` content.
3. `Home.razor` shall remove temporary listing output, including temporary role-related listings and similar scaffold-only content.
4. `Home.razor` shall provide a left vertical icon strip anchored to the absolute left edge of the window.
5. The left icon strip shall expose two or three icon actions.
6. Selecting a left-side icon shall reveal arbitrary sidebar content associated with that icon.
7. The left-side sidebar area shall support expansion and contraction.
8. The left-side sidebar area shall allow only one open panel at a time.
9. Clicking the active left-side icon again shall close the open left-side panel.
10. The left-side sidebar shall be user-resizable by using a Radzen splitter.
11. The left-side sidebar shall resize rightwards.
12. The left-side sidebar shall have a maximum width of `300`.
13. `Home.razor` shall provide a right vertical icon strip anchored to the absolute right edge of the window.
14. The right icon strip shall expose two or three icon actions.
15. Selecting a right-side icon shall reveal arbitrary sidebar content associated with that icon.
16. The right-side sidebar area shall support expansion and contraction.
17. The right-side sidebar area shall allow only one open panel at a time.
18. Clicking the active right-side icon again shall close the open right-side panel.
19. The right-side sidebar shall be user-resizable by using a Radzen splitter.
20. The right-side sidebar shall resize leftwards.
21. The right-side sidebar shall have a maximum width of `300`.
22. The right-side sidebar shall expand leftwards while keeping the right icon strip pinned to the outer edge of the window.
23. Opening either sidebar shall push and resize the main content area rather than overlaying it.
24. Both sidebars shall be closed by default on initial page load.
25. The left and right icon strips shall use a Workbench or tool-style visual treatment with clear hover and active states.
26. The icon strips shall display icons only and shall not display visible text labels.
27. Icon names may be shown as tooltips.
28. The icon strips shall run the full height of the page.
29. Opened sidebar panels shall run the full height of the page.
30. The icon strip treatment should resemble Theia-style activity bars for look and feel direction.
31. The page shall support both light and dark themes.
32. The main content area shall remain visually neutral and shall not introduce panels or cards unless explicitly requested.
33. The main content area shall include a toggle button with the `hello` text.
34. Activating the toggle button shall switch the page theme from light to dark and from dark to light.
35. The selected theme shall reset on page reload and shall not require persistence.
36. When a sidebar first opens, its initial width shall use the Radzen default behavior if one is available.
37. The page shall not include a top header or command bar.
38. The left and right icon strips shall use asymmetric icon counts, with two icons on one side and three on the other.
39. The left icon strip shall use three icons and the right icon strip shall use two icons.
40. Icon groups in both activity bars shall be top-aligned.
41. Sidebar content areas shall use subtle separators against the main area.
42. The supplied screenshot shall be treated only as general visual direction, ignoring bottom panel treatment and sidebar content details.
43. Only the left activity bar shall include a bottom-anchored gear icon.
44. The left bottom gear icon shall be separate from the main left icon total, resulting in `3 + gear` on the left and `2` icons on the right.
45. The left bottom gear icon shall remain visual only and shall not open a sidebar in this iteration.
46. The right activity bar shall not include any bottom utility icon.
47. The left bottom gear icon shall expose a `Settings` tooltip.
48. Sidebar opening and closing shall use subtle animation only.
49. Opened sidebars shall include a minimal title or header area.
50. Opened sidebars shall include a close button in the header area.
51. Each main activity-bar icon shall open visibly different placeholder content.
52. The main `hello` text and theme toggle shall be positioned at the top left of the main area.
53. The page shall behave as a visual mock-up focused on look and feel rather than real content or workflow behavior.
54. No new automated tests shall be required for this work item.

## 5. Non-functional requirements

1. The mock-up should prioritize visual polish, clear structure, and predictable panel mechanics.
2. The page should be easy to revise repeatedly as Workbench look and feel is refined.
3. The implementation should remain intentionally lightweight and disposable where needed because the content is temporary.
4. The layout should adapt sensibly to available browser space so sizing and shell mechanics can be reviewed.
5. The UI should align with repository Blazor guidance, including explicit interactive behavior where input handling is required.

## 6. Data model

No persistent or business data model is required for this mock-up.

Any sidebar content may use temporary in-memory view state only.

## 7. Interfaces & integration

1. `Radzen` integration
   - `WorkbenchHost` must include the packages, services, assets, and UI usage needed to follow the official Radzen get-started path
   - the mock-up should use Radzen where it helps establish the desired shell layout and interaction feel

2. `Home.razor`
   - must replace the current temporary placeholder content
   - must host the full mock-up shell layout

## 8. Observability (logging/metrics/tracing)

No additional observability requirements are currently defined for this mock-up.

## 9. Security & compliance

1. The mock-up should not introduce privileged behavior or business-specific data.
2. Placeholder sidebar content should remain generic and non-sensitive.

## 10. Testing strategy

1. Manual visual verification shall be sufficient for this work.
2. Verification should confirm Radzen is functioning in `WorkbenchHost`.
3. Verification should confirm the left and right icon strips remain edge anchored.
4. Verification should confirm the sidebars expand and contract in the intended directions.
5. Verification should confirm the sidebars can be resized manually through Radzen splitters.
6. Verification should confirm sidebar resizing respects the maximum width of `300`.
7. Verification should confirm the page initially loads with both sidebars closed.
8. Verification should confirm the central region resizes as sidebars open, close, and are resized.
9. Verification should confirm the icon strips present icon-only controls with no visible text labels.
10. Verification should confirm the icon strips provide hover, active, and tooltip feedback.
11. Verification should confirm the icon strips run full height from top to bottom.
12. Verification should confirm opened sidebar panels also run full height.
13. Verification should confirm the mock-up remains usable in both light and dark themes.
14. Verification should confirm the central region remains visually neutral without panel or card framing.
15. Verification should confirm the central region includes a toggle button with the `hello` text.
16. Verification should confirm the toggle switches the page theme in both directions.
17. Verification should confirm the theme resets after page reload.
18. Verification should confirm initial sidebar opening uses the Radzen default width behavior if available.
19. Verification should confirm no top header or command bar is present.
20. Verification should confirm icon groups are top-aligned within both full-height activity bars.
21. No new automated tests are required.

## 11. Rollout / migration

1. Replace the temporary scaffold content on `Home.razor` with the Radzen-based mock-up.
2. Use the mock-up as a disposable visual foundation while the future Workbench shell is still being shaped.
3. Defer real content, workflows, and test investment until the shell mechanics and look and feel are agreed.

## 12. Open questions

No open questions are currently recorded.
