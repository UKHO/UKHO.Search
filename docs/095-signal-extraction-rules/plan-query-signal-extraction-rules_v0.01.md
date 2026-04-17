# Implementation Plan

**Target output path:** `docs/095-signal-extraction-rules/plan-query-signal-extraction-rules_v0.01.md`

Work Package: `docs/095-signal-extraction-rules/`

Based on: `docs/095-signal-extraction-rules/spec-domain-query-signal-extraction-rules_v0.01.md`

Mandatory repository instructions for execution:

- `./.github/instructions/wiki.instructions.md`
- `./.github/instructions/documentation-pass.instructions.md`
- `./.github/instructions/documentation.instructions.md`
- `./.github/instructions/coding-standards.instructions.md`

Repository planning notes carried into this plan:

- This repository uses Onion Architecture: `Hosts -> Infrastructure -> Services -> Domain`.
- Query-side design must include Microsoft Recognizers now, but keep it behind `ITypedQuerySignalExtractor`.
- Query rules are global across search, authored flat under `./rules/query`, and loaded from the `rules:query` configuration prefix.
- The documentation-pass rules in `./.github/instructions/documentation-pass.instructions.md` are a hard Definition of Done gate for every code-writing task in this plan. Execution must fully comment every class, every method, every constructor, every public parameter, and every non-obvious property written or updated during implementation, including internal and other non-public types.
- Wiki review is a mandatory completion gate for every work item in this plan. Execution must record which wiki or repository guidance pages were updated, created, retired, or why no wiki page update was needed.
- Do not run the full test suite for this work package. Prefer targeted builds and targeted test projects that cover the changed query-side slices.

## Query-side search foundation and delivery strategy

This plan is organized as vertical slices. Each work item results in a runnable end-to-end capability through the existing query host, even if later slices add richer extraction, rule behavior, and diagnostics. The early slices intentionally prove the architecture with a minimal but demonstrable path before layering the full signal extraction model on top.

The overall sequence is:

1. establish a real query planning and Elasticsearch execution path in place of the stub client
2. add Microsoft Recognizers-backed typed extraction behind `ITypedQuerySignalExtractor`
3. add the query rules engine and flat `rules:query:*` loading model
4. add explicit rule-driven filters, boosts, refresh, and richer diagnostics
5. close the work package with a mandatory explicit wiki review/update record

