# Next-Gen Arc 09 Work Packages: QueryServiceHost End-User Search Experience

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 09 builds the production end-user search experience in `QueryServiceHost`. It consumes Arc 03 browser-host foundations and stable end-user search APIs from Arc 04. It must not reuse the current QueryServiceHost developer workspace as product design, and it must not expose developer-only diagnostics.

## Numbering

Arc 09 work packages use WP260-WP266.

Reserved buffer before Arc 10: WP267-WP279.

## Evidence Checked

- QueryServiceHost is an authenticated Blazor/Radzen developer workspace, not a stable product API or end-user UI: [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs), [../../src/Hosts/QueryServiceHost/State/QueryUiState.cs](../../src/Hosts/QueryServiceHost/State/QueryUiState.cs), and [../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs](../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs).
- Query service runtime exists in [../../src/UKHO.Search.Services.Query/Execution/QuerySearchService.cs](../../src/UKHO.Search.Services.Query/Execution/QuerySearchService.cs), [../../src/UKHO.Search.Services.Query/Planning/QueryPlanService.cs](../../src/UKHO.Search.Services.Query/Planning/QueryPlanService.cs), [../../src/UKHO.Search.Infrastructure.Query/Search/ElasticsearchQueryExecutor.cs](../../src/UKHO.Search.Infrastructure.Query/Search/ElasticsearchQueryExecutor.cs), and [../../src/UKHO.Search.Infrastructure.Query/Search/ElasticsearchQueryMapper.cs](../../src/UKHO.Search.Infrastructure.Query/Search/ElasticsearchQueryMapper.cs).
- Current facets are not implemented in the real query path; [../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs](../../src/Hosts/QueryServiceHost/Services/QueryUiSearchClient.cs) logs that selected facets are not translated and returns empty facets.

## WP260: Specify Product Search Experience And API Dependencies

Scope:
- Define end-user search journey, route, page structure, default state, search submission, results, facets, filters, sorts, result detail, empty states, errors, and auth behavior.

Requirements carried:
- Consume stable end-user APIs, not host-local Blazor state.
- Do not expose generated-plan editor, raw Elasticsearch request, query-rule drafts, diagnostics trace, or corpus regression.
- Auth follows Arc 02 end-user policy.

Validation anchors:
- UX review against Arc 04 API contracts before build work starts.

## WP261: Build Search Input And Results Experience

Scope:
- Implement search input, submission, loading, result list, hit count, empty state, and error handling.

Requirements carried:
- First screen is the usable search experience.
- Result display is tailored for end users and does not expose raw developer JSON.
- Use Arc 03 Blazor Blueprint and host component conventions.

Validation anchors:
- Component and Playwright tests for query submission, results, empty results, loading, auth error, and API error.

## WP262: Build Facet, Filter, And Sort Interaction

Scope:
- Add product-facing facets, selected filters/chips, sort controls, and search state persistence where appropriate.

Requirements carried:
- Backend support from Arc 04 is required because current query path does not translate selected facets.
- UI renders only filters/sorts supported by API metadata.

Validation anchors:
- Tests for selecting/clearing filters, sort changes, no facets, state persistence, and unsupported filter responses.

## WP263: Build Result Detail And Product-Safe Explain Views

Scope:
- Implement result detail, metadata, snippets/matched fields where product-appropriate, and any product-safe explanation behavior.

Requirements carried:
- Result explanation needs explicit backend capability, not only selected-hit UI state.
- Raw hit JSON and developer-only internals do not leak to end users.

Validation anchors:
- Tests for detail load, not found, restricted result, and missing optional data.

## WP264: Add Search Metadata, Suggestions, And Readiness States

Scope:
- Integrate supported query feature metadata, environment/readiness, filters/sorts metadata, and suggestions if product scope includes them.

Requirements carried:
- Health/readiness/environment metadata are needed for startup and diagnostics.
- Query suggestions or query understanding diagnostics are product-visible only by explicit decision.

Validation anchors:
- Tests for readiness degraded, metadata failure, unsupported feature, and suggestions if implemented.

## WP265: Harden End-User UX, Accessibility, And Performance

Scope:
- Complete responsive behavior, accessibility, keyboard navigation, loading behavior, performance, text fit, and visual polish.

Requirements carried:
- The product UI is ergonomic for repeated search workflows and distinct from developer tooling.

Validation anchors:
- Frontend lint/typecheck/build/test and Playwright desktop/mobile screenshots/accessibility smoke.

## WP266: Document End-User Search Behavior And Boundaries

Scope:
- Document product search behavior, API dependencies, auth policy, filters/sorts, result detail, and exclusions.

Requirements carried:
- Query-rule workbench tunes semantics; product search presents tuned search.
- Developer-only plan JSON, raw request JSON, draft rules, and corpus comparison stay out of product UI.

Validation anchors:
- Documentation review against Arc 04 contracts and implemented UI behavior.

## Arc Requirement Cross-Check

- Production end-user search over stable APIs: WP260-WP266.
- Search execution, result display, facets, filters, sorting, result details: WP261-WP263.
- Query metadata, readiness, optional suggestions: WP264.
- Appropriate end-user auth policy: WP260.
- Do not inherit QueryServiceHost developer workspace layout or diagnostics: WP260-WP266.
- Responsive, accessible, Blazor Blueprint-based quality: WP261-WP265.

## Handoff To Arc 10

Arc 10 can retire QueryServiceHost and other legacy UI surfaces only after this product search experience and developer replacements are proven.