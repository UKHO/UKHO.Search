# Implementation Plan

Based on: `docs/032-rule-workbench/spec.md`

Target output path: `docs/032-rule-workbench/plans/plan-rule-workbench-implementation_v0.01.md`

## Work Package
- Location: `docs/032-rule-workbench/`
- Scope: Implement the Rule Workbench UI (Blazor Server) + evaluation/reporting functionality described in `docs/032-rule-workbench/spec.md`.

---

## 0. Project structure / conventions (applies to all slices)

- Host/UI project: `tools/RulesWorkbench` (Blazor).
- Scope enforcement:
  - `file-share` only (UI + data access + evaluation) for v1.
  - No HTTP API surface is required for v1. UI uses injected services directly.
- Rule engine reuse:
  - Workbench-only UI state, DTOs, file loading, and editor integration remain inside `tools/RulesWorkbench`.
  - Any changes required to extend rule engine behavior (e.g., evaluation tracing/reporting support) must be implemented in the appropriate inner layer (`Domain`/`Services`/`Infrastructure`) following Onion Architecture.

Shared implementation conventions:
- Async APIs end-to-end (`async`/`await`).
- Logging via `ILogger<T>`.
- Validation: distinguish **ruleset validation errors** (JSON/schema/operator/path) from **runtime missing data** (expected; should yield non-matching predicates / skipped actions).
- Do not persist edits back to disk in v1.
- UI JSON standard:
  - Any UI surface that allows JSON editing MUST use Monaco.
  - Any UI surface that displays JSON MUST use Monaco in read-only mode.
  - Enable JSON folding/collapsing by default.
  - When serializing JSON for display, use `JsonSerializerOptions` with `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` so strings with embedded quotes render like `properties[\"x\"]` instead of `properties[\u0022x\u0022]`.
- Blazor interactivity:
  - For any interactive page/component route in `tools/RulesWorkbench`, specify `@rendermode InteractiveServer`; otherwise input/click handlers may not wire up even if other pages are interactive.
- Testing placement:
  - All unit tests for Workbench services/state (and any non-Playwright UI-adjacent tests) MUST be placed in `test/RulesWorkbench.Tests`.
  - Avoid adding Workbench tests to the general `test/UKHO.Search.Tests` project.

---

# Implementation Plan

## Slice A: Bootstrap “Hello Workbench” (rules snapshot + minimal evaluation)

- [x] Work Item A1: Workbench service boots, loads `file-share` rules once, and renders a UI smoke path - Completed
  - **Purpose**: Establish runnable end-to-end vertical slice proving the Workbench host can load a rules snapshot and render it in the UI.
  - **Acceptance Criteria**:
    - Workbench starts via Aspire and serves a web UI.
    - At startup, rules are loaded once from a private rules file in `tools/RulesWorkbench`.
    - UI shows a landing page with a “rules loaded” summary (count) for `file-share`.
    - Rules are not re-read from disk on each interaction; edits remain in-memory.
  - **Definition of Done**:
    - Code implemented (rules snapshot loader, minimal page)
    - Tests passing (basic integration/smoke)
    - Logging & error handling added
    - Documentation updated (this plan references spec)
    - Can execute end-to-end via: running the Workbench and opening the UI
  - [x] Task A1.1: Add private rules snapshot to Workbench output - Completed
    - Summary: Added `tools/RulesWorkbench/ingestion-rules.json` as a private rules snapshot and configured copy-to-output in `RulesWorkbench.csproj`.
  - [x] Task A1.2: Implement rules snapshot loader (Workbench-only) - Completed
    - Summary: Implemented singleton `RulesSnapshotStore` that loads/parses `ingestion-rules.json` once, extracts `rules.file-share`, logs rule count, and returns structured load errors.
  - [x] Task A1.3: Minimal UI wiring - Completed
    - Summary: Updated `Home.razor` to display file-share rule count and load errors.
  - **Files**:
    - `tools/RulesWorkbench/ingestion-rules.json`: private rules snapshot (content only)
    - `tools/RulesWorkbench/RulesWorkbench.csproj`: mark rules file as content copied to output
    - `tools/RulesWorkbench/Program.cs`: DI registration + load-at-start wiring
    - `tools/RulesWorkbench/Services/RulesSnapshotStore.cs`: in-memory rules snapshot and parsing
    - `tools/RulesWorkbench/Services/FileShareRulesSnapshot.cs`: snapshot model
    - `tools/RulesWorkbench/Services/RulesSnapshotError.cs`: error model
    - `tools/RulesWorkbench/Pages/RuleWorkbench.razor`: landing page
    - `test/RulesWorkbench.Tests/*`: unit/smoke tests for rules snapshot loading (xUnit, no Playwright)
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - Run via Aspire (existing entrypoint)
    - Open Workbench URL; confirm rule count renders
    - Confirm rules are not re-read from disk on navigation