## Query planning and execution bootstrap slice
- [x] Work Item 1: Replace the stub search path with a real default-only query planning slice - Completed
  - **Purpose**: Deliver the smallest meaningful end-to-end query capability so `QueryServiceHost` uses a real query planning pipeline and Elasticsearch execution path instead of the current stub client.
  - **Acceptance Criteria**:
    - Entering a query in the existing query UI uses a real application/service/infrastructure path instead of `StubQueryUiSearchClient`.
    - The new path lowercases and cleans the query text, produces a repository-owned query plan, and applies default matching against canonical index fields.
    - Default matching uses residual keyword matching against `keywords` and analyzed matching against both `searchText` and `content`, with `searchText` boosted above `content`.
    - The query-side canonical model and query plan contracts are introduced without reusing the ingestion `CanonicalDocument` CLR type directly.
    - The slice is runnable end to end through the existing host and Elasticsearch-backed infrastructure.
  - **Definition of Done**:
    - Code implemented across Domain, Services, Infrastructure, Host, and test layers with Onion Architecture boundaries preserved.
    - All code-writing work follows `./.github/instructions/documentation-pass.instructions.md` in full as a hard gate, including developer-level comments on every class, method, constructor, public parameter, and non-obvious property touched by the slice.
    - Targeted tests pass for the new query planning and mapping path.
    - Logging and error handling are added for the entrypoint, planner, and Elasticsearch mapping path.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Foundational documentation retains book-like narrative depth, defines technical terms, and includes examples or walkthrough support where the subject matter is conceptually dense.
    - Can execute end-to-end via: `dotnet build` plus targeted query-side tests, then run the existing host stack and perform a search through `QueryServiceHost`.
  - Summary: Added repository-owned query contracts in `src/UKHO.Search.Query`, including the canonical query model, normalized input snapshot, default contributions, execution directives, diagnostics, no-op rule evaluation result, and search-result contracts. Validation: `dotnet build`, `dotnet test` for `UKHO.Search.Query.Tests`. Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md` so contributors can see that QueryServiceHost now fronts a real planner and Elasticsearch-backed execution path.
  - [x] Task 1.1: Introduce repository-owned query plan contracts in the Domain layer - Completed
    - Summary: Added query-owned canonical model, input snapshot, default contribution, execution, diagnostics, rule-evaluation, and search-result contract types under `src/UKHO.Search.Query` without reusing the ingestion `CanonicalDocument` CLR type. Validation: `dotnet build`, `dotnet test test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj`. Wiki review result: reviewed query/runtime architecture guidance and updated `wiki/Home.md` plus `wiki/Solution-Architecture.md` to describe the real query pipeline.
    - [x] Step 1: Add query-side contract types under `src/UKHO.Search.Query` for the canonical query model, query input snapshot, default contributions, execution directives, and diagnostics.
    - [x] Step 2: Ensure the canonical query model mirrors the discovery half of `CanonicalDocument` using query-owned types and field names, not the ingestion document type itself.
    - [x] Step 3: Add developer-level comments and XML comments where applicable, following `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 1.2: Implement query normalization and default planning orchestration in the Services layer - Completed
    - Summary: Added `QueryTextNormalizer`, `QueryPlanService`, `QuerySearchService`, and no-op typed extraction/rule engine implementations so raw query text is normalized into a repository-owned default-only query plan with structured logging and failure handling. Validation: `dotnet build`, `dotnet test test/UKHO.Search.Services.Query.Tests/UKHO.Search.Services.Query.Tests.csproj`. Wiki review result: confirmed the updated architecture narrative now covers the real query planning flow and host-to-service boundary.
    - [x] Step 1: Add a normalization service that lowercases, trims, collapses repeated whitespace, tokenizes the query, and initializes residual tokens/text.
    - [x] Step 2: Add an application service in `src/UKHO.Search.Services.Query` that produces a query plan using default-only behavior for this first slice.
    - [x] Step 3: Keep typed extraction and rule evaluation injectable behind interfaces, but allow the first slice to use no-op implementations so the end-to-end path is provable before richer behavior lands.
    - [x] Step 4: Add structured logging around normalization, plan generation, and failure handling.
    - [x] Step 5: Apply the mandatory commenting and XML documentation standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 1.3: Implement Elasticsearch query mapping and runtime wiring in the Infrastructure layer - Completed
    - Summary: Added deterministic Elasticsearch JSON mapping and raw transport execution in `src/UKHO.Search.Infrastructure.Query`, including keyword matching on `keywords`, analyzed matching on `searchText` and `content`, the required `searchText` boost, DI registration, and invalid-response/empty-plan handling. Validation: `dotnet build`, `dotnet test test/UKHO.Search.Infrastructure.Query.Tests/UKHO.Search.Infrastructure.Query.Tests.csproj`. Wiki review result: updated `wiki/Solution-Architecture.md` so the query infrastructure row and host narrative describe query-plan mapping and execution responsibilities explicitly.
    - [x] Step 1: Add Infrastructure query services that translate the default-only query plan into Elasticsearch DSL targeting `keywords`, `searchText`, and `content`.
    - [x] Step 2: Implement the required boost so `searchText` is scored above `content`.
    - [x] Step 3: Register the real query services in `src/UKHO.Search.Infrastructure.Query/Injection/InjectionExtensions.cs`.
    - [x] Step 4: Add error handling for invalid Elasticsearch responses and null/empty query-plan cases.
    - [x] Step 5: Follow `./.github/instructions/documentation-pass.instructions.md` for all new or updated code.
  - [x] Task 1.4: Replace stub host wiring and prove the slice through the existing UI - Completed
    - Summary: Replaced the stub UI client registration in `src/Hosts/QueryServiceHost/Program.cs` with a thin host adapter that calls the repository-owned query search service, keeping query logic out of the host project. Validation: `dotnet build`, `dotnet test test/QueryServiceHost.Tests/QueryServiceHost.Tests.csproj`. Wiki review result: updated `wiki/Home.md` so contributors see that QueryServiceHost now uses the real pipeline rather than fixed sample data.
    - [x] Step 1: Update `src/Hosts/QueryServiceHost/Program.cs` to register the real query client path rather than the stub client.
    - [x] Step 2: Update or replace `IQueryUiSearchClient` implementations so the UI consumes real query results from the new services path.
    - [x] Step 3: Preserve host-only responsibilities in the host project and keep query logic out of the host.
    - [x] Step 4: Add developer-level comments throughout the host bootstrap changes per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 1.5: Add targeted tests and runnable verification for the bootstrap slice - Completed
    - Summary: Added targeted domain, services, infrastructure, and host composition tests covering contract defaults, normalization, default plan shape, Elasticsearch request mapping, response parsing, and removal of the stub runtime path. Manual verification path recorded below for local stack usage. Validation: `dotnet build`; `dotnet test` for `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, `UKHO.Search.Infrastructure.Query.Tests`, and `QueryServiceHost.Tests`. Wiki review result: reviewed contributor-facing runtime guidance and updated `wiki/Home.md` plus `wiki/Solution-Architecture.md`; no additional wiki page was required because those pages already own the current architecture entry path.
    - [x] Step 1: Add Domain and Services tests for normalization, token generation, residual initialization, and query plan shape.
    - [x] Step 2: Add Infrastructure tests for Elasticsearch DSL generation, including `searchText` boost over `content`.
    - [x] Step 3: Add Host-level or integration-style tests where feasible to verify the stub path is no longer the active runtime path.
    - [x] Step 4: Record the manual run path for searching through `QueryServiceHost` against the local stack.
  - **Files**:
    - `src/UKHO.Search.Query/*`: query plan contracts and normalization primitives.
    - `src/UKHO.Search.Services.Query/*`: orchestration services for query planning.
    - `src/UKHO.Search.Infrastructure.Query/*`: Elasticsearch mapping and runtime execution.
    - `src/Hosts/QueryServiceHost/Program.cs`: real runtime wiring.
    - `src/Hosts/QueryServiceHost/Services/*`: replacement for the stub path.
    - `test/UKHO.Search.Query.Tests/*`: domain/query contract and normalization tests.
    - `test/UKHO.Search.Services.Query.Tests/*`: service orchestration tests.
    - `test/UKHO.Search.Infrastructure.Query.Tests/*`: Elasticsearch mapping tests.
    - `test/QueryServiceHost.Tests/*`: host composition coverage.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Services.Query.Tests/UKHO.Search.Services.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Infrastructure.Query.Tests/UKHO.Search.Infrastructure.Query.Tests.csproj`
    - `dotnet test test/QueryServiceHost.Tests/QueryServiceHost.Tests.csproj`
    - Start the local stack and use the existing query UI in `QueryServiceHost` to issue a search.
  - **User Instructions**:
    - Ensure the local AppHost/Elasticsearch-backed environment is running before manual verification.
    - Use the existing QueryServiceHost search page rather than the previous stub-only assumptions.
  - **Execution Note**:
    - Manual run path recorded for this completed slice: start the local AppHost/services stack with Elasticsearch available, open `QueryServiceHost`, submit a query such as `latest solas`, and confirm the UI now returns Elasticsearch-backed hits instead of the former deterministic stub data. Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md`; no new wiki page was required because the existing architecture entry path was the correct place to explain the runtime change.
    - Follow-up fix: updated the Blazor query UI components to subscribe to `QueryUiState` change notifications and added explicit `@rendermode InteractiveServer` to the home page so completed searches replace the loading state in the visible panels. Validation: `dotnet build`, `dotnet test test/QueryServiceHost.Tests/QueryServiceHost.Tests.csproj`. Wiki review result: no additional wiki update was required because this change corrected a UI refresh defect without changing the documented architecture, workflows, or contributor guidance.

## Typed extraction slice
- [x] Work Item 2: Add Microsoft Recognizers-backed typed extraction through `ITypedQuerySignalExtractor` - Completed
  - **Purpose**: Introduce typed signal extraction now, while keeping the recognizer dependency isolated behind a repository-owned abstraction and mapping recognized years into `MajorVersion` in the query plan.
  - **Acceptance Criteria**:
    - `ITypedQuerySignalExtractor` exists and is used by the query planning path.
    - Microsoft Recognizers is integrated behind the abstraction, not leaked into the query plan contract.
    - Recognized years are preserved in the extracted temporal section and projected into `model.majorVersion`.
    - The slice is runnable end to end through the query host with a demonstrable query such as `2024` or `latest notice from 2024`.
  - **Definition of Done**:
    - Code implemented with Onion Architecture boundaries preserved.
    - All code-writing work follows `./.github/instructions/documentation-pass.instructions.md` in full as a hard gate.
    - Targeted tests pass for typed extraction, year recognition, and `MajorVersion` mapping.
    - Logging and error handling are added around recognizer invocation and normalized output shaping.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Can execute end-to-end via: targeted tests plus manual search through `QueryServiceHost` using a year-bearing query.
  - Summary: Added repository-owned extracted temporal and numeric query-signal contracts in `src/UKHO.Search.Query`, replaced the production no-op extractor with a Microsoft Recognizers-backed infrastructure adapter, projected recognized years into `model.majorVersion`, and hardened planner fallback logging so recognizer failures degrade to a deterministic default-only plan. Validation: `dotnet build`; `dotnet test` for `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests`; targeted Visual Studio test run covering those three projects (14 passed). Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md` so the contributor architecture narrative now explains the typed extraction stage, the `ITypedQuerySignalExtractor` boundary, and the `majorVersion` year projection.
  - [x] Task 2.1: Add typed extraction contracts and abstraction - Completed
    - Summary: Added repository-owned `QueryTemporalSignals`, `QueryTemporalDateSignal`, and `QueryNumericSignal` models, expanded `QueryExtractedSignals` to retain temporal and numeric output, and refreshed `ITypedQuerySignalExtractor` documentation so the inward contract explicitly describes repository-owned typed results. Validation: `dotnet build`, `dotnet test test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj`. Wiki review result: reviewed the query runtime guidance and updated `wiki/Home.md` plus `wiki/Solution-Architecture.md` because the contributor-facing architecture now includes a typed extraction stage.
    - [x] Step 1: Add repository-owned extracted signal models in `src/UKHO.Search.Query` for temporal and numeric outputs.
    - [x] Step 2: Add the `ITypedQuerySignalExtractor` abstraction in the appropriate inward layer.
    - [x] Step 3: Document the abstraction, methods, constructors, parameters, and non-obvious properties according to `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 2.2: Implement Microsoft Recognizers-backed extraction in Infrastructure - Completed
    - Summary: Added Microsoft Recognizers package references and the `MicrosoftRecognizersTypedQuerySignalExtractor` adapter in `src/UKHO.Search.Infrastructure.Query`, normalized recognizer output into repository-owned year, date, and number models, kept recognizer object graphs inside the adapter, and logged extracted signal counts without leaking external payload structure. Validation: `dotnet build`, `dotnet test test/UKHO.Search.Infrastructure.Query.Tests/UKHO.Search.Infrastructure.Query.Tests.csproj`. Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md` because the query infrastructure responsibilities now include typed extraction as well as Elasticsearch execution.
    - [x] Step 1: Add the required package and implementation in `src/UKHO.Search.Infrastructure.Query`.
    - [x] Step 2: Normalize recognizer outputs into repository-owned contracts, with no recognizer object graphs leaving the adapter.
    - [x] Step 3: Handle empty/no-match cases deterministically.
    - [x] Step 4: Add structured logging that explains what was recognized without leaking unnecessary internal detail.
    - [x] Step 5: Follow `./.github/instructions/documentation-pass.instructions.md` fully.
  - [x] Task 2.3: Integrate typed extraction into query planning and year projection - Completed
    - Summary: Updated `QueryPlanService` so typed extraction runs after normalization, recognized years seed the canonical `MajorVersion` intent before rule evaluation, residual defaults remain unchanged when typed signals are present, and recognizer failures are logged before the planner falls back to an empty extracted-signal contract. Validation: `dotnet build`, `dotnet test test/UKHO.Search.Services.Query.Tests/UKHO.Search.Services.Query.Tests.csproj`. Wiki review result: no additional wiki page beyond `wiki/Home.md` and `wiki/Solution-Architecture.md` was required because those pages already own the active query runtime narrative.
    - [x] Step 1: Update the query planning service to call `ITypedQuerySignalExtractor` after normalization.
    - [x] Step 2: Map recognized years into both `extracted.temporal.years` and `model.majorVersion`.
    - [x] Step 3: Ensure residual text/tokens and default matching behavior remain deterministic when typed signals are present.
    - [x] Step 4: Add error handling for recognizer failures so the planner degrades safely rather than crashing the host.
  - [x] Task 2.4: Add targeted tests and manual verification - Completed
    - Summary: Added query contract coverage for the extracted-signal defaults, planner tests for year projection and extraction-failure fallback, and infrastructure adapter tests for year, number, and empty-input handling. Manual verification path recorded below for local stack usage. Validation: `dotnet build`; `dotnet test` for `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests`; targeted Visual Studio test run covering the same three projects (14 passed). Wiki review result: reviewed contributor-facing query architecture guidance and updated `wiki/Home.md` plus `wiki/Solution-Architecture.md`; no new wiki page was required because the current architecture entry path remained the right place to explain the change.
    - [x] Step 1: Add unit tests for recognized-year normalization and `MajorVersion` projection.
    - [x] Step 2: Add integration-style tests proving the planner emits the expected query plan for `latest notice from 2024`.
    - [x] Step 3: Add Infrastructure tests for the recognizer adapter.
    - [x] Step 4: Record a manual run path demonstrating year-bearing queries through the host.
  - **Files**:
    - `src/UKHO.Search.Query/*`: extracted signal models and `ITypedQuerySignalExtractor` contract.
    - `src/UKHO.Search.Services.Query/*`: planner updates for typed extraction.
    - `src/UKHO.Search.Infrastructure.Query/*`: Microsoft Recognizers adapter and DI wiring.
    - `test/UKHO.Search.Query.Tests/*`: typed signal model tests.
    - `test/UKHO.Search.Services.Query.Tests/*`: planner integration tests with years.
    - `test/UKHO.Search.Infrastructure.Query.Tests/*`: adapter tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Services.Query.Tests/UKHO.Search.Services.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Infrastructure.Query.Tests/UKHO.Search.Infrastructure.Query.Tests.csproj`
    - Start the local stack and perform a query containing `2024` through `QueryServiceHost`.
  - **User Instructions**:
    - Use a query containing a recognizer-friendly year value, such as `latest notice from 2024`, during manual verification.
  - **Execution Note**:
    - Manual run path recorded for this completed slice: start the local AppHost/services stack with Elasticsearch available, open `QueryServiceHost`, submit `latest notice from 2024`, and confirm the search still returns Elasticsearch-backed results while the host logs include the structured typed-extraction message showing the recognized year count. Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md`; no new wiki page was required because the existing architecture entry path already teaches contributors where query planning and infrastructure responsibilities live.

## Rule-driven signal extraction slice
- [x] Work Item 3: Add flat query-rule loading and rule-driven `latest SOLAS` behavior - Completed
  - **Purpose**: Deliver the first real signal extraction behavior defined by the specification by loading flat global rules from `./rules/query`, applying them to the query plan, consuming matched phrases/tokens, and proving the `latest SOLAS` scenario end to end.
  - **Acceptance Criteria**:
    - Query rules are authored under `./rules/query` as a flat global structure.
    - The runtime loads rules from the `rules:query:*` configuration namespace using the existing loader-style approach without special nested-folder logic.
    - Matching rules can inspect input and extracted signals, mutate the canonical query model, emit concepts and sort hints, and consume phrases/tokens.
    - `latest SOLAS` produces rule-driven keyword expansions for `solas`, `maritime`, `safety`, and `msi`, plus sort directives for `majorVersion` then `minorVersion` descending.
    - Consumed phrases/tokens are not also reapplied by default matching.
  - **Definition of Done**:
    - Code implemented with Onion Architecture boundaries preserved.
    - All code-writing work follows `./.github/instructions/documentation-pass.instructions.md` in full as a hard gate.
    - Targeted tests pass for loader behavior, rule evaluation, consumption semantics, and the `latest SOLAS` plan shape.
    - Logging and error handling are added for rule loading, matched rule identifiers, and rule application outcomes.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Can execute end-to-end via: targeted tests plus manual search through `QueryServiceHost` using `latest SOLAS`.
  - Summary: Added repository-owned query-rule DTOs, validated runtime models, and rule-derived extracted signal contracts in `src/UKHO.Search.Query`; replaced the production no-op rule engine with a configuration-backed flat rule catalog and a real services-layer evaluator; updated the Elasticsearch mapper so canonical model keyword expansions execute even when residual defaults are fully consumed; and added flat `rules/query/sort-latest.json` plus `rules/query/concept-solas.json` for the end-to-end `latest SOLAS` path. Validation: `dotnet build`; targeted Visual Studio test run covering `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests` (21 passed). Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md` so contributors can see that query rules are now loaded from `rules/query` through `rules:query:*`, are global across search, and shape the query plan before defaults run.
  - [x] Task 3.1: Define query rule contracts and evaluation abstractions - Completed
    - Summary: Added query-rule wrapper/document DTOs, predicate/action DTOs, validated rule definitions, rule snapshot contracts, `IQueryRulesCatalog`, and rule-derived concept/sort-hint extracted signal models under `src/UKHO.Search.Query`. Validation: `dotnet build`, targeted test run including `UKHO.Search.Query.Tests`. Wiki review result: updated `wiki/Solution-Architecture.md` and `wiki/Home.md` because the domain-side query vocabulary now includes flat query-rule contracts and rule-derived extracted signals.
    - [x] Step 1: Add query-rule DTOs and validated runtime models aligned with the specification examples.
    - [x] Step 2: Keep the query-rule design similar in spirit to the ingestion rules DSL, but query-specific in paths and action groups.
    - [x] Step 3: Include support for `containsPhrase`, `model`, `concepts`, `sortHints`, and `consume` in this slice.
    - [x] Step 4: Apply the full commenting and XML documentation standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 3.2: Add flat `rules:query` loading and refresh-friendly catalog behavior - Completed
    - Summary: Added `QueryRuleConfigurationPath`, `AppConfigQueryRulesSource`, `QueryRulesLoader`, `QueryRulesValidator`, `QueryRulesCatalog`, and `AppConfigQueryRulesRefreshService` in `src/UKHO.Search.Infrastructure.Query`, keeping the loader flat, rooted at `rules:query`, startup-logged, and refresh-aware without adding nested-folder semantics. Validation: `dotnet build`, targeted test run including `UKHO.Search.Infrastructure.Query.Tests`. Wiki review result: updated `wiki/Solution-Architecture.md` and `wiki/Home.md` because the query infrastructure layer now owns a configuration-backed flat rule catalog and refresh loop in addition to typed extraction and Elasticsearch execution.
    - [x] Step 1: Implement a query rules source that enumerates the `rules:query` namespace from configuration.
    - [x] Step 2: Keep the loader behavior flat and global across search; do not add nested-folder logic.
    - [x] Step 3: Add startup logging for effective namespace and loaded rule counts.
    - [x] Step 4: Add refresh/reload-compatible catalog behavior similar in spirit to the ingestion rules path.
  - [x] Task 3.3: Implement the rules engine and planner integration - Completed
    - Summary: Added `ConfigurationQueryRuleEngine` in `src/UKHO.Search.Services.Query`, integrated it into DI, appended rule-derived concepts and sort hints onto the extracted signal contract, applied canonical model keyword mutations and consume directives, and ensured planner defaults now run only on the residual content left after rule consumption. Validation: `dotnet build`, targeted test run including `UKHO.Search.Services.Query.Tests`. Wiki review result: no new wiki page was required because the updated `wiki/Home.md` and `wiki/Solution-Architecture.md` already own the active query runtime and layering narrative.
    - [x] Step 1: Evaluate rules against query input and typed extracted signals.
    - [x] Step 2: Apply canonical query model mutations, concept expansions, sort hints, and consume directives.
    - [x] Step 3: Ensure defaults run only on the residual content after consumption.
    - [x] Step 4: Integrate the rules engine into the planner so the resulting query plan carries matched rule identifiers and derived execution behavior.
  - [x] Task 3.4: Add rule files and end-to-end verification content - Completed
    - Summary: Added flat `rules/query/sort-latest.json` and `rules/query/concept-solas.json` repository rule files. No AppHost or seeder code change was required because the existing additional-configuration seeding path already loads the repository `rules` directory beneath the shared `rules` prefix, which means these files naturally project to `rules:query:*` during local run mode. Validation: `dotnet build`, targeted test run including `UKHO.Search.Infrastructure.Query.Tests` and `UKHO.Search.Services.Query.Tests`. Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md` to state that query rules are flat, global, and configuration-backed.
    - [x] Step 1: Add flat query rules under `rules/query` for `sort-latest` and `concept-solas`.
    - [x] Step 2: Ensure those rules seed into the `rules:query` configuration namespace during local run mode.
    - [x] Step 3: Document how to inspect matched rules and expected plan behavior for `latest SOLAS`.
  - [x] Task 3.5: Add targeted tests and host verification - Completed
    - Summary: Added service-layer unit tests for predicate matching, phrase matching, concept expansion, and consumption semantics; infrastructure tests for flat `rules:query:*` loading and catalog reload; and planner plus Elasticsearch mapper tests for the full `latest SOLAS` plan shape and execution mapping. Manual verification path recorded below for local stack usage. Validation: `dotnet build`; targeted Visual Studio test run covering `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests` (21 passed). Wiki review result: reviewed contributor-facing query runtime guidance and updated `wiki/Home.md` plus `wiki/Solution-Architecture.md`; no additional wiki page was required because the current architecture entry path remained the correct place to explain the runtime change.
    - [x] Step 1: Add unit tests for predicate matching, phrase matching, concept expansion, and consumption semantics.
    - [x] Step 2: Add loader/catalog tests for the flat `rules:query:*` namespace.
    - [x] Step 3: Add planner/integration tests for the full `latest SOLAS` query plan.
    - [x] Step 4: Verify end to end via the running host and record the expected observable result.
  - **Files**:
    - `src/UKHO.Search.Query/*`: query rule contracts and validated models.
    - `src/UKHO.Search.Services.Query/*`: rule engine orchestration and planner integration.
    - `src/UKHO.Search.Infrastructure.Query/*`: configuration-backed rule loading and refresh support.
    - `rules/query/*.json`: flat global query rules.
    - `test/UKHO.Search.Query.Tests/*`: rule contract and matching tests.
    - `test/UKHO.Search.Services.Query.Tests/*`: planner/rule integration tests.
    - `test/UKHO.Search.Infrastructure.Query.Tests/*`: loader/catalog tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Services.Query.Tests/UKHO.Search.Services.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Infrastructure.Query.Tests/UKHO.Search.Infrastructure.Query.Tests.csproj`
    - Start the local stack and issue the query `latest SOLAS` through `QueryServiceHost`.
  - **User Instructions**:
    - Ensure the local configuration path has been seeded so `rules/query/*.json` are visible under the `rules:query` namespace.
  - **Execution Note**:
    - Manual run path recorded for this completed slice: start the local AppHost/services stack with Elasticsearch available, open `QueryServiceHost`, submit `latest SOLAS`, and confirm that the search still returns Elasticsearch-backed results while the query host logs show both matched rule identifiers and the applied rule ids, and while the request body is now driven by the rule-expanded `keywords` plus the descending `majorVersion` and `minorVersion` sorts rather than by residual defaults. Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md`; no new wiki page was required because those architecture entry pages already own the contributor-facing explanation of query runtime composition and rule loading.
    - Follow-up fix: corrected `ElasticsearchQueryExecutor` so model-only query plans are no longer skipped before transport execution. This fixes the `latest SOLAS` regression where rule-expanded `keywords` existed on the plan but zero residual defaults caused the executor to return an empty result set without querying Elasticsearch. Validation: `dotnet build`, `dotnet test test/UKHO.Search.Infrastructure.Query.Tests/UKHO.Search.Infrastructure.Query.Tests.csproj`. Wiki review result: no additional wiki update was required because this was a runtime defect fix inside the already documented query pipeline rather than a change to contributor-facing architecture or workflow guidance.

## Filters, boosts, and operational hardening slice
- [x] Work Item 4: Add explicit rule-driven `filters` and `boosts`, richer diagnostics, and refresh verification - Completed
  - **Purpose**: Complete the rule DSL shape promised by the specification by adding explicit `filters` and `boosts`, then harden the operational behavior with diagnostics and refresh verification.
  - **Acceptance Criteria**:
    - The rule DSL supports explicit `filters` and `boosts` sections from the first implementation, not as a deferred mapper-only concept.
    - The planner and Elasticsearch mapper can carry and apply those rule-driven directives.
    - Rule-loading diagnostics and refresh behavior are observable and testable.
    - The end-to-end runtime remains stable and demonstrable after these additions.
  - **Definition of Done**:
    - Code implemented with Onion Architecture boundaries preserved.
    - All code-writing work follows `./.github/instructions/documentation-pass.instructions.md` in full as a hard gate.
    - Targeted tests pass for filter and boost mapping, diagnostics, and refresh behavior.
    - Logging and error handling are added around rule refresh and DSL application.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Can execute end-to-end via: targeted tests plus manual host verification showing rule-driven filters/boosts are honored.
  - Summary: Added explicit filter and boost DTOs, validated runtime models, execution directives, and diagnostics in `src/UKHO.Search.Query`; taught the services-layer rule engine to carry rule-driven filters and boosts into execution directives with richer diagnostics; updated the Elasticsearch mapper and executor so filters become non-scoring filter clauses and boosts become explicit scoring clauses; and added a flat `rules/query/filter-notice-latest.json` example plus targeted validation, planner, mapper, and catalog tests. Validation: `dotnet build`; targeted Visual Studio test run covering `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests` (28 passed). Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md` so contributors can see that query rules now drive explicit filters, boosts, and richer execution diagnostics in addition to concept expansion and sort hints.
  - [x] Task 4.1: Extend the rule DSL and validated runtime model - Completed
    - Summary: Added raw rule DTOs for `filters` and `boosts`, validated runtime models for rule-driven filter and boost definitions, and repository-owned execution directive contracts in `src/UKHO.Search.Query`. Validation: `dotnet build`, targeted test run including `UKHO.Search.Query.Tests` and `UKHO.Search.Infrastructure.Query.Tests`. Wiki review result: updated `wiki/Solution-Architecture.md` and `wiki/Home.md` because the contributor-facing query rule vocabulary now includes explicit filter and boost semantics.
    - [x] Step 1: Add explicit `filters` and `boosts` sections to the query rule DTOs and validated runtime model.
    - [x] Step 2: Define repository-owned plan representations for those directives.
    - [x] Step 3: Document the semantics clearly in code comments and XML comments per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 4.2: Map rule-driven filters and boosts into Elasticsearch behavior - Completed
    - Summary: Updated `ConfigurationQueryRuleEngine`, `QueryPlanDiagnostics`, `ElasticsearchQueryMapper`, and `ElasticsearchQueryExecutor` so query plans now retain explicit filter and boost directives, non-scoring filters become bool `filter` clauses, explicit boosts become additional scoring clauses, and model-only or execution-directive-only plans still execute deterministically. Validation: `dotnet build`, targeted test run including `UKHO.Search.Services.Query.Tests` and `UKHO.Search.Infrastructure.Query.Tests`. Wiki review result: no additional wiki page beyond `wiki/Home.md` and `wiki/Solution-Architecture.md` was required because those pages already own the active query runtime narrative.
    - [x] Step 1: Update the planner to carry filter and boost directives into the query plan.
    - [x] Step 2: Update the Infrastructure mapper to translate them into Elasticsearch DSL.
    - [x] Step 3: Keep the mapping deterministic and testable.
    - [x] Step 4: Preserve the previously proven `latest SOLAS` behavior.
  - [x] Task 4.3: Add diagnostics and refresh verification - Completed
    - Summary: Added richer planning diagnostics for applied filters, boosts, sorts, and rule catalog load timing; extended the query rules catalog to expose load diagnostics; and added tests proving reload diagnostics stay aligned with refreshed snapshots. Manual verification path recorded below for local stack usage. Validation: `dotnet build`; targeted Visual Studio test run covering `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests` (28 passed). Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md`; no new wiki page was required because the current architecture entry path remained the correct place to explain the change.
    - [x] Step 1: Add structured diagnostics for matched rules, derived execution directives, and refresh outcomes.
    - [x] Step 2: Add tests for configuration reload or App Configuration refresh behavior in the query rules catalog.
    - [x] Step 3: Record a manual verification path that changes a query rule and demonstrates the refreshed behavior.
  - **Files**:
    - `src/UKHO.Search.Query/*`: DSL and plan extensions for filters and boosts.
    - `src/UKHO.Search.Services.Query/*`: planner support for filters/boosts.
    - `src/UKHO.Search.Infrastructure.Query/*`: Elasticsearch mapping and refresh diagnostics.
    - `test/UKHO.Search.Query.Tests/*`: DSL validation tests.
    - `test/UKHO.Search.Services.Query.Tests/*`: planner tests.
    - `test/UKHO.Search.Infrastructure.Query.Tests/*`: mapper and refresh tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 3.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Services.Query.Tests/UKHO.Search.Services.Query.Tests.csproj`
    - `dotnet test test/UKHO.Search.Infrastructure.Query.Tests/UKHO.Search.Infrastructure.Query.Tests.csproj`
    - Start the local stack, modify a flat query rule, and verify refreshed behavior through `QueryServiceHost`.
  - **User Instructions**:
    - Keep the flat `rules/query` authoring model; do not introduce nested rule folders for this work package.
  - **Execution Note**:
    - Manual run path recorded for this completed slice: start the local AppHost/services stack with Elasticsearch available, open `QueryServiceHost`, submit a query such as `latest notice`, confirm the host logs show the matched rule id together with applied filter and boost diagnostics, then edit `rules/query/filter-notice-latest.json` to change the category filter or boost weight, allow configuration refresh to occur, rerun the same query, and confirm the refreshed behavior is reflected in both the query logs and the resulting Elasticsearch request body. Wiki review result: updated `wiki/Home.md` and `wiki/Solution-Architecture.md`; no new wiki page was required because those architecture entry pages already own the contributor-facing explanation of query runtime composition and rule loading.

## Final mandatory wiki review and package closure
- [x] Work Item 5: Record the final wiki review/update outcome for the full work package - Completed
  - **Purpose**: Satisfy the repository’s mandatory wiki-maintenance gate for the entire work package and explicitly record the documentation outcome.
  - **Acceptance Criteria**:
    - The relevant wiki pages, glossary entries, or repository guidance pages have been reviewed against the implemented query-side behavior, architecture, workflow, terminology, and contributor guidance.
    - Any required wiki or repository guidance updates are completed.
    - If no wiki update is required for any reviewed page, the no-change result is explicitly recorded with a concrete explanation of what was reviewed and why it remained sufficient.
  - **Definition of Done**:
    - Wiki review performed in accordance with `./.github/instructions/wiki.instructions.md`.
    - The final execution record states which wiki or repository guidance pages were updated, created, retired, or why no change was needed.
    - For architecture, runtime, workflow, setup, and extension guidance, any updated documentation preserves long-form, book-like narrative depth, defines technical terms clearly, and includes examples or walkthroughs where they materially improve comprehension.
    - Can execute end-to-end via: review of the recorded implementation results and documentation changes.
  - Summary: Completed the final mandatory wiki review for the query-signal work package, confirmed the core architecture pages already carried the updated query runtime narrative, and expanded `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Architecture-Walkthrough.md` so contributors can now follow the typed-extraction, flat query-rule, local verification, and runtime-tracing story end to end. Validation: `dotnet build`; targeted Visual Studio test run covering `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests` (28 passed). Wiki review result: updated `wiki/Home.md`, `wiki/Solution-Architecture.md`, `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Architecture-Walkthrough.md`; no pages were retired or split because the existing reading path remained structurally correct once the missing query-side narrative depth was added.
  - [x] Task 5.1: Review contributor-facing documentation paths - Completed
    - Summary: Reviewed the current wiki reading path for query-side architecture, flat rule loading, typed extraction, and local verification guidance, then updated the glossary, setup, and architecture walkthrough pages where contributor understanding still had gaps. Validation: `dotnet build`, targeted test run including `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests`. Wiki review result: updated `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Architecture-Walkthrough.md`; retained `wiki/Home.md` and `wiki/Solution-Architecture.md` as the primary entry path because their overall structure remained correct.
    - [x] Step 1: Review current wiki and repository guidance pages that explain query-side architecture, search behavior, rule loading, and local setup/verification workflows.
    - [x] Step 2: Decide whether each page needs updating, splitting, replacing, or no change.
    - [x] Step 3: Apply any required updates in the appropriate documentation location.
  - [x] Task 5.2: Record the explicit outcome - Completed
    - Summary: Added the final work-package record describing exactly which wiki pages were updated and why no retire/split action was needed. Validation: `dotnet build`, targeted test run including `UKHO.Search.Query.Tests`, `UKHO.Search.Services.Query.Tests`, and `UKHO.Search.Infrastructure.Query.Tests`. Wiki review result: explicitly recorded that `wiki/Home.md`, `wiki/Solution-Architecture.md`, `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Architecture-Walkthrough.md` were updated, and that no wiki pages were retired, split, or left stale after review.
    - [x] Step 1: Add a final work-package execution note stating exactly which pages were updated, created, retired, or intentionally left unchanged.
    - [x] Step 2: Ensure the wording is explicit and concrete rather than generic.
  - **Files**:
    - `wiki/*` and/or repository guidance files as required by the review.
    - `docs/095-signal-extraction-rules/*`: final work-package record updates if needed.
  - **Work Item Dependencies**:
    - Depends on Work Items 1 through 4.
  - **Run / Verification Instructions**:
    - Review the final implementation record and changed wiki/repository guidance pages.
  - **User Instructions**:
    - None.
  - **Execution Note**:
    - Final wiki review outcome for the full work package: reviewed the contributor-facing query documentation path across `wiki/Home.md`, `wiki/Solution-Architecture.md`, `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Architecture-Walkthrough.md`. Updated all five pages so the current-state narrative now explicitly explains the repository-owned query plan, `ITypedQuerySignalExtractor`, flat `rules/query` loading into `rules:query:*`, rule-driven concepts/sorts/filters/boosts, and the local setup and verification path for query-rule work. No wiki pages were created, retired, renamed, or split because the existing information architecture already provided the correct reading path once the missing narrative depth and terminology coverage were added.

## Summary / key considerations

- Start by proving a real query path through the existing host before introducing richer query semantics. This keeps the work package vertical-slice driven rather than building isolated models with no runnable path.
- Keep the query-side contracts repository-owned and provider-independent. The canonical query model should mirror the discovery half of `CanonicalDocument`, but not import ingestion-only fields or reuse the ingestion document type directly.
- Treat Microsoft Recognizers as an adapter concern. The important repository contract is the normalized extracted signal model and the `ITypedQuerySignalExtractor` abstraction, not the external library’s object graph.
- Preserve the explicit repository decision that query rules are flat and global under `./rules/query`, producing the `rules:query:*` namespace without any nested-folder behavior.
- Make consumption semantics a first-class concern. Without consumption, rule-recognized terms such as `latest` will pollute the default search path and reduce the value of the signal extraction model.
- Keep the documentation-pass rules and wiki-maintenance workflow visible throughout implementation. In this repository they are completion gates, not optional polish.
- Use targeted builds and targeted test projects for this work package rather than the full test suite, while still ensuring each slice is demonstrably runnable end to end.
