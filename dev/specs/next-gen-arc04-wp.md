# Next-Gen Arc 04 Work Packages: Query APIs And Query-Rule Diagnostics Foundation

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 04 formalizes query-side backend contracts and makes query-rule behavior explainable. It exposes raw query planning, supplied-plan execution, final search-engine diagnostics, structured query interpretation traces, draft query-rule validation/evaluation, current-versus-draft comparisons, and query corpus regression support.

The React UI must present backend semantics. It must not infer query-rule behavior from final `QueryPlan` JSON or reimplement the rule engine in the browser.

## Numbering

Arc 04 work packages use WP160-WP166.

Reserved buffer before Arc 05: WP167-WP179.

## Evidence Checked

- Query host wires Blazor UI state to service-layer query execution but does not map search endpoints: [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs), [../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs](../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs), and [../../src/Hosts/QueryServiceHost/State/QueryUiState.cs](../../src/Hosts/QueryServiceHost/State/QueryUiState.cs).
- Host-local DTOs live under [../../src/Hosts/QueryServiceHost/Models/](../../src/Hosts/QueryServiceHost/Models/).
- Query service pipeline exists in [../../src/UKHO.Search.Services.Query/Planning/QueryPlanService.cs](../../src/UKHO.Search.Services.Query/Planning/QueryPlanService.cs), [../../src/UKHO.Search.Services.Query/Rules/ConfigurationQueryRuleEngine.cs](../../src/UKHO.Search.Services.Query/Rules/ConfigurationQueryRuleEngine.cs), and [../../src/UKHO.Search.Services.Query/Execution/QuerySearchService.cs](../../src/UKHO.Search.Services.Query/Execution/QuerySearchService.cs).
- Query plan contracts live in [../../src/UKHO.Search.Query/Models/QueryPlan.cs](../../src/UKHO.Search.Query/Models/QueryPlan.cs).
- Microsoft Recognizers are already behind `ITypedQuerySignalExtractor`: [../../src/UKHO.Search.Infrastructure.Query/TypedExtraction/MicrosoftRecognizersTypedQuerySignalExtractor.cs](../../src/UKHO.Search.Infrastructure.Query/TypedExtraction/MicrosoftRecognizersTypedQuerySignalExtractor.cs).
- Query rules load from the flat `rules:query:*` namespace through [../../src/UKHO.Search.Infrastructure.Query/Rules/QueryRuleConfigurationPath.cs](../../src/UKHO.Search.Infrastructure.Query/Rules/QueryRuleConfigurationPath.cs), [../../src/UKHO.Search.Infrastructure.Query/Rules/QueryRulesCatalog.cs](../../src/UKHO.Search.Infrastructure.Query/Rules/QueryRulesCatalog.cs), and [../../src/UKHO.Search.Infrastructure.Query/Rules/QueryRulesValidator.cs](../../src/UKHO.Search.Infrastructure.Query/Rules/QueryRulesValidator.cs).

## WP160: Define Stable Query API Contracts

Scope:
- Define explicit HTTP request/response models for end-user-compatible query execution and developer query planning/execution.
- Separate public search results from developer diagnostics and avoid reusing host-local Blazor DTOs as durable contracts.

Requirements carried:
- Execute raw search.
- Generate or return a query plan.
- Execute a supplied query plan for developer diagnostics.
- Return final search-engine request diagnostics where allowed.
- Return facets, apply facet/filter selections, describe supported filters/sorts, and return result detail/explain data.
- Expose health/readiness/version/environment metadata for UI startup.

Validation anchors:
- Query API contract tests, OpenAPI tests, and existing query service tests.

## WP161: Implement Query API Endpoints Over Existing Services

Scope:
- Add minimal API endpoints in the Arc 02-selected host for raw search, plan generation, and supplied-plan execution.

Requirements carried:
- QueryServiceHost behavior should move behind stable HTTP APIs before React replaces Razor components.
- Generated-plan editor flow needs explicit endpoints.
- Edited plan validation must happen server-side or against shared backend contracts.
- Facets currently log as unsupported and response facets are empty; the API must implement or explicitly report unsupported facet behavior until complete.

