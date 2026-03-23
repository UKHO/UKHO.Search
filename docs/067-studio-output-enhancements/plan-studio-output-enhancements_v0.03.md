# Implementation Plan

**Target output path:** `docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.03.md`

**Based on:** `docs/067-studio-output-enhancements/spec-studio-output-enhancements_v0.02.md`

**Version:** `v0.03` (`Draft`)

**Supersedes:** `docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.02.md`

## Change Log

- `v0.03` — Replanned the work package around a read-only `xterm.js`-based output surface, replacing the earlier custom-widget refinement approach while preserving the `Copy all` baseline requirement.
- `v0.02` — Brought `Copy all` into baseline scope, added toolbar-action planning and verification coverage, and aligned the final consistency pass with the updated requirement.
- `v0.01` — Initial draft.

## Baseline

- `Studio Output` already exists as a Studio-owned lower-panel widget in `src/Studio/Server/search-studio`.
- The current output flow already has a working `SearchStudioOutputService`, `SearchStudioOutputWidget`, and toolbar-based `Clear output` command path.
- Output entries already carry `timestamp`, `level`, `source`, and `message` metadata.
- The current compact custom-widget rendering has not met the desired density target and is no longer the preferred baseline.

## Delta

- Introduce a read-only `xterm.js` rendering surface inside the existing `Studio Output` panel.
- Preserve explicit visible `time`, `severity`, and `source` text on each line in the first implementation.
- Keep the output stream merged and chronological, with reveal-latest behavior enabled by default.
- Add a toolbar `Copy all` action for the full merged output stream.
- Preserve non-terminal semantics despite using a terminal-grade rendering control.

## Carry-over

- Explicit channel switching remains out of scope.
- Shell execution, prompts, and interactive terminal input remain out of scope.
- Density refinements beyond the first agreed visible line format may be revisited later.
- Advanced export behaviors beyond `Copy all` remain deferred.

---

## Slice 1 — Read-only `xterm.js` host with merged-stream baseline rendering

