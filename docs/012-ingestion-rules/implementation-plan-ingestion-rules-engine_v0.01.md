# Implementation Plan

Work Package: `docs/012-ingestion-rules/`

This plan implements **all** requirements in `docs/012-ingestion-rules/spec-ingestion-rules-engine_v0.01.md`, including the complete automated test suite described in §9.

## Project / folder structure

- Domain (existing): `src/UKHO.Search.Ingestion`
  - Contains `IIngestionEnricher`, ingestion request models, `CanonicalDocument`.
- Infrastructure (target for new engine): `src/UKHO.Search.Infrastructure.Ingestion`
  - New: `Rules/` (rules DSL, loader, validator, engine, path resolver, templating).
- Host (rules file location & startup): `src/Hosts/IngestionServiceHost`
  - Add required `ingestion-rules.json` and ensure it is deployed and validated at startup.
- Tests (target): `test/UKHO.Search.Ingestion.Tests`
  - New test suites for parsing/validation, predicate evaluation, action application, templating, provider scoping, and fail-fast behaviors.

---

## Slice 1: End-to-end “rules engine runs in pipeline” (minimal, runnable)

- [x] Work Item 1: Provider-aware rules enrichment integrated into ingestion pipeline - Completed
  - **Purpose**: Establish the provider-scoped invocation path from ingestion pipeline → `IIngestionEnricher` → rules engine, proving the integration with a minimal rule and failing fast at host startup if rules are missing.
  - **Acceptance Criteria**:
    - Ingestion pipeline can supply `providerName` to enrichment logic.
    - A rules file `src/Hosts/IngestionServiceHost/ingestion-rules.json` is required and validated at startup; missing/empty fails startup.
    - For `providerName == "file-share"`, a simple rule can add a keyword to `CanonicalDocument` during enrichment.
  - **Definition of Done**:
    - Provider name is available to enrichers in a scoped manner.
    - Rules file exists, is copied to output, and is loaded/validated during host startup.
    - A rules-based enricher runs during ingestion and mutates `CanonicalDocument`.
    - Unit + integration tests for this slice pass.
    - Can execute end-to-end via: `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`.

  - [x] Task 1.1: Add a provider context accessor used by enrichers - Completed
    - [x] Step 1: In `src/UKHO.Search.Ingestion`, add a new public interface `IIngestionProviderContext` (one public type per file) with a settable `string? ProviderName`.
    - [x] Step 2: In `src/UKHO.Search.Infrastructure.Ingestion`, add `IngestionProviderContext` implementing the interface and register it as **scoped** in `AddIngestionServices()`.
    - [x] Step 3: Update `ApplyEnrichmentNode` to:
      - store the constructor `providerName` in a private field
      - resolve `IIngestionProviderContext` from the scope (optional) and set `ProviderName` before executing enrichers.

  - [x] Task 1.2: Add rules engine public API and a pipeline enricher wrapper - Completed
    - [x] Step 1: In `src/UKHO.Search.Infrastructure.Ingestion/Rules/`, create:
      - `IIngestionRulesEngine` with `void Apply(string providerName, IngestionRequest request, CanonicalDocument document)` (mutates `document` in-place).
      - `IngestionRulesEnricher : IIngestionEnricher` that reads `providerName` from `IIngestionProviderContext` and calls `IIngestionRulesEngine`.
    - [x] Step 2: Register `IngestionRulesEnricher` as `Scoped<IIngestionEnricher>` in `AddIngestionServices()` with an `Ordinal` that runs early but after any required canonical creation (e.g., `Ordinal = 50`).

  - [x] Task 1.3: Add minimal rules file + startup loading hook (fail-fast) - Completed
    - [x] Step 1: Create `src/Hosts/IngestionServiceHost/ingestion-rules.json` with `schemaVersion: "1.0"` and at least one rule for provider `file-share`.
    - [x] Step 2: Ensure it is copied to output (e.g., `CopyToOutputDirectory` in `IngestionServiceHost.csproj` or as `Content`).
    - [x] Step 3: Add a rules loader/validator service (see Slice 2) and invoke it during startup to guarantee fail-fast.
      - Preferred hook: extend `UKHO.Search.Infrastructure.Ingestion.Bootstrap.BootstrapService.BootstrapAsync()` to validate rules, because the host already calls bootstrap during startup.

  - [x] Task 1.4: Tests for Slice 1 integration - Completed
    - [x] Step 1: Add unit tests ensuring `ApplyEnrichmentNode` sets `IIngestionProviderContext.ProviderName` when `providerName` is provided.
    - [x] Step 2: Add a minimal end-to-end test in `test/UKHO.Search.Ingestion.Tests`:
      - Build a service provider containing `AddIngestionServices()` registrations.
      - Resolve enrichers and run `IngestionRulesEnricher.TryBuildEnrichmentAsync()` with a fake `IIngestionProviderContext.ProviderName = "file-share"`.
      - Assert keyword was added per the minimal rule.

  - **Files**:
    - `src/UKHO.Search.Ingestion/Rules/IIngestionProviderContext.cs`: new interface.
    - `src/UKHO.Search.Ingestion/Pipeline/Nodes/ApplyEnrichmentNode.cs`: set provider context in scope.
    - `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`: register provider context + rules enricher/engine.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/IIngestionRulesEngine.cs`: engine API.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/IngestionRulesEnricher.cs`: pipeline enricher wrapper.
    - `src/Hosts/IngestionServiceHost/ingestion-rules.json`: required rules file.
    - `src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`: copy rules file to output.
    - `test/UKHO.Search.Ingestion.Tests/*`: new tests.

  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
    - `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`
  - **User Instructions**:
    - None; rules file is committed and required.

  - **Completed Summary**:
    - Added scoped provider context (`IIngestionProviderContext`) and ensured `ApplyEnrichmentNode` sets it per-scope.
    - Implemented minimal rules catalog + engine + enricher (`IIngestionRulesEngine`) and registered via `AddIngestionServices()`.
    - Added `ingestion-rules.json` minimal rule and forced fail-fast load/validation during `BootstrapService.BootstrapAsync()`.
    - Added unit + integration tests; verified with `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`.