Validation anchors:
- API integration tests using `IQuerySearchService`/`IQueryPlanService` and tests for unsupported/implemented facet behavior.

## WP162: Add Structured Query Interpretation Trace Model

Scope:
- Extend query planning/rule evaluation to emit a structured trace that records each stage and each rule predicate/action outcome.

Requirements carried:
- Trace raw input, normalized text, cleaned text, tokens, typed extracted signals, seed model, catalog metadata, per-rule predicate evaluation, resolved path values, match/no-match reasons, matched value, action outputs, consumed tokens/phrases, residual tokens/text, default contributions, final plan, Elasticsearch request JSON, warnings, timings, and optional results.
- Include rules that did not match.
- Record field-level deltas caused by matched rules.
- Keep trace emission in backend services.

Validation anchors:
- Unit tests for predicate trace output, no-match reasons, action application, residual consumption, and deltas.

## WP163: Add Query Rule List, Fetch, And Validation APIs

Scope:
- Expose query-rule discovery, authored/effective rule fetch, and draft validation APIs.

Requirements carried:
- List effective query rules with id, title, enabled state, description, validation state, and loaded snapshot metadata.
- Get authored/effective query-rule documents by id.
- Validate draft query-rule documents through backend validation.
- Preserve flat `rules:query:*` namespace behavior.
- Do not enable save/promote until authorization, audit, conflict handling, and App Configuration update behavior are defined.

Validation anchors:
- QueryRulesValidator tests and API tests for valid/invalid drafts.

## WP164: Add Draft Evaluation And Current-Versus-Draft Comparison APIs

Scope:
- Evaluate supplied draft query rules without saving and compare them against the current catalog for one raw query.

Requirements carried:
- Return matched rules, non-matched rules, predicate traces, action outputs, residual changes, generated plan, request JSON, optional result data, and warnings.
- Compare model fields, filters, boosts, sorts, residual defaults, request JSON, result count, top-result order, matched fields, warnings, timings, and score-shaping changes where available.

Validation anchors:
- Tests with mutable query-rule sources and fixed query executor results.

## WP165: Add Query Corpus Regression Foundation

Scope:
- Define representative query corpus storage and comparison APIs for draft query-rule regression.

Requirements carried:
- Query rules are global; a fix for one phrase can break another.
- Developers need named query suites before promotion.
- Corpus comparison should report per-query and aggregate impact, warnings, timing, result count changes, top-order changes, request changes, and failures.

Validation anchors:
- Unit/API tests for corpus execution and report summarization.

## WP166: Harden Query API Security, Diagnostics, And Documentation

Scope:
- Apply Arc 02 auth/authorization decisions and document operational behavior.

Requirements carried:
- Developer-only plan JSON, raw Elasticsearch requests, draft rule controls, and trace details must not leak into end-user interfaces.
- Request diagnostics may be restricted by deployment or role.
- API docs must describe health/readiness, supported features, warnings, and known unsupported behavior.

Validation anchors:
- Endpoint authorization tests, diagnostics exposure tests, and documentation review.

## Arc Requirement Cross-Check

- Stable search/query HTTP APIs replacing Blazor state: WP160-WP161.
- Raw search, plan generation, supplied-plan execution, diagnostics, health/readiness: WP160-WP161, WP166.
- Facets/filter selections, supported filters/sorts, and result detail/explain contracts: WP160-WP161.
- Structured trace from normalization through typed extraction, rule evaluation, residual defaults, final plan, request mapping, and optional results: WP162.
- Per-rule predicate resolution, no-match reasons, action outputs, consumed tokens/phrases, and field-level deltas: WP162.
- Query-rule list/get/validate APIs: WP163.
- Draft evaluation and current-versus-draft comparison: WP164.
- Query corpus regression before promotion: WP165.
- Microsoft Recognizers remain behind `ITypedQuerySignalExtractor`: WP162.
- Developer diagnostics separate from end-user search: WP160, WP166.

## Handoff To Arc 07 And Arc 09

Arc 07 builds the React developer query-rule workbench over WP162-WP165. Arc 09 builds end-user search over non-diagnostic query contracts from WP160-WP161 and WP166.