- [x] Work Item 1: Replace the current custom compact-row renderer with a Studio-owned read-only `xterm.js` output surface that shows real Studio events end to end - Completed
  - **Summary**: Added `@xterm/xterm`, `@xterm/addon-fit`, and `@xterm/addon-webgl`, moved `Studio Output` onto a read-only terminal host with merged-stream serialization, resolved the xterm font configuration to a clean monospace stack, and enabled the WebGL renderer when available for sharper text. Added formatter/widget coverage plus a manual smoke path note. Verified with `node .\src\Studio\Server\node_modules\typescript\bin\tsc -p .\src\Studio\Server\search-studio\tsconfig.json`, `node --test .\src\Studio\Server\search-studio\test`, and `yarn --cwd .\src\Studio\Server build:browser`. The Studio browser bundle now builds with explicit `bindings` resolution, VS 2022 node-gyp environment selection, and backend stubs for unused native `drivelist`/`node-pty` paths.
  - **Purpose**: Deliver the smallest meaningful runnable slice by proving that `Studio Output` can use `xterm.js` for dense rendering while remaining Studio-owned, read-only, and wired to the existing output service.
  - **Acceptance Criteria**:
    - `Studio Output` remains a Studio-owned lower-panel widget.
    - The widget hosts a read-only `xterm.js` surface rather than custom row markup.
    - Output is rendered as a single merged stream in chronological reading order.
    - Each line still visibly includes `time`, `severity`, `source`, and `message`.
    - The surface does not expose prompt, input, or shell execution semantics.
  - **Definition of Done**:
    - `xterm.js` dependency added and wired into the frontend build.
    - The widget initializes and disposes the terminal surface correctly.
    - Existing output-service events populate the terminal with real output lines.
    - Focused tests cover line formatting and terminal-host lifecycle assumptions where practical.
    - No backend/API changes introduced.
    - Can execute end to end via: Studio shell `Studio Output` panel showing real Studio events through read-only `xterm.js`.
  - [x] Task 1.1: Introduce the read-only terminal host - Completed
    - **Summary**: Wired `xterm` dependencies and stylesheet loading, refactored `SearchStudioOutputWidget` to own a read-only terminal host with lifecycle-managed initialization, fitting, disposal, a stable monospace stack, and optional WebGL rendering for improved text quality.
    - [x] Step 1: Add `xterm.js` and any required companion package(s) to `src/Studio/Server/search-studio/package.json`.
    - [x] Step 2: Refactor `SearchStudioOutputWidget` so it hosts a terminal container rather than custom-rendered row markup.
    - [x] Step 3: Initialize the terminal in read-only mode and ensure disposal occurs when the widget is torn down.
    - [x] Step 4: Keep the panel clearly Studio-owned and non-terminal in interaction semantics despite the rendering primitive.
  - [x] Task 1.2: Preserve the merged-stream output format - Completed
    - **Summary**: Added stable terminal line serialization while retaining explicit visible `time`, `severity`, `source`, and `message` text in chronological merged-stream order.
    - [x] Step 1: Extend `search-studio-output-format.ts` with stable line serialization suitable for terminal output.
    - [x] Step 2: Keep explicit visible `time`, `severity`, `source`, and `message` text on each line.
    - [x] Step 3: Preserve chronological oldest-to-newest line order in the visible stream.
    - [x] Step 4: Ensure source metadata remains present in the rendered text for future filtering readiness.
  - [x] Task 1.3: Add baseline verification for the terminal host - Completed
    - **Summary**: Added merged-stream serialization and terminal-host lifecycle tests, retained service ordering coverage, and documented the manual smoke path plus the current Studio browser bundle blocker.
    - [x] Step 1: Add or update tests for stable merged-stream line serialization.
    - [x] Step 2: Add focused verification for chronological append order in the output service.
    - [x] Step 3: Add focused verification for terminal-host initialization/disposal behavior where practical without over-coupling to implementation details.
    - [x] Step 4: Document a manual smoke path for confirming real Studio output appears in the read-only terminal surface.
  - **Files**:
    - `src/Studio/Server/search-studio/package.json`: add `xterm.js` dependencies.
    - `src/Studio/Server/browser-app/package.json`: add explicit backend runtime dependencies required for the Studio browser bundle.
    - `src/Studio/Server/browser-app/webpack.config.js`: customize backend bundling to avoid unused native terminal packaging paths.
    - `src/Studio/Server/browser-app/stubs/*`: stub unused native backend modules required only by optional Theia features.
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-widget.tsx`: host and manage the read-only terminal surface.
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-format.ts`: stable terminal line formatting helpers.
    - `src/Studio/Server/search-studio/src/browser/common/search-studio-output-service.ts`: preserve merged-stream ordering and append semantics.
    - `src/Hosts/AppHost/AppHost.cs`: force Studio shell native module installation to use the supported VS 2022 toolchain.
    - `src/Studio/Server/search-studio/test/*`: baseline formatter/service/terminal-host coverage.
  - **Work Item Dependencies**: Existing `066-studio-minor-ux` implementation only.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio install --ignore-engines`
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Open `Studio Output` and verify real Studio events render inside the read-only terminal surface with explicit `time`, `severity`, `source`, and `message` text.
  - **User Instructions**: Trigger a few Studio actions so the new terminal-backed surface can be reviewed with realistic output entries.

---

## Slice 2 — Severity styling and reveal-latest behavior on the terminal surface

- [x] Work Item 2: Add terminal-compatible severity styling and reveal-latest behavior so the newest output stays visible in normal use - Completed
  - **Summary**: Added terminal-compatible ANSI severity styling for `INFO` and `ERROR`, tightened reveal-latest behavior so clear resets do not trigger unnecessary scrolling, removed the temporary xterm diagnostics, and expanded formatter/widget coverage for styled output and clear-followed-by-append behavior. Verified with `yarn --cwd .\src\Studio\Server\search-studio build`, `node --test .\src\Studio\Server\search-studio\test`, `yarn --cwd .\src\Studio\Server build:browser`, and solution build.
  - **Purpose**: Deliver the next runnable slice by combining the denser terminal surface with the expected output-pane behavior for severity scanning and default latest-line visibility.
  - **Acceptance Criteria**:
    - New output keeps the latest visible line in view by default.
    - `INFO` and `ERROR` use the agreed pastel colors in the rendered terminal output.
    - The visible line format remains stable while severity treatment stays readable on dark backgrounds.
    - The implementation remains read-only and does not introduce prompt or cursor-driven command entry.
  - **Definition of Done**:
    - Reveal-latest behavior works end to end in the terminal-backed widget.
    - Terminal-compatible severity formatting is implemented for at least `INFO` and `ERROR`.
    - Clear behavior still resets the panel cleanly.
    - Focused tests cover reveal-latest logic and severity-format helpers.
    - Can execute end to end via: Studio shell `Studio Output` during active navigation/actions.
  - [x] Task 2.1: Implement reveal-latest behavior for the terminal surface - Completed
    - **Summary**: Kept scroll-to-latest on append, retained layout refresh on attach/show, and ensured clear resets terminal state without forcing a latest-line scroll.
    - [x] Step 1: Choose the simplest terminal-host mechanism for keeping the newest line visible.
    - [x] Step 2: Implement default reveal-latest behavior when new output is appended.
    - [x] Step 3: Ensure clear resets the terminal buffer and reveal state cleanly.
    - [x] Step 4: Confirm behavior remains correct with chronological output order.
  - [x] Task 2.2: Add pastel severity treatment for terminal output - Completed
    - **Summary**: Extended the output formatter with truecolor ANSI styling helpers so the severity token remains visibly pastel blue for `INFO` and pastel red for `ERROR` while the visible line text stays stable.
    - [x] Step 1: Extend output-format helpers to emit terminal-compatible severity styling.
    - [x] Step 2: Apply pastel blue `#A9C7FF` to `INFO` and pastel red `#FFB3BA` to `ERROR`.
    - [x] Step 3: Keep the emphasis token-led so styling remains subtle and readable.
    - [x] Step 4: Ensure the implementation remains theme-respecting and easy to adjust later.
  - [x] Task 2.3: Add targeted verification for reveal-latest and severity formatting - Completed
    - **Summary**: Added formatter coverage for ANSI severity serialization, widget coverage for no-scroll-on-clear and clear-followed-by-append, and refreshed the manual smoke note for continuous output review.
    - [x] Step 1: Add tests for reveal-latest helper logic or focused widget-level behavior.
    - [x] Step 2: Add tests for terminal-compatible severity formatting helpers.
    - [x] Step 3: Add verification for clear-followed-by-append behavior.
    - [x] Step 4: Add manual smoke notes for continuous output review in the terminal surface.
  - **Files**:
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-widget.tsx`: reveal-latest behavior for the terminal surface.
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-format.ts`: terminal-compatible severity styling helpers.
    - `src/Studio/Server/search-studio/src/browser/common/search-studio-output-service.ts`: unchanged unless event sequencing needs a small refinement.
    - `src/Studio/Server/search-studio/test/*`: reveal-latest and severity-format coverage.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Produce multiple output lines and confirm the latest line stays visible while `INFO` and `ERROR` use the agreed pastel severity treatment.
  - **User Instructions**: Leave the output panel open while triggering multiple actions across `Providers`, `Rules`, and `Ingestion`.