---

## Slice 2: DSL parsing + fail-fast validation (schema, predicates, paths, actions)

- [x] Work Item 2: Implement JSON DSL parsing and comprehensive validation on load - Completed
  - **Purpose**: Implement the v0.01 JSON DSL contract, ensuring invalid JSON/schema/operators/path syntax fail startup, and empty/missing rules fail startup.
  - **Acceptance Criteria**:
    - Rules file must be present; missing file fails startup.
    - `schemaVersion` is required and must equal `"1.0"`.
    - `rules` object must contain at least one provider key with a non-empty rule array.
    - Every rule validates required fields (`id`, predicate block, `then`).
    - Predicate validation supports shorthand AND-only and explicit boolean forms (`all`/`any`/`not`), including non-empty arrays.
    - Path syntax is validated at startup, including:
      - case-insensitive segments
      - wildcard-only array selection (`[*]`), no numeric indexes
      - explicit collection access requirement (e.g., reject `files.mimeType`)
      - `properties.<name>` and `properties["<name>"]` forms
    - `then.facets.add` rejects entries containing both `value` and `values`.
    - `then.documentType.set` validates scalar-only resolution rules (see Slice 4).
  - **Definition of Done**:
    - Loader parses via `System.Text.Json`.
    - Validator provides structured exceptions/messages and fails load.
    - Tests cover all fail-fast scenarios listed in §9.

  - [x] Task 2.1: Create DTOs + loader - Completed
    - [x] Step 1: Add internal DTOs in `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/`:
      - `RulesetDto` (`schemaVersion`, `rules: Dictionary<string, RuleDto[]>`).
      - `RuleDto` (`id`, `description?`, `enabled?`, `if?`, `match?`, `then`).
      - `ThenDto` and action DTOs.
      - `FacetAddDto` with `name`, `value?`, `values?`.
    - [x] Step 2: Add `IngestionRulesLoader` to read `ingestion-rules.json` from host content root (use `IHostEnvironment.ContentRootPath`).

  - [x] Task 2.2: Implement validator - Completed
    - [x] Step 1: Create `IngestionRulesValidator` producing a compiled immutable model (or validated DTO) and throwing on any validation error.
    - [x] Step 2: Validate:
      - required file present
      - supported `schemaVersion`
      - `rules` has at least one provider with at least one rule
      - rule `id` present, unique per provider (recommended for operability)
      - predicate aliasing (`if` or `match` exactly one)
      - enabled defaults to true
      - `then` block shape
      - facets add entries (no `value`+`values`)
      - boolean predicate node constraints

  - [x] Task 2.3: Path parser + syntax/type validation - Completed
    - [x] Step 1: Implement a path parser producing a list of steps (case-insensitive matching):
      - property access steps
      - wildcard enumerable step (`[*]`)
      - special `properties` lookup steps supporting dot and bracket name access
    - [x] Step 2: Validate path syntax:
      - reject numeric indexes (`[0]`)
      - reject filter selectors (`[name=...]`)
      - reject missing wildcard when traversing known collections (via reflection over `AddItemRequest`/`UpdateItemRequest` types)
    - [x] Step 3: Ensure all path segments resolve to an accessible member on the active payload type (reflection, case-insensitive), except `properties["..."]` / `properties.<name>` where the name is runtime.

  - [x] Task 2.4: Startup hook and startup logging - Completed
    - [x] Step 1: Add `IIngestionRulesCatalog` (validated compiled rules) and register as singleton.
    - [x] Step 2: In `BootstrapService.BootstrapAsync()`, resolve and validate the catalog (forcing load) and log:
      - number of providers with rules
      - number of rules per provider and their `id` values

  - [x] Task 2.5: Tests for parsing/validation - Completed
    - [x] Step 1: Add tests for invalid JSON and missing file.
    - [x] Step 2: Add tests for unsupported/missing `schemaVersion`.
    - [x] Step 3: Add tests for empty ruleset (no providers / empty arrays).
    - [x] Step 4: Add tests for invalid predicate shapes (`all`/`any` empty, `not` array form, multiple keys).
    - [x] Step 5: Add tests for invalid path syntax (missing wildcard, numeric index, selector syntax).
    - [x] Step 6: Add tests for invalid facet entries (`value` + `values`).

  - **Completed Summary**:
    - Added `System.Text.Json` DTOs under `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/` plus `IngestionRulesLoader`.
    - Implemented `IngestionRulesValidator` with boolean predicate validation, provider/rule shape checks, and facets validation.
    - Implemented path parsing + reflection-based validation enforcing wildcard-only collection traversal (`[*]`) and `properties.<name>` / `properties["<name>"]` special-case.
    - Reworked `IIngestionRulesCatalog` to be validation-backed and added startup logging in `BootstrapService`.
    - Added comprehensive fail-fast unit tests in `test/UKHO.Search.Ingestion.Tests/Rules/RulesetValidationTests.cs`.

  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/*`: loader, validator, DTOs, compiled model.
    - `src/UKHO.Search.Infrastructure.Ingestion/Bootstrap/BootstrapService.cs`: force-load + startup logging.
    - `src/Hosts/IngestionServiceHost/ingestion-rules.json`: remains required.
    - `test/UKHO.Search.Ingestion.Tests/Rules/*`: new unit tests.

  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

---

## Slice 3: Predicate evaluation engine (operators + boolean composition)

- [x] Work Item 3: Evaluate predicates against active payload (`AddItem`/`UpdateItem`) with bounded semantics - Completed
  - **Purpose**: Implement rule matching semantics (including shorthand and boolean predicates), operator behaviors, multi-value ANY-match semantics, and runtime missing-path behavior.
  - **Acceptance Criteria**:
    - Engine evaluates only rules for `providerName`.
    - Active payload is `request.AddItem` or `request.UpdateItem`.
      - If neither present, engine performs no mutation and reports a structured error (see Task 3.4).
    - Shorthand AND-only predicates work as `eq` comparisons.
    - Boolean predicates support `all`/`any`/`not` nesting.
    - Supported operators: `exists`, `eq`, `contains`, `startsWith`, `endsWith`, `in`.
    - String comparisons are case-insensitive, trimmed, invariant.
    - Missing/unresolvable paths at runtime do **not** throw; they cause non-match.
    - Multi-value paths default to ANY-match semantics.
  - **Definition of Done**:
    - Predicate evaluation returns a `MatchResult` containing:
      - `IsMatch`
      - matched rule id
      - matched values binding for `$val`
    - Full unit test coverage for operator semantics and missing-path behavior.

  - [x] Task 3.1: Implement runtime path resolution - Completed
    - [x] Step 1: Implement `IPathResolver` that, given an active payload object, returns resolved values as `IReadOnlyList<string>` (coerce values using `ToString()`; null → empty).
    - [x] Step 2: Special-case `properties.<name>` and `properties["<name>"]` to search `payload.Properties` (case-insensitive name match) and return `Value` coerced to string.
    - [x] Step 3: For reflection-based paths, traverse properties case-insensitively; for `[*]`, flatten enumerable elements.

  - [x] Task 3.2: Implement operator evaluation - Completed
    - [x] Step 1: Normalize candidate strings using the same comparison normalization: `Trim().ToLowerInvariant()`.
    - [x] Step 2: Implement operators over resolved values (ANY-match):
      - `exists`: any resolved value is non-null/non-empty
      - `eq`: any value equals comparator
      - `contains`/`startsWith`/`endsWith`: any value satisfies
      - `in`: any value is in the provided set

  - [x] Task 3.3: Boolean composition + shorthand evaluation - Completed
    - [x] Step 1: Implement predicate tree evaluation for `all`/`any`/`not`.
    - [x] Step 2: Implement shorthand form `{ "<path>": "<string>" }` as implicit AND of `eq` leaves.

  - [x] Task 3.4: Active payload selection and structured error - Completed
    - [x] Step 1: In the engine, choose active payload:
      - if `request.AddItem != null` use it
      - else if `request.UpdateItem != null` use it
      - else: no-op + emit a structured debug log event indicating unsupported request type for rules engine.

  - [x] Task 3.5: Tests for predicate evaluation - Completed
    - [x] Step 1: Tests for each operator with scalar and wildcard (`files[*].MimeType`) paths.
    - [x] Step 2: Tests for boolean composition: nested `not`, combined `any`/`all`.
    - [x] Step 3: Tests for shorthand AND behavior.
    - [x] Step 4: Tests that missing runtime fields do not throw and simply do not match.

  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/*`: resolver + predicate evaluator.
    - `test/UKHO.Search.Ingestion.Tests/Rules/Predicate*Tests.cs`: new tests.

  - **Completed Summary**:
    - Added runtime path resolution (`Rules/Evaluation`) supporting case-insensitive reflection, wildcard flattening (`[*]`), and `properties.<name>` / `properties["<name>"]` lookup.
    - Implemented normalized (trim/lowercase invariant) operator evaluation with ANY-match semantics (`exists`, `eq`, `contains`, `startsWith`, `endsWith`, `in`).
    - Implemented boolean predicate evaluation (`all`/`any`/`not`) plus shorthand AND form.
    - Updated `IngestionRulesEngine` to select `AddItem`/`UpdateItem` as active payload, evaluate predicates, and apply only matching rules; unsupported request types are ignored with a debug log.
    - Added unit tests for path resolution, operator semantics, boolean composition, shorthand predicates, and missing-path non-match behavior.

  - **Work Item Dependencies**: Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

---

## Slice 4: Action application + variables/templating + observability

- [x] Work Item 4: Apply actions to `CanonicalDocument` with `$val` / `$path:` templating, dedupe rules, and required logging - Completed
  - **Purpose**: Implement all action types with correct normalization and list expansion semantics, plus per-request debug logging.
  - **Acceptance Criteria**:
    - `then.keywords.add` adds normalized keywords (skip null/empty).
    - `then.searchText.add` appends discrete phrases and deduplicates phrases per field (case-insensitive after canonical normalization).
    - `then.content.add` appends discrete phrases and deduplicates phrases per field.
    - `then.facets.add` supports `value` and `values` entries; adds normalized facet name/value(s).
    - `then.documentType.set` sets `CanonicalDocument.DocumentType` using templating; must resolve to exactly one value.
    - `$val` and `$path:<path>` variables work with list expansion and are skipped when null/empty.
    - Unknown variables are treated as missing (skip output).
    - Debug logging per request includes provider name, matched rule ids (in file order), and summary of actions applied.
  - **Definition of Done**:
    - Engine applies **all** matching enabled rules in file order for the provider.
    - Comprehensive tests for actions, templating, dedupe, and documentType scalar enforcement.

  - [x] Task 4.1: Match bindings for `$val` - Completed
    - [x] Step 1: Extend predicate evaluation to return matched value(s) for each leaf condition that matched.
    - [x] Step 2: Define `$val` for a matched rule as the concatenation of all matched leaf values in evaluation order.
      - For wildcard paths, `$val` includes all matching values (not all resolved values).
      - For `exists`, `$val` includes resolved non-empty values.

  - [x] Task 4.2: Implement template expansion - Completed
    - [x] Step 1: Implement a small template expander where any `$...` token is treated as a variable reference (no escaping).
    - [x] Step 2: Supported variables:
      - `$val`
      - `$path:<path>`
    - [x] Step 3: If a variable resolves to multiple values:
      - if used as whole-string value: returns all values
      - if used inside a template string: expand to one output string per value
    - [x] Step 4: If a variable resolves null/empty or unknown, skip the produced value(s).

  - [x] Task 4.3: Apply each action type - Completed
    - [x] Step 1: Keywords: expand templates → call `document.AddKeyword()` per produced value.
    - [x] Step 2: SearchText:
      - Expand templates → for each phrase, only apply if it is not already present in `document.SearchText` as a phrase (boundary-aware substring match after normalization).
      - Apply via `document.SetSearchText()`.
    - [x] Step 3: Content: same as SearchText, using `document.Content` and `document.SetContent()`.
    - [x] Step 4: Facets:
      - For each entry, expand `name` and values.
      - If `value`: expand to 0..N values; apply each via `document.AddFacetValue(name, value)`.
      - If `values`: expand each element and apply via `document.AddFacetValues(name, values)`.
    - [x] Step 5: DocumentType:
      - Expand template to produced values.
      - Enforce exactly one produced value; if zero, do not set; if >1, treat as ruleset validation error (enforced in Task 4.4).

  - [x] Task 4.4: Strengthen validation for `documentType.set` scalar safety - Completed
    - [x] Step 1: During load validation, reject `then.documentType.set` templates that can produce multiple values:
      - reject `$val` when the rule predicate may produce multiple values (wildcard path or multiple leaves)
      - reject `$path:` referencing paths containing `[*]`
    - [x] Step 2: Add tests verifying these rules fail startup validation.

  - [x] Task 4.5: Observability - Completed
    - [x] Step 1: Add `ILogger<IngestionRulesEngine>` and log at `Debug` per request:
      - provider name
      - matched rule ids in application order
      - summary counts for each action applied (keywords/searchText/content/facets/documentType)
    - [x] Step 2: Ensure startup logging (Work Item 2) includes counts and rule ids per provider.

  - [x] Task 4.6: Tests for actions + templating - Completed
    - [x] Step 1: Keywords add + normalization/dedupe via `SortedSet`.
    - [x] Step 2: SearchText/content phrase dedupe (case-insensitive after normalization).
    - [x] Step 3: Facet add single + multi-value.
    - [x] Step 4: `$val` scalar and list expansion, including templated string expansion.
    - [x] Step 5: `$path:<path>` scalar and wildcard expansion.
    - [x] Step 6: Unknown variable and null/empty variables skip output.
    - [x] Step 7: Provider scoping and rule order: all matching enabled rules applied in file order.

  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/IngestionRulesEngine.cs`: main engine.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Templating/*`: template expander.
    - `test/UKHO.Search.Ingestion.Tests/Rules/Actions*Tests.cs`: new tests.

  - **Completed Summary**:
    - Implemented `$val` / `$path:` template expansion with list expansion semantics and skip-on-missing behavior.
    - Implemented full action application for `keywords.add`, `searchText.add`, `content.add`, `facets.add` and `documentType.set` (scalar-only).
    - Added phrase-level dedupe for `SearchText` and `Content` (boundary-aware match after normalization).
    - Strengthened startup validation to reject `documentType.set` templates that can yield multiple values (wildcards / multi-leaf `$val`, wildcard `$path:`).
    - Added per-request `Debug` logging with provider name, matched rule ids, and applied-action counts.
    - Added unit/integration tests covering templating, action behavior, provider scoping, rule order, and validation failures.

  - **Work Item Dependencies**: Work Item 3.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

---

## Slice 5: Comprehensive fail-fast and regression suite (spec §9 completion)

- [x] Work Item 5: Complete and comprehensive automated test suite, including startup-fail conditions - Completed
  - **Purpose**: Close the spec’s definition of “complete” by ensuring §9 test coverage exists and is passing.
  - **Acceptance Criteria**:
    - Tests exist and pass for all items in §9:
      - parsing/validation
      - predicate evaluation
      - action application
      - variable binding
      - provider scoping
      - fail-fast validation scenarios
    - Startup-fail tests cover missing file and empty rules.
  - **Definition of Done**:
    - All tests pass locally and in CI.

  - [x] Task 5.1: Startup-fail tests - Completed
    - [x] Step 1: Test bootstrap fails when `ingestion-rules.json` missing.
    - [x] Step 2: Test bootstrap fails when rules are empty (no providers or empty arrays).

  - [x] Task 5.2: Regression tests for example rules - Completed
    - [x] Step 1: Create a test rules file containing the full §5.8 example rules for provider `file-share`.
    - [x] Step 2: Create requests that trigger each rule and assert the resulting `CanonicalDocument` mutations:
      - MIME type `app/s63` → keyword + searchText additions
      - property `abcdef == "a value"` → keyword additions
      - property `abcdef` exists → facet added with `$path:properties["abcdef"]`

  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/Rules/RulesEngineEndToEndExampleTests.cs`: new tests.

  - **Completed Summary**:
    - Added bootstrap-level fail-fast tests ensuring startup fails when `ingestion-rules.json` is missing or contains no provider rules.
    - Added end-to-end regression tests implementing the spec §5.8 example rules and asserting `CanonicalDocument` mutations (keywords/searchText/facets).

  - **Work Item Dependencies**: Work Item 4.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

---

## Summary / key considerations

- The engine is implemented in `UKHO.Search.Infrastructure.Ingestion` and integrated via `IIngestionEnricher` so it remains provider-agnostic.
- Startup is fail-fast by forcing rules load/validation during the host bootstrap path.
- Predicate evaluation is bounded and deterministic: no arbitrary code, no external calls, wildcard-only array traversal.
- Runtime missing data is non-fatal: missing path values simply cause non-match and/or skipped outputs.
- The test suite is treated as part of the deliverable; §9 coverage is completed within this plan.
