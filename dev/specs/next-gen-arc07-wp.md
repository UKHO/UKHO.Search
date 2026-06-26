# Next-Gen Arc 07 Work Packages: React Developer Query-Rule Workbench

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 07 builds the React developer workspace for query-rule inspection, draft editing, backend validation, current-versus-draft comparison, and query corpus regression. It consumes Arc 03 frontend foundations and Arc 04 query diagnostics APIs.

## Numbering

Arc 07 work packages use WP220-WP226.

Reserved buffer before Arc 08: WP227-WP239.

## Evidence Checked

- Query UI state today is Blazor-local in [../../src/Hosts/QueryServiceHost/State/QueryUiState.cs](../../src/Hosts/QueryServiceHost/State/QueryUiState.cs).
- Query UI service adapter projects service-layer results to host-local DTOs in [../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs](../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs).
- Query planning and rule application are backend-owned in [../../src/UKHO.Search.Services.Query/Planning/QueryPlanService.cs](../../src/UKHO.Search.Services.Query/Planning/QueryPlanService.cs) and [../../src/UKHO.Search.Services.Query/Rules/ConfigurationQueryRuleEngine.cs](../../src/UKHO.Search.Services.Query/Rules/ConfigurationQueryRuleEngine.cs).
- Existing diagnostics are high-level in [../../src/UKHO.Search.Query/Models/QueryPlanDiagnostics.cs](../../src/UKHO.Search.Query/Models/QueryPlanDiagnostics.cs); Arc 04 supplies the structured trace this UI consumes.

## WP220: Build Query-Rule Workbench Shell And Data Flow

Scope:
- Add the query-rule workbench route, layout, API hooks, loading/error states, and navigation within the Arc 03 React shell.

Requirements carried:
- The workbench is a developer query-rule lab, not an end-user search product.
- It consumes Arc 04 APIs and never reimplements rule evaluation in the browser.
- It does not port current QueryServiceHost panels or Workbench contribution machinery.

Validation anchors:
- Frontend component tests and Playwright smoke with mocked backend responses.

## WP221: Build Query Interpretation Trace Viewer

Scope:
- Present the structured query trace produced by Arc 04.

Requirements carried:
- Show raw input, normalized text, tokens, typed extracted signals, seed model, per-rule predicate evaluation, matched and non-matched rules, action outputs, consumed tokens/phrases, residual defaults, final query plan, Elasticsearch request JSON, result data, warnings, and timings.
- Explain non-matched rules using resolved path values or predicate reasons.
- Use progressive disclosure so diagnostics remain scannable.

Validation anchors:
- Tests for full trace, no-rule match, malformed trace response, warning, and empty-result states.

## WP222: Build Draft Rule Editor And Backend Validation UI

Scope:
- Implement rule list/fetch/editor/validation behavior using Arc 04 APIs and Arc 03 editor foundation.

Requirements carried:
- Developers can open a rule, edit JSON, validate it with backend validator, and inspect validation errors.
- Draft editing does not immediately save to App Configuration.
- Save/promote controls appear only if backend promotion is enabled with auth, audit, conflict handling, and App Configuration update behavior.

Validation anchors:
- Tests for valid draft, invalid draft, unsaved changes, server validation errors, and save unavailable.

## WP223: Build Current-Versus-Draft Comparison UI

Scope:
- Display Arc 04 comparison reports for one raw query and draft rules.

Requirements carried:
- Compare canonical model, filters, boosts, sorts, residual defaults, request JSON differences, result count, top-result ordering, matched fields, warnings, timing, and score-shaping changes where available.
- Use backend-produced comparison data rather than browser-computed semantic diffs.

Validation anchors:
- Tests for changed, unchanged, warning, backend error, and large-diff states.

## WP224: Build Query Corpus Regression UI

Scope:
- Add UI for named representative query suites and current-versus-draft regression results.

Requirements carried:
- Draft query-rule changes must be checked against representative searches before promotion.
- Show per-query and aggregate model/request/result-order/warning/timing changes, failures, and corpus governance notes.

Validation anchors:
- Tests for empty corpus, running state, mixed outcomes, failed query, and failed corpus run.

## WP225: Build Query Rule Promotion Flow If Enabled

Scope:
- Add save/promote behavior only when Arc 04 and Arc 02 make it safe.

Requirements carried:
- Save-back requires authorization, audit, validation, conflict handling, App Configuration update behavior, and refresh behavior.
- A useful first version may support diagnostics and draft comparison before promotion.

Validation anchors:
- Tests for unauthorized, invalid, conflict, successful promotion, and audit metadata if enabled.

## WP226: Document Query-Rule Tuning Workflow

Scope:
- Document how developers use the workbench to diagnose query interpretation, edit drafts, compare changes, and run corpus regression.

Requirements carried:
- Documentation distinguishes developer query-rule workbench from end-user search.
- Documentation states that backend APIs own trace semantics and the UI presents them.

Validation anchors:
- Documentation review against Arc 04 contracts and implemented UI.

## Arc Requirement Cross-Check

- React developer query-rule workbench over Arc 04 APIs: WP220.
- Full interpretation pipeline display and matched/non-matched rules: WP221.
- Draft editing and backend validation without immediate save-back: WP222.
- Current-versus-draft comparison for one query: WP223.
- Named query corpus regression: WP224.
- Save/promote only with auth, audit, validation, conflict, and App Configuration behavior: WP225.
- Developer-only diagnostics separate from end-user search: WP220, WP226.

## Handoff To Arc 09

Arc 09 builds the end-user search product separately and must not inherit this diagnostic layout as product UX.