---

## Slice B: Rule browsing (provider rules list + search/filter + selection)

- [x] Work Item B1: Browse `file-share` rules with search/filter and view selected rule JSON (read-only) - Completed
  - **Purpose**: Provide rule authors immediate visibility into the ruleset and the ability to select a rule.
  - **Acceptance Criteria**:
    - UI displays a list of `file-share` rules in ruleset order.
    - UI supports filtering by rule `id` and free text (description / JSON string search).
    - Selecting a rule shows its JSON in a Monaco read-only view and basic metadata.
  - **Definition of Done**:
    - Browsing and selection feature runnable end-to-end
    - Unit tests for filtering logic
    - Unit tests added in `test/RulesWorkbench.Tests` (no Playwright for this slice)
  - [x] Task B1.2: Implement UI rule list + search + selection - Completed
    - [x] Step: Add rule list component with search box.
    - [x] Step: Add a details panel showing selected rule JSON using Monaco read-only.
    - [x] Step: Add a “Copy JSON” action for the selected rule (clipboard) for quick author workflows.
    - Summary: Added `/rules` page that lists and filters file-share rules from the singleton snapshot store and allows selecting a rule and copying its JSON.
  - **Files**:
    - `tools/RulesWorkbench/Components/Pages/Rules.razor`: list + filter + details
    - `tools/RulesWorkbench/Shared/*`: reusable list/detail components (if project conventions prefer)
    - `tools/RulesWorkbench/Services/RulesSnapshotStore.cs`: query methods
    - `tools/RulesWorkbench/Services/RuleSummary.cs`: rules list DTO
    - `tools/RulesWorkbench/Services/ClipboardService.cs`: clipboard abstraction
    - `tools/RulesWorkbench/Services/BrowserClipboardService.cs`: browser clipboard implementation (JS interop)
    - `tools/RulesWorkbench/wwwroot/rulesWorkbench.js`: clipboard JS helper
    - `tools/RulesWorkbench/Components/App.razor`: include clipboard JS
    - `tools/RulesWorkbench/Components/Layout/NavMenu.razor`: add Rules nav item
    - `test/RulesWorkbench.Tests/*`: unit tests for filtering/browsing behavior
  - **Work Item Dependencies**: A1
  - **Run / Verification Instructions**:
    - Navigate to `/rules` in Workbench UI and filter/select rules

---

## Slice C: JSON editing (Monaco preferred) + validation gating

- [x] Work Item C1: Edit a selected rule in JSON mode with syntax validation and builder-mode gate - Completed
  - **Purpose**: Enable power-user edits directly in JSON while preventing invalid JSON from corrupting in-memory rules.
  - **Acceptance Criteria**:
    - Monaco editor is used for JSON editing (no plain-text fallback for v1).
    - Selected rule JSON is editable in Monaco.
    - Invalid JSON shows errors and cannot be applied to the in-memory ruleset.
    - If JSON is invalid, switching to Builder mode is disabled (or blocked) until valid.
    - Edits remain in memory for the session and affect evaluation.
  - **Definition of Done**:
    - JSON editor integrated
    - Validation feedback works
    - UI reflects updated in-memory JSON for the selected rule
    - Tests cover JSON parsing/validation
  - [x] Task C1.1: Add Monaco (or equivalent) JSON editor integration - Completed
    - Summary: Added `JsonEditor` component and JS module interop that loads Monaco via CDN (RequireJS + cdnjs) and enables JSON folding.
  - [x] Task C1.2: Implement rule JSON edit state + apply/cancel - Completed
    - Summary: Added JSON edit mode to `/rules` with draft/apply/cancel and in-memory update of selected rule.
  - [x] Task C1.3: Validation model - Completed
    - Summary: Added syntax validation (System.Text.Json) and gated Apply + Builder switch until JSON is valid.
  - **Files**:
    - `tools/RulesWorkbench/Components/JsonEditor.razor`: Monaco wrapper component
    - `tools/RulesWorkbench/wwwroot/js/monacoEditorInterop.js`: Monaco JS module interop
    - `tools/RulesWorkbench/Components/App.razor`: includes RequireJS loader for Monaco
    - `tools/RulesWorkbench/Services/RuleJsonValidator.cs`: validator interface
    - `tools/RulesWorkbench/Services/RuleJsonValidationResult.cs`: validation result
    - `tools/RulesWorkbench/Services/SystemTextJsonRuleJsonValidator.cs`: System.Text.Json implementation
    - `tools/RulesWorkbench/Services/RulesSnapshotStore.cs`: update rule API
    - `tools/RulesWorkbench/Components/Pages/Rules.razor`: JSON edit UI wiring
    - `test/RulesWorkbench.Tests/*`: unit tests for JSON validation + update behavior
  - **Work Item Dependencies**: B1
  - **Run / Verification Instructions**:
    - Select rule → edit JSON → introduce syntax error (error shown) → fix → apply → re-open rule confirms update

