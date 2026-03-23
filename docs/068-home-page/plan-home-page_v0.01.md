# Implementation Plan

**Target output path:** `docs/068-home-page/plan-home-page_v0.01.md`

**Based on:** `docs/068-home-page/spec-home-page_v0.01.md`

**Version:** `v0.01` (`Draft`)

---

## Slice 1 — Restore a branded `Home` tab as the default Studio entry document

- [x] Work Item 1: Restore a runnable `Home` tab document with copied logo asset, static intro content, default startup behavior, and `View` menu reopening - Completed
  - **Purpose**: Reintroduce the Studio landing experience as a normal closable Theia document so users enter Studio on a useful branded page rather than landing directly in a task surface.
  - **Acceptance Criteria**:
    - Studio opens with `Home` as the default initial document in the editor area.
    - The `Home` document is a normal closable tab.
    - The page renders static introductory content on the expected dark background.
    - The UKHO logo is copied from `docs/ukho-logo-transparent.png` into the appropriate application asset location for runtime use.
    - The rendered logo uses a reduced default size and does not dominate the page.
    - A Theia `View` menu action reopens `Home` after it has been closed.
    - Reopening `Home` follows the simplest normal Theia behavior implemented by the shell and remains consistent.
  - **Definition of Done**:
    - `Home` document contribution implemented in the Studio Theia frontend
    - startup layout updated so `Home` opens by default
    - `View` menu command/action implemented for reopening `Home`
    - logo asset copied into the Studio application's runtime-served asset location
    - static page layout and styling implemented for dark-theme readability
    - automated tests added for command/menu and startup/opening behavior where practical
    - documentation updated if implementation details require alignment with the spec
    - Can execute end to end via: launching Studio and verifying `Home` opens as a closable tab, then reopening it from `View`
  - [x] Task 1.1: Introduce a dedicated `Home` document contribution in the Studio frontend - Completed
    - [x] Step 1: Identify the current Studio document-opening pattern used for provider, rules, and ingestion overview documents. - Completed
    - [x] Step 2: Add a dedicated `Home` document descriptor/type so the page can open in the editor area as a first-class Studio document. - Completed
    - [x] Step 3: Implement the `Home` widget/component using the same normal Theia document/tab mechanics as other Studio documents. - Completed
    - [x] Step 4: Ensure the `Home` tab remains closable and does not require any pinned or special-case tab behavior. - Completed
    - **Completed Summary**:
      - Added a dedicated `SearchStudioHomeWidget` and `SearchStudioHomeService` so `Home` opens in the main workbench area as a normal closable tab document.
      - Kept the implementation aligned with the existing widget-manager and shell document-opening pattern already used by Studio placeholder documents.
  - [x] Task 1.2: Add default startup and `View` menu reopening behavior - Completed
    - [x] Step 1: Update shell startup/layout behavior so Studio opens `Home` by default on initial entry. - Completed
    - [x] Step 2: Add a command for showing `Home` again after the user navigates elsewhere. - Completed
    - [x] Step 3: Contribute that command to the Theia `View` menu using the repository's existing Studio menu contribution pattern. - Completed
    - [x] Step 4: Implement consistent reopen behavior, allowing either focus-existing or open-new if that matches the simpler normal Theia approach. - Completed
    - **Completed Summary**:
      - Updated the shell layout contribution so the Studio side-bar views are revealed and the `Home` tab opens as the default initial document.
      - Added a `Show Home` command and wired it into the Theia `View` menu for reopening after close.
  - [x] Task 1.3: Copy and serve the logo asset correctly - Completed
    - [x] Step 1: Create or use the appropriate application asset/static location within the `search-studio` package for runtime-served assets. - Completed
    - [x] Step 2: Copy `docs/ukho-logo-transparent.png` into that runtime asset location. - Completed
    - [x] Step 3: Wire the `Home` page to use the copied runtime asset rather than referencing `docs/` directly. - Completed
    - [x] Step 4: Add styling constraints so the logo renders at a materially reduced default size while remaining crisp and undistorted. - Completed
    - **Completed Summary**:
      - Copied `docs/ukho-logo-transparent.png` into `src/Studio/Server/search-studio/src/browser/assets/` and added a package build step that copies runtime assets into `lib/browser/assets`.
      - Wired the `Home` widget to use the runtime-served asset and constrained the logo to a reduced default size.
  - [x] Task 1.4: Implement the static `Home` layout and introductory content - Completed
    - [x] Step 1: Add a dark-compatible page layout with a branded header section. - Completed
    - [x] Step 2: Add concise placeholder intro text describing `Search Studio` and how to begin. - Completed
    - [x] Step 3: Keep the first version intentionally static, with no status cards, counts, or recent-activity summary. - Completed
    - [x] Step 4: Verify keyboard focus order and readable contrast for the page structure. - Completed
    - **Completed Summary**:
      - Implemented a lightweight dark-compatible `Home` layout with branded header treatment, concise introduction text, and static orientation cards for the current Studio work areas.
      - Kept the first version intentionally non-dashboard and free of task jump points until Work Item 2.
  - [x] Task 1.5: Add coverage for startup and reopen behavior - Completed
    - [x] Step 1: Add frontend tests for the new `Home` show/open command and `View` menu wiring. - Completed
    - [x] Step 2: Add focused tests for logo-path resolution or home document descriptor mapping if those are implemented through pure helpers. - Completed
    - [x] Step 3: Document a manual smoke path covering startup, close, and reopen behavior until broader Studio e2e coverage exists. - Completed
    - **Completed Summary**:
      - Added Node-based tests covering the `Show Home` command, `View` menu wiring, shell startup layout behavior, and `Home` service document opening semantics.
      - Verified the runnable smoke path with `yarn test`, `yarn build:browser`, and a successful workspace build.
  - **Files**:
    - `src/Studio/Server/search-studio/src/browser/search-studio-frontend-module.ts`: bind the new `Home` document/widget services and menu command wiring.
    - `src/Studio/Server/search-studio/src/browser/search-studio-command-contribution.ts`: register the `Show Home` command.
    - `src/Studio/Server/search-studio/src/browser/search-studio-menu-contribution.ts`: add the Theia `View` menu action for reopening `Home`.
    - `src/Studio/Server/search-studio/src/browser/search-studio-shell-layout-contribution.ts`: open `Home` as the default initial document.
    - `src/Studio/Server/search-studio/src/browser/search-studio-view-contribution.ts`: integrate `Home` with the existing Studio document-opening workflow if required.
    - `src/Studio/Server/search-studio/src/browser/common/*`: shared document descriptors or open-home helpers if a common abstraction is needed.
    - `src/Studio/Server/search-studio/src/browser/home/*`: new `Home` widget/component, types, and styling helpers.
    - `src/Studio/Server/search-studio/src/browser/assets/*` or equivalent static asset location: copied UKHO logo runtime asset.
    - `src/Studio/Server/search-studio/test/search-studio-command-contribution.test.js`: extend for `Show Home` command coverage.
    - `src/Studio/Server/search-studio/test/*`: add home-open/menu/startup coverage in the existing frontend test location.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Open Studio on the configured port from `src/Hosts/AppHost/appsettings.json`.
    - Verify `Home` opens by default, close the tab, then reopen it from the Theia `View` menu.
  - **User Instructions**: Ensure the local Theia/Node prerequisites already used by the Studio shell are installed.
  - **Completed Summary**:
    - Restored `Home` as a normal closable Studio tab document with a reduced-size UKHO logo, static introduction content, and dark-theme-friendly presentation.
    - Wired `Home` into startup so it opens by default, and added Theia `View` menu reopening through the new `Show Home` command.
    - Added runtime asset copying, frontend tests, and wiki updates documenting the restored `Home` entry point.
    - Follow-up fix: triggered an initial render for the `Home` widget so its content is visible when first opened, and inserted new `Home` tabs before existing main-area tabs so `Home` appears first in tab order.

