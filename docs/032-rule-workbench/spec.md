# Specification: Rule Workbench (Rules authoring + evaluation + report)

Target output path: `docs/032-rule-workbench/spec.md`

## 1. Overview

### 1.1 Purpose

Provide an internal web-based Rule Workbench to help author, validate, and evaluate ingestion rules against messy real-world source data.

The workbench must allow users to:

- Browse rule sets for the `file-share` provider.
- Edit individual rules with a high-quality JSON editing experience.
- Prepare test inputs equivalent to the ingestion payload (`IndexRequest`-like property bag, plus files/tokens), run evaluation, and see:
  - which rules matched/fired
  - what outputs were produced
  - what the resulting `CanonicalDocument` looks like

The workbench should be deployable alongside the stack into test environments (Aspire).

### 1.2 Goals

- Provide a web UI for rule evaluation using the same rules engine as ingestion.
- Provide a `RuleEvaluationReport` that describes which rules fired and the resulting transformed output.
- Provide a JSON editor (preferably Monaco) for editing an individual rule object.
- Provide an input builder for a property bag similar to `IndexRequest.Properties` (and related fields used by rule evaluation).
- Provide quick feedback loops for rule authors: validation errors, missing path behavior, output preview.

- Support evaluating rules for the `file-share` provider only (initial scope).
- Allow entering a `batchId` to prepopulate a JSON representation of `UKHO.Search.Ingestion.Requests.IngestionRequest` for evaluation.

### 1.2.1 Starting point (implementation bootstrap)

The standard Blazor project at `tools/RulesWorkbench` will already exist and be hooked into Aspire so it can run locally and be deployed into test environments (including appropriate DI wiring such as database connectivity). Subsequent work focuses on implementing the rule browsing/editing/evaluation features.

### 1.3 Non-goals

- Persisting/saving edited rules back to `ingestion-rules.json` (for now).
- Choosing the long-term storage mechanism for rules (JSON file vs blob storage vs other) (explicitly undecided).
- Building a full ETL/data-cleansing solution for messy inputs.
- Production hardening as an end-user product; this is an internal workbench.

### 1.4 Assumptions

- The existing ingestion rules engine can be reused for evaluation.
- The existing ingestion model types (e.g. `IndexRequest`, `IngestionProperty`, `IngestionFileList`) represent the expected payload shape.
- Missing runtime data should not be treated as an error: if a referenced property/path is not present at evaluation time, the condition simply does not match and/or derived outputs are skipped.

### 1.5 Stakeholders

- Rule authors (engineers, analysts) working with messy source data.
- Ingestion service maintainers.
- QA/test environment maintainers.

### 1.6 Risks and considerations

- **Messy inputs**: workbench must make it easy to experiment without requiring perfectly structured payloads.
- **Safety**: do not allow the workbench to mutate production systems; keep evaluation isolated.
- **Drift**: evaluation logic must stay aligned with ingestion runtime behavior.
- **Security**: test payloads may contain sensitive data; access control and logging should be considered.

### 1.7 Technical decisions to confirm

- Whether the workbench evaluates:
  1) only `file-share` initially, or
  2) any provider.

  Decision: (1) only `file-share`.

  Initial implementation note: there is no additional discovery mechanism required beyond parsing the rules JSON to enumerate the `file-share` provider rules.

- How rules are loaded:
  1) from a file/config snapshot per environment, or
  2) via a future rules store abstraction.

  Initial implementation note: rules are loaded from a private copy of `ingestion-rules.json` placed in the root of the `tools/RulesWorkbench` project.
  - Decision: include this private rules file as a content file copied to output.

## 2. System / Component scope

### 2.1 In scope

- A new internal web experience (“Rule Workbench”).
- Ability to browse rules grouped by provider.
- Ability to edit an individual rule (JSON).
- Ability to provide test input data and execute evaluation.
- A `RuleEvaluationReport` describing matches and outputs.

### 2.2 Out of scope

- Persisting edited rules back to the repo file.
- Blob storage rules management (future option).
- Operational tooling for bulk rules migration.

### 2.3 High-level change summary

