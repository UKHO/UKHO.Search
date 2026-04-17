# Specification: Wiki Update for Query Signal Extraction Rules and Query Pipeline

**Target output path:** `docs/095-signal-extraction-rules/spec-wiki-query-documentation-update_v0.01.md`

**Version:** v0.01 (Draft)

**Related work package:** `docs/095-signal-extraction-rules/`

**Primary source documents:**

- `docs/095-signal-extraction-rules/spec-domain-query-signal-extraction-rules_v0.01.md`
- `docs/095-signal-extraction-rules/plan-query-signal-extraction-rules_v0.01.md`
- `./.github/instructions/wiki.instructions.md`

## 1. Overview

### 1.1 Purpose

This specification defines the wiki documentation update required to bring the query-side documentation up to the same standard, depth, and contributor usefulness as the ingestion-side documentation already present in the repository wiki.

The immediate problem is not that the repository lacks any mention of query behavior. The problem is that the current wiki path only contains partial query-side coverage embedded inside broader architecture and setup pages. That level of coverage is enough to signal that the query runtime exists, but it is not enough to teach a contributor how the query pipeline actually works, how query rules are authored and loaded, how typed extraction participates in planning, how residual defaults behave after rule consumption, or how the repository-owned query plan maps into Elasticsearch request bodies.

The repository now has a real query pipeline with these characteristics:

- query normalization
- typed extraction behind `ITypedQuerySignalExtractor`
- flat query-rule loading from `rules/query/*.json`
- rule-driven concept expansion, sort hints, filters, and boosts
- residual default matching against canonical fields
- repository-owned query-plan and diagnostics contracts
- deterministic Elasticsearch request mapping and execution

Those behaviors materially change how contributors must reason about search in this repository. The wiki therefore needs a complete query-side chapter set, written in the same book-like style already used for ingestion pages such as [Ingestion pipeline](../../wiki/Ingestion-Pipeline.md), [Ingestion rules](../../wiki/Ingestion-Rules.md), [Ingestion walkthrough](../../wiki/Ingestion-Walkthrough.md), and [Appendix: rule syntax quick reference](../../wiki/Appendix-Rule-Syntax-Quick-Reference.md).

This specification describes the wiki pages that should be added or expanded, the reading-path changes that should accompany them, and the exact depth of coverage expected in each page.

### 1.2 Scope

This specification covers the contributor-facing wiki documentation for the query side of the repository.

It includes:

- a proposed query-side wiki page set
- required updates to existing reading-path and architecture pages
- content expectations for each proposed page
- terminology additions needed in the glossary
- explicit cross-linking requirements between overview, walkthrough, reference, and setup pages
- detailed expectations for examples, diagrams, and explanation style
- guidance for how the query-side documentation should mirror the ingestion-side documentation model without duplicating ingestion-specific concepts that do not belong on the query side

This specification does not implement the wiki pages themselves.

This specification also does not introduce new runtime behavior. It defines how the wiki should describe the runtime that already exists after the query signal extraction work package.

### 1.3 Stakeholders

The primary stakeholders are:

- contributors working on `QueryServiceHost`
- contributors working on `UKHO.Search.Query`
- contributors working on `UKHO.Search.Services.Query`
- contributors working on `UKHO.Search.Infrastructure.Query`
- rule authors maintaining `rules/query/*.json`
- developers debugging query results against Kibana or Elasticsearch
- maintainers responsible for keeping repository wiki guidance aligned with the current runtime

### 1.4 Definitions

- **Query pipeline**: the end-to-end query runtime that turns raw user-entered search text into a repository-owned `QueryPlan` and then into Elasticsearch request JSON.
- **Query signal extraction rule**: a flat JSON-authored rule stored under `rules/query/*.json` that can inspect normalized input or typed extracted signals and emit canonical query intent, concepts, execution-time directives, or consume directives.
- **Query plan**: the repository-owned runtime contract that carries normalized input, typed extraction outputs, canonical query intent, residual defaults, execution directives, and diagnostics.
- **Execution directives**: query-plan elements that influence Elasticsearch execution but are not themselves part of the canonical query model, such as sort directives, filters, and boosts.
- **Residual defaults**: the default keyword and analyzed-text contributions built only from the query content that remains after rule consumption has removed matched phrases or tokens.
- **Typed extraction**: the stage that derives structured values such as years and numbers from cleaned query text through `ITypedQuerySignalExtractor`.
- **Canonical query model**: the query-owned model that mirrors the discovery-facing half of the canonical index contract without reusing the ingestion `CanonicalDocument` CLR type directly.

