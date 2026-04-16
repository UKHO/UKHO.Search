# Query pipeline

This page is the narrative entry point for the query-side chapter.

Use it when you want the current repository view of how free-form user text becomes a repository-owned query plan, why the runtime is staged the way it is, and which pages to read next when you need rule-authoring, mapping, or code-tracing detail.

## Reading path

Read this chapter in the order that matches the kind of question you are trying to answer.

- Start here on [Query pipeline](Query-Pipeline) when you need the mental model for the read side as a whole.
- Continue to [Query signal extraction rules](Query-Signal-Extraction-Rules) when you need the deep explanation of how `rules/query/*.json` shape canonical intent, execution directives, and residual consumption.
- Continue to [Query walkthrough](Query-Walkthrough) when you want a code-oriented trace through `QueryServiceHost`, the planner, the rule catalog, and Elasticsearch execution.
- Read [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping) when you need the detailed contract-level explanation of `QueryInputSnapshot`, `QueryExtractedSignals`, the canonical query model, and deterministic request mapping.
- Keep [Appendix: query rule syntax quick reference](Appendix-Query-Rule-Syntax-Quick-Reference) nearby when you need a shorter lookup companion while authoring or reviewing query-rule JSON.

## What the query runtime is trying to achieve

The query runtime exists to interpret user intent without letting the host UI build search-engine requests directly.

That design choice matters because the repository does not want search semantics to be spread across Razor components, host adapters, infrastructure helpers, and ad hoc Elasticsearch JSON fragments. Instead, the query side follows the same broad architectural instinct that the ingestion side follows: keep the long-lived repository concepts inward, let hosts stay focused on composition and UI, and make the transition from user input to search execution explicit enough that contributors can reason about it.

In practical terms, the runtime tries to do five things well:

1. preserve the user input in a deterministic normalized shape
2. derive typed signals such as years before rule logic runs
3. let query rules add repository-owned intent and execution directives
4. fall back to residual default matching only for the text that rules did not already account for
5. execute against the canonical Elasticsearch index and parse the result back into repository-owned result contracts

The result is not just a thin search box over Elasticsearch. It is a repository-owned interpretation pipeline. That distinction is important because it tells contributors where to put future behavior. Search semantics belong in the query-side planner, rules, and mapping layers, not in host-local UI code.

## Why the query side is staged

At first glance, it might seem simpler to take raw text from `QueryServiceHost`, turn it straight into one search-engine request, and let Elasticsearch do the rest. The repository deliberately does not do that.

The staged design exists because user-entered text usually carries several different kinds of meaning at once. Some of that meaning is structural. A recognized year such as `2024` is better represented as a typed value than as an unstructured token. Some meaning is repository-specific. A word such as `solas` may stand for a known concept that should expand into canonical keywords and sort intent. Some meaning is still ordinary residual text and should remain available for analyzed matching against the canonical search fields. Putting all of those jobs into one direct request-builder step would blur together interpretation, policy, and execution.

By separating normalization, typed extraction, rules, residual defaults, and execution mapping, the repository keeps each stage narrow enough to understand. The planner can explain what the query means before the executor decides how Elasticsearch should express that meaning. That makes the runtime easier to test, easier to document, and easier to evolve without turning the host into a search-policy owner.

## Current runtime map

The diagram below should be read from left to right. It starts with the host-local search request and ends with a parsed repository-owned result. Each box represents a stage boundary where the runtime changes the shape of the data or adds a new layer of interpretation.

```mermaid
flowchart LR
    Ui[QueryServiceHost UI request] --> Host[QueryUiSearchClient]
    Host --> SearchService[QuerySearchService]
    SearchService --> Normalize[Normalize raw text into QueryInputSnapshot]
    Normalize --> Extract[Typed extraction via ITypedQuerySignalExtractor]
    Extract --> Rules[Evaluate flat query rules]
    Rules --> Defaults[Build residual default contributions]
    Defaults --> Map[Map QueryPlan to Elasticsearch JSON]
    Map --> Execute[Execute against canonical index]
    Execute --> Parse[Parse hits into QuerySearchResult]
```