---

## Slice D: Builder mode v1 (guided edits + JSON synchronization)

- [x] Work Item D1: Builder mode for IF predicates and THEN actions (v1 scope) - Completed
  - **Purpose**: Allow safer edits without writing raw JSON; ensure JSON and Builder stay synchronized.
  - **Acceptance Criteria**:
    - Users can switch between Builder and JSON modes when JSON is valid.
    - Builder supports creating a new rule in-memory (no persistence) and adding it to the in-memory `file-share` ruleset.
    - Builder supports creating/editing:
      - Predicates: `properties["x"] == "value"`, `properties["x"] exists`, `files[*].mimeType == "..."`
      - Composition: `all` / `any`
      - Actions: Keywords, SearchText, Content, Taxonomy fields, Version fields
    - Rule `id` is normalized to lowercase in Builder.
    - Duplicate rule `id` is rejected with a clear validation message.
    - Builder updates patch underlying rule JSON and updates JSON view.
  - **Definition of Done**:
    - Builder edits produce valid engine-compatible JSON
    - Unit tests for builder patch operations
    - UI usable end-to-end
  - [x] Task D1.1: Define builder view-models aligned to rule DSL - Completed
    - Summary: Added builder models/enums and a mapper to generate rules JSON from builder state.
  - [x] Task D1.2: Implement builder UI - Completed
    - Summary: Added Builder mode to `/rules` with guided IF/THEN inputs, id normalization to lowercase, and id uniqueness validation.
  - [x] Task D1.3: Synchronization rules - Completed
    - Summary: Switching to Builder is blocked when JSON is invalid; Builder updates generate JSON preview and Apply updates the in-memory ruleset.
  - **Files**:
    - `tools/RulesWorkbench/Builder/*`: builder models + mapping
    - `tools/RulesWorkbench/Components/Pages/Rules.razor`: mode switch + builder UI + JSON sync
    - `tools/RulesWorkbench/Services/RulesSnapshotStore.cs`: in-memory update support reused by builder apply
    - `test/RulesWorkbench.Tests/RuleBuilderMapperTests.cs`: unit tests for mapping
  - **Work Item Dependencies**: C1
  - **Run / Verification Instructions**:
    - Select a rule → switch Builder → change predicate/action → verify JSON updates and remains valid

---

## Slice E: Test input preparation (payload builder + raw JSON)