## 2. System context

### 2.1 Current documentation state

The wiki currently contains enough query-side information to prove that the query runtime is no longer stub-based, but not enough to function as a full contributor chapter.

Current evidence in the wiki includes:

- `wiki/Home.md` now states that `QueryServiceHost` uses a real query path rather than a stub client.
- `wiki/Solution-Architecture.md` now explains that query planning includes typed extraction, flat query-rule loading, rule-driven execution behavior, and Elasticsearch request mapping.
- `wiki/Architecture-Walkthrough.md` now includes a short query-path section and a high-level explanation of normalization, typed extraction, rule evaluation, residual defaults, and mapping.
- `wiki/Glossary.md` now defines several query-specific terms, including `ITypedQuerySignalExtractor`, query plan, query rule, and query rules catalog.
- `wiki/Project-Setup.md` now explains that `rules/query/*.json` are seeded into `rules:query:*` during local services-mode startup and recommends query-specific verification checks after startup.

Those improvements were necessary and correct, but they still leave a contributor without a dedicated reading path comparable to the ingestion-side journey.

A contributor who wants to answer any of the following questions does not yet have a single authoritative wiki chapter sequence:

- What exactly happens between raw query text and the final Elasticsearch request body?
- Which parts of the query runtime belong in Domain, Services, Infrastructure, and Host layers?
- How are query rules authored, loaded, validated, and refreshed?
- What do `concepts`, `sortHints`, `filters`, `boosts`, and `consume` mean in practice?
- Which parts of the query plan are canonical intent and which parts are execution-only behavior?
- How do rule-shaped clauses and residual defaults combine in Elasticsearch?
- Why did a particular query such as `latest SOLAS` behave the way it did?

The ingestion side already has a strong answer to this kind of documentation need:

- an overview page for the subsystem
- a deep-dive page for the rules engine
- a walkthrough page that traces the runtime in code-oriented order
- a short syntax appendix for fast authoring lookup

The query side needs the same documentation structure.

### 2.2 Proposed documentation state

The wiki should gain a dedicated query-side documentation chapter with the same overall shape and contributor experience as the ingestion-side chapter.

The recommended query-side page set is:

1. `wiki/Query-Pipeline.md`
2. `wiki/Query-Signal-Extraction-Rules.md`
3. `wiki/Query-Walkthrough.md`
4. `wiki/Query-Model-and-Elasticsearch-Mapping.md`
5. `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`

The wiki should also update the current reading path in these existing pages:

- `wiki/Home.md`
- `wiki/Solution-Architecture.md`
- `wiki/Architecture-Walkthrough.md`
- `wiki/Glossary.md`
- `wiki/Project-Setup.md`
- `wiki/Setup-Walkthrough.md`

This page set is intentionally similar to the ingestion-side pattern, but not identical.

The ingestion-side chapter revolves around message processing, provider-specific enrichment, dead-letter flows, and canonical document mutation. The query-side chapter revolves around interpretation of free-form text, typed extraction, flat global rules, residual defaults, repository-owned planning contracts, and deterministic request mapping. The documentation structure should therefore mirror the ingestion-side reading model while still using query-appropriate terms and examples.

### 2.3 Documentation assumptions

This specification assumes the current runtime behavior already implemented by the work package:

- `QueryServiceHost` is the real query host.
- Query planning is repository-owned and no longer stub-based.
- Typed extraction is active behind `ITypedQuerySignalExtractor`.
- Query rules are authored flat under `rules/query/*.json`.
- Query rules are loaded from the `rules:query:*` configuration namespace.
- Query rules can emit model mutations, concepts, sort hints, filters, boosts, and consume directives.
- Elasticsearch mapping uses canonical model clauses, explicit execution directives, and residual defaults in deterministic order.
- Query-side documentation should remain provider-independent, because the query runtime targets the canonical index rather than provider-specific source payloads.

### 2.4 Documentation constraints

The wiki update defined by this specification must follow `./.github/instructions/wiki.instructions.md` in full.

That means the resulting pages must:

- describe the repository as it exists now
- use present-tense, current-state guidance
- preserve book-like narrative depth
- define specialized terms instead of assuming them
- connect concept pages and walkthrough pages into a deliberate reading path
- use examples where they materially improve comprehension
- avoid shallow, bullet-only treatment for foundational topics

This specification also adopts the repository preference that a work package should produce one comprehensive spec document rather than splitting the planning material across multiple spec files.

## 3. Component / service design (high level)

### 3.1 Proposed wiki page set

#### 3.1.1 `wiki/Query-Pipeline.md`

This page should become the narrative entry point for the query-side chapter in the same way that `wiki/Ingestion-Pipeline.md` is the narrative entry point for ingestion.

Its job is to explain:

- what the query runtime is trying to achieve
- why the query side is structured as a pipeline rather than a host-local request builder
- where the main stage boundaries are
- how normalized input, typed extraction, rules, residual defaults, and Elasticsearch execution fit together
- which pages the reader should continue to next depending on whether they need conceptual, authoring, or code-tracing detail

This page should not try to carry every implementation detail. It should establish the mental model and the current runtime map.

#### 3.1.2 `wiki/Query-Signal-Extraction-Rules.md`

This page should be the deep-dive rules-engine guide in the same role that `wiki/Ingestion-Rules.md` currently plays for ingestion.

Its job is to explain:

- what query rules do and do not do
- the repository authoring shape for `rules/query/*.json`
- the effective runtime view under `rules:query:*`
- rule-loading and refresh behavior
- predicate semantics
- supported rule action groups
- consume behavior and why it matters
- diagnostics and observability around matched rules and applied execution directives
- worked examples for representative queries such as `latest SOLAS` and `latest notice`

This page should be extremely thorough and should become the main reference narrative for query-rule contributors.

#### 3.1.3 `wiki/Query-Walkthrough.md`

This page should play the same role for query as `wiki/Ingestion-Walkthrough.md` plays for ingestion.

Its job is to follow one query from local startup conditions through the runtime, including:

- local AppHost services mode and configuration seeding of `rules/query`
- `QueryServiceHost` as the composition root for the query side
- the normalization stage
- typed extraction invocation
- rules catalog access and rule engine evaluation
- residual default generation
- deterministic mapping into Elasticsearch request JSON
- result parsing back into the repository-owned hit model

This page should be code-oriented and should help contributors trace responsibility boundaries across Host, Infrastructure, Services, and Domain.

#### 3.1.4 `wiki/Query-Model-and-Elasticsearch-Mapping.md`

This page is needed because the query-side model and request mapping are rich enough to justify their own conceptual treatment.

Its job is to explain:

- the structure and purpose of `QueryInputSnapshot`
- the structure and purpose of `QueryExtractedSignals`
- the canonical query model and how it mirrors the discovery-facing half of the canonical index
- the separation between canonical query intent and execution directives
- how residual default contributions are constructed
- how the Elasticsearch mapper combines canonical model clauses, filters, boosts, sorts, and residual defaults
- how to reason about bool `filter`, bool `should`, `minimum_should_match`, and sort clauses in repository terms rather than raw Elasticsearch jargon alone

This page should be especially explicit about which fields are exact-match and which are analyzed fields, and why the query runtime uses each of them the way it does.

#### 3.1.5 `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`

This page should be the short authoring companion for query-rule JSON in the same spirit as the ingestion-side rule appendix.

Its job is to provide:

- rule file shapes used in the repository
- required fields
- supported predicate forms and paths
- supported action groups and field constraints
- concise examples for common authoring tasks
- quick examples for `containsPhrase`, `eq`, `model`, `concepts`, `sortHints`, `filters`, `boosts`, and `consume`

This page should stay shorter and more lookup-oriented than `wiki/Query-Signal-Extraction-Rules.md`, and should explicitly point the reader back to the deep-dive page.

### 3.2 Required existing-page updates

#### 3.2.1 `wiki/Home.md`

This page should be updated so the reading routes include a dedicated query-side path.

The current reader table includes onboarding, setup, ingestion, Workbench, and repository-history paths. It should add a query-focused route such as:

- start with `Solution architecture`
- continue to `Query pipeline`
- then `Query walkthrough`
- then `Query signal extraction rules`
- then `Query model and Elasticsearch mapping`
- then `Appendix: query rule syntax quick reference`

The “Major areas of the wiki” and “Related supporting pages” sections should also expose the new query chapter explicitly.