- Add a Workbench UI and API that:
  - loads a ruleset snapshot
  - runs the existing rule engine in “evaluation mode”
  - returns a report containing fired rules and resulting `CanonicalDocument`

## 3. Functional requirements (high-level)

### 3.1 Rules browsing

- The UI SHALL target the `file-share` provider.
- The UI SHALL display rules in the same way rules are grouped in `ingestion-rules.json` (`rules.file-share` array).
- The UI SHOULD support searching/filtering by rule `id` and text.

### 3.2 Rule editing

- The UI SHALL allow selecting an individual rule.
- Builder mode v1 SHALL support creating new rules (in-memory only) as well as editing existing rules from the private rules file.

- When creating/editing a rule `id` in Builder mode, the UI SHOULD normalize user input to lowercase (e.g., if the user enters capitals, lowercase it and redisplay).
- The UI SHALL support dual-mode editing:
  - **Builder mode**: guided editing controls that patch/update the underlying rule JSON.
  - **JSON mode**: direct JSON editing for power users.
- In **Builder mode**, the UI SHALL allow the user to:
  - select a source field/path (e.g. `properties["..."]`)
  - choose a condition (e.g. equals, exists)
  - select which `CanonicalDocument` target field/action to set/add (supported `then` actions)
  - the Workbench SHALL apply these changes by patching the rule JSON representation.

- The primary aim of Builder mode is to allow users to build and modify rules without needing to write JSON from scratch.

- Builder mode v1 SHALL support creating/editing IF predicates using:
  - `properties["x"] == "value"` (string equals)
  - `properties["x"] exists`
  - `files[*].mimeType == "..."`
  - composition using `all` / `any`

- Builder mode v1 SHALL support creating/editing THEN actions for:
  - Keywords
  - SearchText
  - Content
  - Taxonomy fields: Category, Series, Instance, Authority, Region, Fornat
  - Version fields: MajorVersion, MinorVersion
- In **JSON mode**, the UI SHALL show the selected rule JSON in an editor.
- The JSON editor SHOULD provide a high-quality JSON editing experience (Monaco preferred).
- The UI SHALL validate rule JSON syntax.
- The system SHALL surface rule schema/path/operator validation errors distinctly from runtime “missing data” behavior.

- The UI SHOULD keep Builder and JSON views synchronized for valid JSON.
- If the user enters invalid JSON in JSON mode, the UI SHOULD:
  - show validation errors
  - prevent switching back to Builder mode until JSON is valid again.

### 3.3 Test input preparation

- The UI SHALL allow authors to construct an evaluation input payload closely matching ingestion payloads.
- The payload builder MUST support:
  - `Id`
  - `Timestamp` (or allow defaulting)
  - `SecurityTokens` (non-empty)
  - `Properties` (name/type/value)
  - `Files` (file list entries, including MIME type)
- The UI SHOULD provide a convenient raw JSON view for copy/paste.

- The UI SHALL allow entering a `batchId` and prepopulating a JSON representation of `UKHO.Search.Ingestion.Requests.IngestionRequest`.
  - The prepopulated JSON MUST be suitable for use verbatim as evaluation input.
  - Users MUST be able to edit the JSON to try different values.
  - The batch lookup and request construction SHOULD follow the same logic as the FileShare emulator, referencing:
    - `tools/FileShareEmulator/Services/IndexService.cs` (see class `FileShareEmulator.Services.IndexService`, method flow `IndexBatchByIdAsync(...)` → `CreateRequestAsync(...)`).
    - Type: `src/UKHO.Search.Ingestion/Requests/IngestionRequest.cs` (`UKHO.Search.Ingestion.Requests.IngestionRequest`).
  - Decision: copy the relevant SQL/query logic into `tools/RulesWorkbench` (keep it contained to the Workbench), while using the emulator implementation as the reference.

### 3.4 Rule evaluation

- The workbench SHALL allow running evaluation for the `file-share` provider.
- On Workbench load, the system SHALL read the `rules.file-share` ruleset from the private rules JSON file once and create a single in-memory editable copy.
- The default "Run" behavior SHALL evaluate the entire in-memory `rules.file-share` ruleset against the current payload.
- The workbench SHALL run evaluation using only the current in-memory edited ruleset copy.
- The workbench SHALL NOT re-read the source rules file on each run, and SHALL NOT merge or refresh rules from disk during the session.
- The engine SHALL produce outputs consistent with ingestion.