- [ ] Work Item E1: Payload builder for IndexRequest-like inputs + raw JSON view
  - **Purpose**: Provide rule authors a way to craft messy/realistic inputs quickly and rerun evaluation.
  - **Acceptance Criteria**:
    - UI supports editing payload fields: `Id`, `Timestamp` (optional), `SecurityTokens` (non-empty), `Properties`, `Files` (mimeType, etc.).
    - UI supports raw JSON view (copy/paste) that maps to the same model, using Monaco for editing.
    - Validation errors shown for missing required fields (e.g., empty tokens).
  - **Definition of Done**:
    - Payload can be constructed via forms and via raw JSON
    - Unit tests for payload parsing/validation
  - [ ] Task E1.1: Define evaluation payload contract (Workbench DTO)
    - [ ] Step: Create DTO mirroring ingestion request shape needed by rules engine.
    - [ ] Step: Add conversion into domain ingestion types (`IndexRequest` etc.) using existing types.
  - [ ] Task E1.2: Implement payload builder UI
    - [ ] Step: Form controls for properties list (name/type/value) and files list.
    - [ ] Step: Add raw JSON editor panel bound to same DTO, using Monaco.
    - [ ] Step: Add a “Copy JSON” action for the payload JSON.
  - [ ] Task E1.3: Validation & error messaging
    - [ ] Step: Implement validation (data annotations or custom) and show errors.
  - **Files**:
    - `tools/RulesWorkbench/Contracts/EvaluationPayloadDto.cs`
    - `tools/RulesWorkbench/Services/EvaluationPayloadMapper.cs`
    - `tools/RulesWorkbench/Pages/Evaluate.razor`: payload builder UI
    - `test/...`: unit tests for mapper + validation
  - **Work Item Dependencies**: A1
  - **Run / Verification Instructions**:
    - Navigate to `/evaluate`, create payload, switch to raw JSON, paste/edit, and confirm round-trip

---

## Slice F: Rule evaluation + report (matched rules + canonical document output)

- [ ] Work Item F1: Evaluate in-memory ruleset against payload and display `RuleEvaluationReport`
  - **Purpose**: Deliver the core workbench value: run evaluation using the same engine as ingestion and see outputs + canonical document.
  - **Acceptance Criteria**:
    - Clicking “Run” evaluates `file-share` using the current in-memory rules.
    - Results include:
      - Matched/fired rules in application order with summaries
      - FinalDocument as JSON of `CanonicalDocument` displayed using Monaco read-only
      - Errors/Warnings separated (validation vs runtime)
    - UI shows:
      - Structured report view (matched rules list + canonical document JSON via Monaco read-only)
      - Full `RuleEvaluationReport` JSON (Monaco read-only) in a collapsible “Raw report JSON” section
    - Missing runtime data does not error; it results in non-match/skip behavior.
  - **Definition of Done**:
    - End-to-end evaluation runnable
    - Integration tests assert matched rule IDs and expected canonical output for a sample payload
    - Logging included around evaluation run
  - [ ] Task F1.1: Define evaluation contracts (Workbench DTOs)
    - [ ] Step: Create DTOs for evaluation input and report output within Workbench.
    - [ ] Step: Ensure report captures rule IDs/descriptions and a summary of applied actions.
  - [ ] Task F1.2: Implement evaluation adapter that calls existing rules engine
    - [ ] Step: Use existing rules validation + application logic.
    - [ ] Step: Add minimal “evaluation mode” reporting hook if not present (engine change goes in correct layer).
  - [ ] Task F1.3: Build `RuleEvaluationReport`
    - [ ] Step: Populate matched/fired rules and action summaries.
    - [ ] Step: Serialize final `CanonicalDocument` deterministically.
  - [ ] Task F1.4: UI results panel
    - [ ] Step: Add Run button, show progress, display report.
    - [ ] Step: Display `CanonicalDocument` JSON using Monaco read-only.
    - [ ] Step: Display full `RuleEvaluationReport` JSON using Monaco read-only in a collapsible “Raw report JSON” section.
    - [ ] Step: Add “Copy JSON” actions for `CanonicalDocument` and raw report JSON.
  - **Files**:
    - `tools/RulesWorkbench/Contracts/RuleEvaluationReportDto.cs`
    - `tools/RulesWorkbench/Services/RuleEvaluationService.cs`
    - `tools/RulesWorkbench/Pages/Evaluate.razor`: run + report view
    - `src/*`: only if rule engine needs new reporting extension points
    - `test/...`: integration tests for evaluation
  - **Work Item Dependencies**: C1, E1
  - **Run / Verification Instructions**:
    - In UI: paste payload → Run → confirm matched rules + canonical document JSON
    - Verify that the in-memory edited rule affects the output

---

## Slice G: BatchId prepopulation (read-only DB lookup) for test environments