#### 3.2.2 `wiki/Solution-Architecture.md`

This page already contains the broad architectural summary, but once the dedicated query pages exist it should link to them explicitly as the deeper reading path for the query subsystem.

The architecture page should remain an overview, not a substitute for the new query chapter.

#### 3.2.3 `wiki/Architecture-Walkthrough.md`

This page should link to `Query-Walkthrough.md` as the deeper follow-on for contributors working specifically on the query side.

It should not duplicate the full query walkthrough, but it should clearly hand off to it.

#### 3.2.4 `wiki/Glossary.md`

This page should be expanded so the new query chapter can define and reuse terms consistently.

Additional entries likely needed include:

- normalized query input
- residual defaults
- canonical query model
- execution directives
- query signal extraction rule
- matched rule diagnostics
- exact-match field
- analyzed field

#### 3.2.5 `wiki/Project-Setup.md` and `wiki/Setup-Walkthrough.md`

These pages should explain the practical local verification path for query-rule work in the same way they already explain ingestion-rule work.

That means they should cover:

- how `rules/query/*.json` seed into `rules:query:*`
- how to verify that the local services stack has loaded current query rules
- which example searches are useful sanity checks after startup or after a query-rule change
- which tools to use when the behavior does not match expectation, including `QueryServiceHost`, Kibana, and the runtime logs

### 3.3 Partial migration-plan expectations

This specification includes page recommendations, reading-path changes, and explicit existing-page update requirements.

It does not prescribe a detailed step-by-step implementation order for the wiki update.

That decision follows the chosen collaboration outcome for this work: include the migration intent and reading-path impact, but do not turn the specification into an execution checklist.

## 4. Functional documentation requirements

### 4.1 Common requirements for all new query-side wiki pages

Every new query-side page defined by this specification must:

- read as current-state guidance
- explain what the page topic is before explaining how to work with it
- define important query-side terms when they first appear, or link to the glossary entry that defines them
- use repository-owned terminology first, with external technology terms such as Elasticsearch terms explained in repository context
- link both backward and forward in the reading path so a contributor can move between overview, walkthrough, and quick-reference material naturally
- include examples that use real repository behavior and realistic query text
- avoid vague phrases such as “the system may later” unless the historical/future context is explicitly labelled and useful
- explain why the design exists, not only which files are involved

### 4.2 Requirements for `wiki/Query-Pipeline.md`

This page must include at least the following sections:

1. Reading path
2. What the query runtime is trying to achieve
3. Current runtime map
4. Why the query runtime is staged this way
5. Stage-by-stage view
6. Diagnostics and observability
7. Practical local commands or local verification notes
8. When to read the next query pages

This page must explain, in narrative prose, the current stage sequence:

1. raw query ingress
2. normalization
3. typed extraction
4. rule evaluation
5. residual default generation
6. Elasticsearch mapping and execution
7. result parsing

It must include at least one Mermaid diagram if that diagram materially improves understanding.

A suitable diagram would show the flow from `QueryServiceHost` to normalized input, typed extraction, rules evaluation, default contributions, Elasticsearch, and result projection.

### 4.3 Requirements for `wiki/Query-Signal-Extraction-Rules.md`

This page must include at least the following sections:

1. Reading path
2. What the rules engine does
3. Current authoring and loading model
4. Global scope and runtime identity
5. Required rule fields
6. Predicate model
7. Path model
8. Action-group semantics
9. Consumption semantics
10. Diagnostics and refresh behavior
11. Worked examples
12. Practical local checks

This page must explicitly describe the current repository authoring shape:

- one rule per file under `rules/query/*.json`
- wrapper structure using `schemaVersion` and `rule`

This page must explicitly describe the effective runtime view:

- AppHost seeding into `rules:query:<rule-id>`
- flat query-rule loading without nested-provider behavior
- refresh-aware catalog behavior and the difference between authoring shape and configuration key shape

It must explain the supported action groups as they exist now:

- `model`
- `concepts`
- `sortHints`
- `consume`
- `filters`
- `boosts`

It must explain the purpose of each action group in repository terms, not only in JSON terms.

It must include detailed worked examples for at least:

- `latest SOLAS`
- `latest notice`
- a year-bearing query such as `latest notice from 2024`

For each worked example, the page should explain:

- how normalization shapes the text
- which rule predicates match
- which canonical model changes occur
- which execution directives are emitted
- which content is consumed
- what residual defaults remain
- what the final high-level Elasticsearch intent becomes

### 4.4 Requirements for `wiki/Query-Walkthrough.md`

This page must include at least the following sections:

1. Reading path
2. One-query mental model
3. AppHost and configuration visibility
4. Query host composition
5. Normalization
6. Typed extraction
7. Rules catalog and rule engine evaluation
8. Residual default construction
9. Elasticsearch request generation and execution
10. Practical tracing recipes
11. Where to look when extending the query side

This page must explicitly name the main repository areas involved in the query runtime:

- `src/Hosts/QueryServiceHost`
- `src/UKHO.Search.Query`
- `src/UKHO.Search.Services.Query`
- `src/UKHO.Search.Infrastructure.Query`

It must explain how the query side preserves Onion Architecture boundaries rather than flattening planning, execution, and UI behavior into one host project.

### 4.5 Requirements for `wiki/Query-Model-and-Elasticsearch-Mapping.md`

This page must include at least the following sections:

1. Reading path
2. Why the query side needs repository-owned contracts
3. Query input snapshot
4. Typed extracted signals
5. Canonical query model
6. Execution directives
7. Residual default contributions
8. Elasticsearch mapping by clause type
9. Worked request-body examples
10. Common reasoning mistakes

This page must explicitly distinguish:

- exact-match canonical fields such as `keywords`, `authority`, `region`, `format`, `category`, `series`, `instance`, `title`, `majorVersion`, and `minorVersion`
- analyzed text fields such as `searchText` and `content`

It must explain how the mapper combines:

- canonical-model clauses
- explicit filters
- explicit boosts
- residual defaults
- sort directives

It must include JSON examples showing representative request-body fragments, including:

- a model-only request such as `latest SOLAS`
- a request with filters and boosts such as `latest notice`
- a default-only request from residual content

The examples must be explained in prose rather than left as raw JSON dumps.

### 4.6 Requirements for `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`

This page must include:

- repository authoring shape
- effective runtime shape
- required fields table
- predicate forms table
- supported path table
- supported action-group summary
- small fast examples for common rule-authoring tasks

The appendix must stay intentionally shorter than the deep-dive rules page, but it must still be current-state and repository-specific.

### 4.7 Requirements for reading-path and cross-link updates

The completed wiki update must make the query chapter discoverable from the same places where contributors currently discover ingestion guidance.

At minimum:

- `Home` must include a query reading route.
- `Solution architecture` must point readers to the query chapter for subsystem detail.
- `Architecture walkthrough` must hand off to the query walkthrough page.
- `Project setup` and `Setup walkthrough` must mention query-rule verification in services mode.
- `Glossary` must support the terminology used across the new query pages.

## 5. Narrative and style requirements

### 5.1 Match the ingestion-side documentation style

The new query documentation must match the style and contributor usefulness of the ingestion-side documentation set.

That means the query pages must:

- read like a chapter in the same technical book
- use long-form prose as the default treatment for foundational topics
- combine conceptual explanation and practical examples
- avoid reducing the subject to terse reference bullets
- expose real runtime boundaries and rationale
- help contributors reason about the subsystem, not just recite its file layout

The query chapter should feel like the sibling of the ingestion chapter, not like an isolated appendix.

### 5.2 Example quality requirements

Examples must be concrete, current-state, and repository-specific.

Suitable examples include:

- `latest SOLAS`
- `latest SOLAS msi`
- `latest notice`
- `latest notice from 2024`

Each example should teach a specific concept such as:

- concept expansion
- token consumption
- residual default preservation
- typed year projection into `majorVersion`
- explicit filter and boost emission
- request-body clause composition

### 5.3 Diagram requirements

Mermaid diagrams are optional but recommended where they improve understanding.

Recommended places include:

- the query pipeline overview page
- the query walkthrough page
- optionally the model/mapping page if a clause-composition diagram adds clarity

Any included diagram must be accompanied by prose that explains how to read it and why it matters.

## 6. Proposed wiki page inventory and target paths

### 6.1 New wiki pages to create

- `wiki/Query-Pipeline.md`
- `wiki/Query-Signal-Extraction-Rules.md`
- `wiki/Query-Walkthrough.md`
- `wiki/Query-Model-and-Elasticsearch-Mapping.md`
- `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`