The important thing to notice is that the runtime does not jump directly from the UI to Elasticsearch. The host forwards user input into repository-owned services, the planner produces a `QueryPlan`, and only then does the infrastructure layer translate that plan into a deterministic request body.

That boundary remains important even though the current `QueryServiceHost` screen now exposes the generated plan much more directly than it did earlier in the repository history. The home page is now a single-screen developer workspace rather than a nested three-column results-and-details page. A contributor enters raw text in the top command bar, the raw-query path still runs through `QueryUiSearchClient` into `IQuerySearchService`, and the resulting `QueryPlan` is projected back into the host as formatted JSON shown in Monaco beside the flat results list. The left insight column now derives extracted signals and a **transformation trace** from that same repository-owned plan, the right diagnostics column shows the final Elasticsearch request JSON plus execution warnings and timings returned by the inward pipeline, and selected-result detail lives in a collapsible bottom drawer. In other words, the host has become much better at *showing* repository-owned runtime artifacts, but it still does not *own* planning or request mapping.

That distinction now matters even more because the host can also execute a caller-supplied plan directly from the Monaco pane. The generated plan produced by the raw-query path becomes a **generated-plan baseline**, meaning the last repository-owned plan produced from raw text. `QueryUiState` keeps that baseline separately from the current editable editor contents. When a contributor clicks the pane-level `Search` button, the host first validates that the editor contains valid `QueryPlan` JSON, then calls the supplied-plan path on the application service instead of regenerating a new plan from the raw-query bar. The host is therefore now capable of round-tripping a visible plan back into execution, but the architectural rule still holds: validation and UI workflow live in the host, while execution still flows inward through repository-owned query contracts and services.

## Stage-by-stage explanation

### 1. Raw query ingress stays thin in the host

`QueryServiceHost` owns the interactive Blazor surface and the authentication-aware browser host composition. It does not own query semantics.

The practical entry point is the host adapter `QueryUiSearchClient`, which receives the UI request and forwards the query text into `IQuerySearchService`. That is an architectural boundary, not just a convenience wrapper. It means the host can stay focused on user interaction while the actual search meaning lives inward in `UKHO.Search.Services.Query`, `UKHO.Search.Query`, and `UKHO.Search.Infrastructure.Query`.

The current host shell makes that architectural rule visible in day-to-day use. `Home.razor` now lays out a top command bar, a left insight column, a centre split workspace, a right diagnostics column, and a bottom detail drawer host. The centre split is the most important part for query contributors because it keeps the generated plan and the results visible together. `QueryUiState` stores both the latest generated-plan baseline and a writable working copy for Monaco, but those are still host-local projections of repository-owned runtime data. The surrounding diagnostics now complete that story: the left column projects extracted signals and a staged transformation trace from `QueryPlan`, while the right column projects the final Elasticsearch request JSON, search-engine timing, wall-clock timing, and non-blocking warnings from `QuerySearchResult`. The host can therefore teach contributors how one run behaved without becoming a second planner.

The edited-plan slice extends that same rule rather than breaking it. `QueryPlanPanel.razor` now exposes a pane-level `Search` button and a `Reset to generated plan` action above Monaco. Those controls make the developer workflow much faster because a contributor can tweak the current plan, execute it, and then return to the last generated raw-query baseline without touching the top command bar. Even so, the host still does not translate the plan into Elasticsearch JSON itself. It validates the JSON shape locally, preserves user edits when validation fails, records the blocking errors in the diagnostics area, and then delegates successful supplied-plan execution back through the same inward application-service boundary.

### 1a. Two execution paths now share one runtime core

The current host now has two closely related execution paths, and contributors should understand the difference between them because the user interface makes both visible.