- [ ] Work Item G1: Load payload JSON by `batchId` using FileShare emulator logic as reference
  - **Purpose**: Provide fast path from a real batch to a realistic evaluation payload.
  - **Acceptance Criteria**:
    - UI allows entering `batchId` and fetching a prepopulated JSON representation of `UKHO.Search.Ingestion.Requests.IngestionRequest`.
    - The JSON is suitable for evaluation verbatim and is editable using Monaco.
    - DB lookup logic is contained within `tools/RulesWorkbench` and uses emulator flow as reference.
    - Failure modes are clear (batch not found, DB unreachable) and do not crash the app.
  - **Definition of Done**:
    - Feature works end-to-end when DB connectivity is available
    - Integration test uses test DB or is marked appropriately for environment
  - [ ] Task G1.1: Implement Batch lookup service (Workbench-only)
    - [ ] Step: Copy relevant query + request construction logic from `tools/FileShareEmulator/Services/IndexService.cs`.
    - [ ] Step: Return DTO suitable for the payload editor.
  - [ ] Task G1.2: UI integration
    - [ ] Step: Add batchId input + Load button that populates payload editor.
    - [ ] Step: After loading, display the prepopulated request JSON in Monaco (editable) and allow copy-to-clipboard.
  - **Files**:
    - `tools/RulesWorkbench/Services/BatchPayloadLoader.cs`
    - `tools/RulesWorkbench/Pages/Evaluate.razor`
    - `test/...`: tests as feasible for env
  - **Work Item Dependencies**: E1
  - **Run / Verification Instructions**:
    - Enter batchId → Load → JSON populated → Run evaluation

---

## Slice H: UI theme parity with `QueryServiceHost` + polish

- [ ] Work Item H1: Align Workbench look-and-feel with `src/Hosts/QueryServiceHost`
  - **Purpose**: Ensure consistent UI styling and shared components across internal tools.
  - **Acceptance Criteria**:
    - Workbench layout, nav, and static assets match `QueryServiceHost` styling.
    - No broken CSS/assets when deployed via Aspire.
  - **Definition of Done**:
    - Visual parity achieved for key surfaces (layout/nav/forms)
    - Smoke-tested locally
  - [ ] Task H1.1: Copy/align `wwwroot` assets and layout structure
    - [ ] Step: Bring across relevant `wwwroot` (css/js/fonts) and shared components.
  - [ ] Task H1.2: Apply consistent component styling to editor + results
    - [ ] Step: Ensure Monaco container sizing and theme works with site theme.
  - **Files**:
    - `tools/RulesWorkbench/Shared/MainLayout.razor`
    - `tools/RulesWorkbench/wwwroot/*`
    - `tools/RulesWorkbench/App.razor` / `Routes.razor`
  - **Work Item Dependencies**: A1
  - **Run / Verification Instructions**:
    - Run Workbench and compare layout to QueryServiceHost

---

## Slice I: Test strategy hardening (Playwright smoke flows)

- [ ] Work Item I1: Add Playwright smoke tests for key flows (browse → edit → evaluate)
  - **Purpose**: Prevent regressions in the UI-driven authoring workflow.
  - **Acceptance Criteria**:
    - Playwright test runs in CI/local and validates:
      - rules list loads
      - select a rule shows JSON
      - edit JSON and apply
      - run evaluation and see report rendered
  - **Definition of Done**:
    - Playwright suite added and passing
    - Test data deterministic (use in-memory rules + fixed payload)
  - [ ] Task I1.1: Create Playwright project/tests (if not already present)
    - [ ] Step: Add tests targeting Workbench base URL.
  - [ ] Task I1.2: Add stable selectors
    - [ ] Step: Add `data-testid` attributes in key UI elements.
  - **Files**:
    - `test/*Playwright*`: E2E test project and tests
    - `tools/RulesWorkbench/Pages/*`: add test ids
  - **Work Item Dependencies**: B1, C1, F1
  - **Run / Verification Instructions**:
    - Run Playwright tests; confirm green

---

# Overall approach / key considerations

- Deliver the Rules Workbench incrementally using vertical slices that are continuously runnable: start from “load rules + show list”, then add editing, payload builder, and evaluation/report.
- Keep Workbench-specific concerns in `tools/RulesWorkbench`; only extend the underlying rules engine when necessary for reporting/tracing, and do so within the appropriate Onion layer.
- Maintain strict separation between validation errors (fail fast) and runtime missing data behavior (expected; do not error).
- Prefer Monaco for JSON editing to ensure a high-quality authoring experience; gate builder-mode switching on valid JSON.