---

## Slice 3 — Toolbar `Copy all` and selection-friendly terminal interaction

- [x] Work Item 3: Add `Copy all` and confirm the terminal-backed output remains naturally selectable and copy-friendly - Completed
  - **Summary**: Added a `Copy all` toolbar command that serializes the merged output stream in the stable visible format and writes it to the clipboard, kept the terminal surface read-only and selection-friendly without introducing prompt-like behavior, and added toolbar/command verification for copy-all availability and clipboard behavior. Verified with `yarn --cwd .\src\Studio\Server\search-studio build`, `node --test .\src\Studio\Server\search-studio\test`, `yarn --cwd .\src\Studio\Server build:browser`, and solution build.
  - **Purpose**: Deliver a third runnable slice that completes the baseline user interaction model by adding a full-stream copy command while validating that the read-only terminal surface supports day-to-day inspection workflows.
  - **Acceptance Criteria**:
    - Output remains naturally selectable and copy-friendly.
    - `Copy all` is available as a toolbar action and copies the full merged stream to clipboard.
    - The copied text preserves `time`, `severity`, `source`, and `message` ordering.
    - `Clear output` and `Copy all` are both available as toolbar actions.
    - No explicit source/channel switching is introduced.
  - **Definition of Done**:
    - `Copy all` implemented end to end for the visible merged stream.
    - Existing terminal-backed selection behavior reviewed and refined if needed.
    - Source metadata remains available and stable in rendered/captured output.
    - Focused tests cover toolbar registration and copied text generation.
    - Can execute end to end via: normal Studio usage with output inspection and copy/select behavior.
  - [x] Task 3.1: Confirm read-only selection and copy-friendliness - Completed
    - **Summary**: Reviewed the terminal configuration and kept the read-only xterm surface selection-friendly by preserving default selection behavior while continuing to suppress terminal input and prompt-like affordances.
    - [x] Step 1: Review terminal configuration for anything that impedes natural selection or text copy.
    - [x] Step 2: Ensure the visible output remains readable and selectable in the terminal surface.
    - [x] Step 3: Avoid introducing prompt-like affordances or fake command-entry behavior.
  - [x] Task 3.2: Add `Copy all` toolbar action - Completed
    - **Summary**: Added a dedicated `Copy all` command and toolbar action beside `Clear output`, serialized the full merged stream with the stable visible format, and copied it through the browser clipboard API with user feedback.
    - [x] Step 1: Introduce a dedicated toolbar command/contribution for `Copy all` alongside `Clear output`.
    - [x] Step 2: Serialize the full merged output stream using the stable visible line format.
    - [x] Step 3: Copy the merged stream text to clipboard without requiring selection.
    - [x] Step 4: Keep command wiring aligned with the existing frontend module and output-command pattern.
  - [x] Task 3.3: Add targeted interaction verification - Completed
    - **Summary**: Added command coverage for clipboard copy success and unavailable-clipboard warnings, extended toolbar coverage so `Copy all` remains output-only, and preserved plain-text serialization verification for copied output ordering and source metadata.
    - [x] Step 1: Add verification that the copied text preserves stable merged-stream ordering and source metadata.
    - [x] Step 2: Add verification that `Copy all` is only exposed for `Studio Output`.
    - [x] Step 3: Add manual smoke notes for selecting visible output text and using the toolbar copy action.
    - [x] Step 4: Add focused verification that no channel-switch UI is introduced.
  - **Files**:
    - `src/Studio/Server/search-studio/src/browser/search-studio-constants.ts`: add `Copy all` command metadata.
    - `src/Studio/Server/search-studio/src/browser/search-studio-command-contribution.ts`: implement `Copy all` command behavior.
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-toolbar-contribution.ts`: register `Copy all` alongside `Clear output`.
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-format.ts`: stable full-stream text serialization helpers.
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-widget.tsx`: expose any terminal-backed selection/copy refinements if needed.
    - `src/Studio/Server/search-studio/test/*`: toolbar and copy-all coverage.
  - **Work Item Dependencies**: Work Items 1 and 2.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Open `Studio Output`, use `Copy all`, and confirm the copied text matches the visible merged stream with explicit visible sources.
  - **User Instructions**: None beyond the normal Studio startup prerequisites.

---

## Slice 4 — Final terminal-surface consistency pass and contributor documentation

- [x] Work Item 4: Harmonize the terminal-backed `Studio Output` baseline and document the accepted direction - Completed
  - **Summary**: Completed the final consistency pass across the terminal-backed `Studio Output` behaviors, verified the full frontend build and test path, and updated the Studio shell wiki with the accepted output baseline plus rebuild/refresh guidance for Theia browser assets. The earlier stale-bundle rendering confusion was documented as contributor guidance so future frontend changes are easier to validate.
  - **Purpose**: Close the work package with a consistency review, final verification pass, and documentation updates so future Studio shell work starts from a clear, accepted read-only terminal-backed baseline.
  - **Acceptance Criteria**:
    - `Studio Output` clearly reads as a dense IDE-style output pane backed by a read-only terminal renderer.
    - Toolbar actions, severity styling, reveal-latest behavior, and `Copy all` feel coherent together.
    - Documentation captures the accepted direction and smoke path.
    - No scope creep into terminal emulation, shell execution, or explicit channel switching occurs.
  - **Definition of Done**:
    - Full frontend build and test path completed.
    - Manual smoke walkthrough completed against the terminal-backed output surface.
    - Wiki guidance updated if contributor expectations changed materially.
    - Deferred follow-up ideas recorded separately rather than absorbed into scope.
    - Can execute end to end via: complete Studio walkthrough with output-focused review.
  - [x] Task 4.1: Review cross-surface consistency - Completed
    - **Summary**: Confirmed the `Studio Output` panel remains visually aligned with the shell toolbar model and read-only interaction style while the merged stream, severity styling, reveal-latest behavior, and `Copy all` action work together coherently.
    - [x] Step 1: Confirm `Studio Output` aligns visually with the Studio shell panel and toolbar model.
    - [x] Step 2: Confirm the surface remains clearly read-only despite its terminal-grade density.
    - [x] Step 3: Confirm severity palette, merged stream, reveal-latest behavior, and `Copy all` feel coherent together.
  - [x] Task 4.2: Complete verification and documentation - Completed
    - **Summary**: Re-ran the frontend build and test path, captured the successful user smoke validation after rebuilding the browser bundle, updated the Studio shell wiki with the accepted baseline and refresh guidance, and left deferred refinements recorded in the existing plan notes instead of absorbing them into scope.
    - [x] Step 1: Run the full frontend build and test path.
    - [x] Step 2: Complete a final manual smoke walkthrough focused on output behavior.
    - [x] Step 3: Update `wiki/Tools-UKHO-Search-Studio.md` if the accepted terminal-backed baseline needs contributor guidance.
    - [x] Step 4: Record any deferred density or metadata-display refinements separately.
  - **Files**:
    - `docs/067-studio-output-enhancements/spec-studio-output-enhancements_v0.02.md`: update only if implementation evidence requires clarification.
    - `docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.03.md`: keep aligned with completed work status if the plan is later progressed.
    - `wiki/Tools-UKHO-Search-Studio.md`: update Studio shell guidance if implementation materially changes contributor expectations.
    - `src/Studio/Server/search-studio/test/*`: any final smoke-oriented verification notes or additions.
  - **Work Item Dependencies**: Work Items 1, 2, and 3.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Review `Studio Output` during a complete Studio walkthrough, including `Copy all`.
  - **User Instructions**: Review the finished output-pane baseline before requesting additional enhancements such as filtering.

---

## Overall approach and key considerations

- This plan keeps the work package vertical and lightweight: first establish the read-only `xterm.js` host, then add reveal-latest and severity treatment, then complete `Copy all` and interaction polish, and finally close with consistency review and documentation.
- The plan preserves the existing output service and lower-panel ownership model while changing the rendering primitive to achieve the desired density.
- The first release still keeps a single merged stream with explicit visible `time`, `severity`, `source`, and `message` text on each line.
- The terminal-backed surface must remain clearly non-interactive despite using `xterm.js`.
- `Copy all` remains intentionally limited to full merged-stream text to avoid scope expansion into richer export behaviors.
- Future filtering remains easier to add later if `source` metadata stays explicit in the rendered and copied output.