- After the user clicks the "Run" action, the UI SHALL display the transformed `CanonicalDocument` output as JSON somewhere in the results area.

### 3.5 Evaluation reporting

- The results UI SHALL display:
  - which rules matched/fired
  - ordering of application (ruleset order)
  - resulting `CanonicalDocument` state (including a JSON representation)

- The report SHOULD include action summaries per rule (counts of values added).
- The report MAY include detailed predicate trace (future enhancement).

### 3.6 Usability

- The workbench SHOULD support rapid iteration:
  - edit rule
  - edit payload
  - re-run evaluation
  - compare results

## 4. Technical requirements

### 4.1 Architecture constraints

- The repository follows Onion Architecture:
  - `Hosts -> Infrastructure -> Services -> Domain`

  Architecture enforcement for this work:
  - Any code changes required to **extend the ingestion rule engine** (e.g., new evaluation tracing/reporting capabilities that are part of the engine) MUST follow the repository Onion Architecture and be placed in the appropriate inner layer (Domain/Services/Infrastructure) rather than in the host.
  - Any code that is purely implementing the **Rules Workbench** (UI, workbench-specific models/DTOs, UI services, JSON editor integration, in-memory editing state, simple file loading of the private rules copy) MUST remain in the `tools/RulesWorkbench` project.

- The workbench UI/API MUST be implemented as a Host-layer component.
- Rule evaluation logic MUST reuse existing domain/services/infrastructure components rather than duplicating logic.

### 4.2 Deployability

- The workbench MUST run as a web service deployable into a test environment.
- The workbench SHOULD integrate with Aspire so it can be run locally and deployed to test.

### 4.2.1 UI styling and Blazor setup consistency

- The RulesWorkbench UI styling/theme MUST match `src/Hosts/QueryServiceHost`.
- The Blazor setup (layout, shared components, and static assets) SHOULD be taken from `src/Hosts/QueryServiceHost`, including `wwwroot` contents, so the look-and-feel is consistent.

### 4.3 Rule loading (no persistence)

- In the first iteration, rules SHOULD be loaded once from a ruleset snapshot source at Workbench start.
- Edits SHALL remain in-memory for the session and shall not persist by default.

### 4.4 API contracts (indicative)

- `GET /api/rules/providers`
  - returns provider names and basic metadata

- `GET /api/rules/{provider}`
  - returns rule list (id, description, JSON object)

- `POST /api/evaluate`
  - request: provider name + payload + optional rule override
  - response: `RuleEvaluationReport`

### 4.5 RuleEvaluationReport (indicative)

- Fields:
  - `ProviderName`
  - `MatchedRules`: list of `{ RuleId, Description, Summary }`
  - `FinalDocument`: serialized `CanonicalDocument`
  - `Warnings` / `Errors` (validation vs runtime)

## 5. Acceptance criteria

1. Workbench is accessible as a web UI.
2. Users can browse rules grouped by provider.
3. Users can select a rule and edit it with JSON tooling (Monaco or equivalent).
4. Users can construct/paste a payload (IndexRequest-like) and execute evaluation.
5. Workbench displays a `RuleEvaluationReport` including which rules matched and the resulting `CanonicalDocument`.
6. No rule persistence back to `ingestion-rules.json` occurs.

## 6. Validation / Test plan
- Unit tests:
  - Validate `RuleEvaluationReport` generation logic.
  - Validate request parsing for evaluation payloads.

- Integration tests:
  - Evaluate known sample payloads and assert:
    - correct matched rule IDs
    - expected canonical outputs

- UI testing:
  - Smoke test the basic flows: load rules -> edit -> evaluate -> view report.

## 7. Open questions (to resolve before implementation)

1. Provider scope for first release:
   - Decision: `file-share` only.

2. How will the workbench obtain test payloads?
   - Decision: (2) optional read-only “load from database by id” in test environments.

3. Authentication/authorization expectations for test environment deployment?
   - Decision: authentication/authorization not required.