---

## Slice 2 — Add task-focused `Home` jump points for useful end-to-end navigation

- [x] Work Item 2: Add task-focused jump points from `Home` into existing Studio destinations using normal Theia destination behavior - Completed
  - **Purpose**: Turn the restored `Home` page into a practical operational landing surface by giving users obvious one-click entry to common workflows.
  - **Acceptance Criteria**:
    - The `Home` page exposes the baseline jump points `Start ingestion`, `Manage rules`, and `Browse providers`.
    - Each jump point routes to an existing Studio destination rather than a placeholder-only route.
    - Jump points use the normal Theia interaction model for their destination instead of forcing a special custom open behavior.
    - Jump points are visually obvious, keyboard accessible, and readable on the dark theme.
    - The page remains static apart from navigation actions and does not add summary/status content.
  - **Definition of Done**:
    - `Home` jump-point UI implemented with clear labels and short supporting text if needed
    - navigation handlers implemented for ingestion, rules, and providers entry points
    - destination routing reuses existing Studio navigation/document-opening services
    - automated tests added for jump-point wiring and target resolution where practical
    - manual smoke verification documented for the three common-task flows
    - Can execute end to end via: opening `Home`, selecting a jump point, and arriving at the intended existing Studio destination
  - [x] Task 2.1: Add the jump-point UI to the `Home` page - Completed
    - [x] Step 1: Choose a low-noise card, button, or action-list treatment that fits the current Studio shell styling. - Completed
    - [x] Step 2: Add the three baseline jump points using the confirmed labels `Start ingestion`, `Manage rules`, and `Browse providers`. - Completed
    - [x] Step 3: Add short explanatory text where needed so each action is self-explanatory without hidden knowledge. - Completed
    - [x] Step 4: Ensure keyboard traversal and visible focus treatment work cleanly for all actions. - Completed
    - **Completed Summary**:
      - Replaced the passive Home orientation cards with low-noise button cards using the confirmed task-focused labels.
      - Kept the actions keyboard-friendly by using native buttons and retaining the lightweight static landing-page layout.
  - [x] Task 2.2: Wire each jump point to an existing Studio destination - Completed
    - [x] Step 1: Map `Start ingestion` to the closest existing ingestion landing path already supported by the Studio shell. - Completed
    - [x] Step 2: Map `Manage rules` to the closest existing rules landing path already supported by the Studio shell. - Completed
    - [x] Step 3: Map `Browse providers` to the closest existing providers landing path already supported by the Studio shell. - Completed
    - [x] Step 4: Reuse the normal destination-opening path already used elsewhere in Studio so `Home` does not introduce one-off navigation logic. - Completed
    - **Completed Summary**:
      - Added a dedicated Home navigation service that resolves the current or first available provider and routes each jump point through the existing Studio document-opening services.
      - Mapped the actions to ingestion overview, rules overview, and provider overview documents without introducing custom destination behavior.
  - [x] Task 2.3: Add focused test coverage for jump-point behavior - Completed
    - [x] Step 1: Add frontend tests for jump-point rendering and click/keyboard activation. - Completed
    - [x] Step 2: Add tests for target resolution so each action opens the expected destination intent. - Completed
    - [x] Step 3: Extend the manual Studio smoke path to cover all three jump points from `Home`. - Completed
    - **Completed Summary**:
      - Added Node-based tests for the agreed Home jump-point set and the navigation service that opens the correct destination or warns when no providers are available.
      - Revalidated the manual smoke path expectations with `yarn test`, `yarn build:browser`, and a successful workspace build.
  - [x] Task 2.4: Update supporting documentation - Completed
    - [x] Step 1: Update the work package plan if implementation details materially differ from the draft plan. - Completed
    - [x] Step 2: Update the repository wiki Studio guidance with the restored `Home` tab, `View` menu reopen path, and common-task jump points. - Completed
    - [x] Step 3: Note the runtime asset location used for the copied logo so future contributors do not mistakenly point back to `docs/`. - Completed
    - **Completed Summary**:
      - Updated the work package plan with completed-task summaries and refreshed the Studio wiki guidance to mention the Home jump points and runtime asset location.
  - **Files**:
    - `src/Studio/Server/search-studio/src/browser/home/*`: `Home` page layout, jump-point presentation, and navigation handlers.
    - `src/Studio/Server/search-studio/src/browser/common/*`: shared document-opening helpers or target-resolution helpers if needed.
    - `src/Studio/Server/search-studio/src/browser/providers/*`: only if a provider landing helper or provider overview entrypoint needs a small extension.
    - `src/Studio/Server/search-studio/src/browser/rules/*`: only if a rules landing helper needs a small extension.
    - `src/Studio/Server/search-studio/src/browser/ingestion/*`: only if an ingestion landing helper needs a small extension.
    - `src/Studio/Server/search-studio/test/*`: jump-point rendering and navigation tests.
    - `wiki/Home.md`: Studio usage/build notes reflecting the restored `Home` landing page and navigation entry points.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Open Studio, confirm `Home` displays the three jump points, and verify each action navigates to the intended existing Studio destination using normal shell behavior.
  - **User Instructions**: No manual setup beyond normal Studio prerequisites.
  - **Completed Summary**:
    - Added task-focused Home jump points for `Start ingestion`, `Manage rules`, and `Browse providers` using the existing Studio destination-opening flows.
    - Added a Home navigation service, Home jump-point metadata, focused frontend tests, and updated Studio wiki guidance.

---

## Overall approach summary

This plan keeps the work package small and vertical:

1. restore a usable branded `Home` document first, including the copied logo asset, default startup behavior, closable tab behavior, and `View` menu reopening
2. add task-focused jump points only after the landing document itself is working end to end

Key considerations for implementation:

- keep `Home` a normal Theia document rather than a bespoke shell surface
- copy the logo into the application's runtime asset location instead of serving it from `docs/`
- scale the logo down intentionally because the source asset is too large unscaled
- reuse existing Studio document-opening/navigation paths for jump points
- keep the first version static and lightweight, with no status-summary content