- The **raw-query path** starts with free-form text in the top command bar. The application service plans the query first and then executes the resulting plan.
- The **edited-plan path** starts with JSON already present in Monaco. The host validates that JSON against the repository-owned `QueryPlan` contract and then asks the application service to execute that supplied plan directly.

The important point is that the repository still has only one real execution core. `QuerySearchService` now exposes both `SearchAsync()` for raw text and `ExecutePlanAsync()` for caller-supplied plans, but both routes converge on `IQueryPlanExecutor`. That convergence means the edited-plan path is a developer convenience built on top of the same runtime behavior, not a second execution engine hidden in the UI.

### 2. Normalization creates one deterministic input snapshot

The first planning stage converts raw input into a `QueryInputSnapshot`.

That snapshot preserves several related views of the same query:

- the raw text exactly as the caller supplied it
- a lower-cased normalized form
- a cleaned text form with repeated whitespace collapsed
- a deterministic token list
- the residual text and residual tokens that remain available for default matching

This stage matters because later steps should not each invent their own tokenization and cleanup rules. If a contributor is diagnosing surprising rule matches or missing defaults, the first question is often whether the normalized snapshot looks the way they expected.

### 3. Typed extraction adds structure before rules run

After normalization, the planner invokes `ITypedQuerySignalExtractor`.

This is where the runtime looks for structured meaning that should not be treated as plain text. The current contract retains temporal and numeric signals on `QueryExtractedSignals`, including recognized years and richer date-like matches. The planner then uses those extracted values to seed the canonical query model before rule evaluation continues. In current behavior, recognized years are projected into `majorVersion` intent early so later stages can treat them as first-class query meaning rather than as incidental text.

That ordering is deliberate. Rules should be able to react to a typed year that has already been recognized, and the executor should not have to rediscover typed meaning from raw text later.

### 4. Query rules shape repository-owned intent

The next stage evaluates the validated query-rule catalog loaded from the `rules:query:*` configuration namespace.

A query rule is a repository-owned interpretation rule. It can inspect normalized input or extracted signals and then contribute several different kinds of outcome:

- canonical model mutations such as keyword additions
- concept signals that explain which domain idea was recognized
- sort hints and concrete sort directives
- explicit filters that constrain execution without adding score
- explicit boosts that shape score intentionally
- consume directives that remove already-accounted-for words or phrases from the residual path

This is the stage where the repository stops treating the query as only a bag of words. The planner can now say, in a structured way, that part of the query expressed a known concept, part of it requested a recency sort, and part of it should no longer fall through into the default matching path.

### 5. Residual defaults protect the remaining user intent

After rules have run, the planner builds default contributions only from the residual content that remains.

This is a small detail with large practical consequences. If the runtime did not separate rule-owned meaning from residual defaults, the same phrase could influence the search twice: once through a rule and again through naive default matching. Consumption semantics avoid that duplication.

The current default contribution model is intentionally simple and deterministic:

- residual tokens contribute an exact-terms clause against `keywords`
- residual cleaned text contributes analyzed match clauses against `searchText` and `content`

Those defaults are therefore a fallback layer, not the main semantic layer. They preserve useful broad matching without taking ownership away from typed extraction and query rules.

### 6. Mapping translates the repository plan into Elasticsearch JSON

Only once the query meaning is captured in a `QueryPlan` does the infrastructure layer build the Elasticsearch request body.

The mapper combines several sources in stable order:

- canonical model clauses produced by typed extraction and rules
- explicit execution filters
- explicit execution boosts
- residual default contributions
- explicit sort directives

That deterministic order matters for both testing and contributor reasoning. A contributor reading the plan should be able to explain why the final request body contains a given filter, boost, or `should` clause without needing to reverse-engineer host code.

### 7. Execution and result parsing stay in infrastructure