### 6.2 Existing wiki pages to update

- `wiki/Home.md`
- `wiki/Solution-Architecture.md`
- `wiki/Architecture-Walkthrough.md`
- `wiki/Glossary.md`
- `wiki/Project-Setup.md`
- `wiki/Setup-Walkthrough.md`

### 6.3 Pages intentionally left unchanged unless later review finds a concrete gap

At the time of this specification, the following pages do not require mandatory change solely because of the query-signal work package:

- `wiki/Ingestion-Pipeline.md`
- `wiki/Ingestion-Rules.md`
- `wiki/Ingestion-Walkthrough.md`
- `wiki/Appendix-Rule-Syntax-Quick-Reference.md`

These pages remain the ingestion-side reference set and should not be diluted with query-side detail.

## 7. Acceptance criteria

The wiki update defined by this specification is complete when:

1. the wiki contains a dedicated query chapter with overview, walkthrough, rules, model/mapping, and quick-reference pages
2. the new pages follow the narrative depth and reading-path style already used by the ingestion chapter
3. contributors can trace a query from raw text to Elasticsearch request body using only the wiki
4. contributors can author and reason about `rules/query/*.json` using the wiki without relying on source-code reading alone
5. the glossary and setup pages support the terminology and local verification path used by the new query chapter
6. reading-path updates make the new query chapter discoverable from the main wiki entry points
7. the resulting pages remain current-state, repository-specific, and explicitly aligned with `./.github/instructions/wiki.instructions.md`

## 8. Risks and important decisions

### 8.1 Risk: query documentation remains distributed and hard to discover

If the query-side documentation remains embedded only inside broader architecture pages, contributors will continue to understand the query runtime as a secondary concern instead of as a first-class subsystem. That makes debugging slower and makes future query-rule work appear more mysterious than it should.

### 8.2 Risk: the rules page becomes a thin JSON reference instead of a contributor guide

If `Query-Signal-Extraction-Rules.md` is written as a short field list rather than as a full deep-dive, it will fail to match the ingestion-side standard and will not help contributors understand why query-rule behavior can differ from residual defaults.

### 8.3 Important decision: keep the query chapter parallel to the ingestion chapter

This specification deliberately recommends a query-side chapter structure that parallels ingestion:

- subsystem overview
- deep-dive rules page
- code-oriented walkthrough
- short syntax appendix

That is the right shape because the query subsystem is now rich enough to justify the same contributor journey.

### 8.4 Important decision: keep the model/mapping material as its own page

The query-side contract and Elasticsearch mapping behavior are central enough to deserve their own page rather than being buried in the pipeline overview or rules page.

That separation keeps the reader path clearer:

- use the pipeline page for the runtime story
- use the rules page for rule semantics
- use the walkthrough for code-oriented tracing
- use the model/mapping page for contract and DSL reasoning

## 9. Suggested examples that must appear in the eventual wiki update

The eventual wiki implementation should include, at minimum, examples that explain:

1. **`latest SOLAS`**
   - `latest` produces sort intent and is consumed
   - `SOLAS` triggers concept expansion into `solas`, `maritime`, `safety`, and `msi`
   - residual defaults may be empty
   - the final request still executes because model clauses are executable

2. **`latest SOLAS msi`**
   - `SOLAS` still expands by rule
   - `msi` may remain in residual content depending on the rule and query shape
   - the example is useful for explaining the relationship between rule expansions and residual defaults

3. **`latest notice`**
   - a rule can emit explicit filter and boost directives
   - residual content may still participate in defaults if not consumed
   - the resulting Elasticsearch request can contain both non-scoring filter clauses and scoring boosts

4. **`latest notice from 2024`**
   - typed extraction recognizes a year
   - the planner projects the year into `majorVersion`
   - rules and defaults still run in predictable order around the typed signal

## 10. Explicit outcome statement expected when this specification is later executed

When the wiki update described by this specification is carried out, the execution record should state explicitly:

- which new query-side wiki pages were created
- which existing pages were updated
- whether any pages were intentionally left unchanged after review
- why the final page set was chosen

That reporting requirement is part of the repository wiki workflow and must not be skipped.

## 11. Open questions

None.

## 12. Change log

### v0.01

- Initial draft defining the full wiki documentation specification for the query-side chapter, including the proposed page set, required reading-path updates, style expectations, and detailed content coverage requirements.