`ElasticsearchQueryExecutor` sends the mapped JSON to the configured canonical index, validates the response, and parses it back into repository-owned result contracts.

The parsed result does not expose raw Elasticsearch client types to the host. Instead, the runtime maps hit data into `QuerySearchResult` and `QuerySearchHit`, including the matched field names and a stable raw payload copy when present. The executor also retains the exact Elasticsearch request JSON that it generated, the search-engine-reported execution duration when Elasticsearch returns one, and any non-blocking warnings that explain why execution was skipped or shaped a particular way. That keeps the host UI focused on presentation while the query infrastructure remains the owner of transport details and response interpretation.

## Worked example: `latest SOLAS`

A query such as `latest SOLAS` is useful because it shows why the runtime needs more than one stage.

### Start with normalization

The raw input enters as `latest SOLAS`. Normalization lower-cases and cleans it into `latest solas`, then produces the token sequence `latest` and `solas`.

At this point, nothing about the query says whether `latest` is ordinary text, a sort request, or both. Nothing yet says whether `solas` should stay a literal token or expand into a broader maritime concept. The normalized snapshot only gives later stages a stable surface to work from.

### Then apply rule-owned meaning

The representative rule behavior exercised in the current tests shows two different rule outcomes:

- a `concept-solas` rule recognizes the token `solas`, adds canonical keywords such as `solas`, `maritime`, `safety`, and `msi`, records a concept signal, and consumes the `solas` token from the residual path
- a `sort-latest` rule recognizes `latest`, emits sort intent for descending `majorVersion` and `minorVersion`, and consumes the phrase `latest`

Once those rules have run, the residual token stream is empty. That is exactly what the planner wants for this query. The meaningful parts of the request have already been captured in structured form, so the default residual layer has nothing left to contribute.

### Finally map and execute the plan

Because the canonical model now contains explicit keyword intent and the execution directives contain explicit sort intent, the mapper can build a focused request body. Instead of searching for the literal phrase `latest solas` across analyzed text fields, it can issue terms-based keyword clauses for the expanded concept and append the descending version sorts.

That behavior is why the query chapter treats rule consumption as a first-class idea rather than a small optimization. `latest SOLAS` should behave like a request for the newest SOLAS-related material, not like a full-text search that still insists on matching the literal word `latest` in the index.

## Why the query runtime stays provider-independent

The query side reads the canonical index, not the upstream provider payloads.

That boundary is one of the main payoffs of the wider repository architecture. Ingestion absorbs source-specific differences earlier and projects them into a canonical search shape. Query can therefore reason in repository-owned field names such as `keywords`, `searchText`, `content`, `category`, `majorVersion`, and `region` instead of trying to understand every source schema directly.

This page is intentionally written in those canonical terms. If a contributor finds themselves designing query behavior around a provider-specific payload detail, that is usually a sign that the needed normalization belongs upstream in ingestion or in the canonical projection rather than in the query pipeline itself.

## When to read the next query pages

| If you need to understand... | Read next |
|---|---|
| Why a rule matched, what it can emit, or how `consume` changes residual defaults | [Query signal extraction rules](Query-Signal-Extraction-Rules) |
| Which projects and runtime boundaries own each stage in practice | [Query walkthrough](Query-Walkthrough) |
| How the repository-owned plan turns into Elasticsearch clauses | [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping) |
| The quick JSON authoring shape for the query-rule DSL | [Appendix: query rule syntax quick reference](Appendix-Query-Rule-Syntax-Quick-Reference) |

## Related pages

- [Home](Home)
- [Solution architecture](Solution-Architecture)
- [Architecture walkthrough](Architecture-Walkthrough)
- [Query signal extraction rules](Query-Signal-Extraction-Rules)
- [Query walkthrough](Query-Walkthrough)
- [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping)
- [Appendix: query rule syntax quick reference](Appendix-Query-Rule-Syntax-Quick-Reference)
- [Project setup](Project-Setup)
