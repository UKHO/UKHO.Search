# Implementation Plan

**Target output path:** `docs/095-signal-extraction-rules/plan-wiki-query-documentation-update_v0.01.md`

Work Package: `docs/095-signal-extraction-rules/`

Based on: `docs/095-signal-extraction-rules/spec-wiki-query-documentation-update_v0.01.md`

Mandatory repository instructions for execution:

- `./.github/instructions/wiki.instructions.md`
- `./.github/instructions/documentation.instructions.md`
- `./.github/instructions/documentation-pass.instructions.md`

Repository planning notes carried into this plan:

- This work package is documentation-only. The intended implementation scope is the repository wiki and work-package documentation, not source-code changes.
- The wiki review defined by `./.github/instructions/wiki.instructions.md` is not a final polish step. It is the main delivery mechanism for this work package and must be treated as a completion gate for every work item.
- The repository standard for architecture, runtime, setup, workflow-heavy, and extension-oriented documentation is book-like narrative depth. The implementation must therefore prefer substantial explanatory prose, explicit technical-term definition, and worked examples over terse bullet-only treatment.
- The target documentation must match the contributor usefulness and narrative style already established on the ingestion side in pages such as `wiki/Ingestion-Pipeline.md`, `wiki/Ingestion-Rules.md`, `wiki/Ingestion-Walkthrough.md`, and `wiki/Appendix-Rule-Syntax-Quick-Reference.md`.
- No source-code changes are planned in this work package. If implementation unexpectedly touches code or hand-maintained C# files while supporting the documentation effort, `./.github/instructions/documentation-pass.instructions.md` becomes a hard gate for those code changes in full, including developer-level comments on all touched public and non-public types and members.
- Do not run the full test suite for this work package. If non-documentation files are touched unexpectedly, prefer targeted validation only.

## Query-side wiki documentation delivery strategy

This plan is organized as documentation vertical slices. Each work item produces a usable contributor-facing capability in the wiki rather than building isolated page fragments that only make sense after later items land.

The overall sequence is:

1. establish the query-side reading path and narrative entry point
2. add the deep-dive query rules guide and quick-reference appendix
3. add the code-oriented runtime walkthrough and the query-model / Elasticsearch-mapping guide
4. close the loop with setup, glossary, and cross-link updates so the full reader journey is coherent
5. record the final explicit wiki review outcome for the full work package

## Query chapter entry and discoverability slice
- [x] Work Item 1: Add the query chapter entry page and expose it through the main wiki reading path - Completed
  - **Purpose**: Deliver the smallest meaningful documentation slice by making the query subsystem discoverable from the existing wiki entry points and by adding the narrative overview page that explains the query runtime as a first-class subsystem.
  - **Acceptance Criteria**:
    - The wiki contains a dedicated `Query-Pipeline.md` page that serves as the narrative entry point for the query-side chapter.
    - The page explains what the query runtime is trying to achieve, why it is staged the way it is, and how normalization, typed extraction, rules, residual defaults, and Elasticsearch execution fit together.
    - `wiki/Home.md` includes an explicit query-side reading route alongside the existing onboarding, setup, ingestion, and Workbench routes.
    - `wiki/Solution-Architecture.md` and `wiki/Architecture-Walkthrough.md` point readers to the new query-side chapter for deeper subsystem detail rather than treating the existing short query passages as sufficient on their own.
  - **Definition of Done**:
    - Documentation implemented in the wiki with current-state guidance and Onion-Architecture-aware terminology.
    - New and updated pages use longer-form narrative prose, define technical terms when first introduced, and include at least one meaningful worked example or walkthrough fragment where it improves comprehension.
    - Any Mermaid diagram used by this slice is GitHub-renderable and supported by surrounding explanatory prose.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Can execute end to end via: opening the wiki entry path at `wiki/Home.md`, following the query reading route into `wiki/Query-Pipeline.md`, and confirming the reader can understand the high-level query runtime without needing source-code inspection.
  - [x] Task 1.1: Create the dedicated query pipeline entry page
    - [x] Step 1: Create `wiki/Query-Pipeline.md` as the query-side sibling of `wiki/Ingestion-Pipeline.md`.
    - [x] Step 2: Add a reading-path section that links forward to the rules, walkthrough, model/mapping, and quick-reference pages.
    - [x] Step 3: Explain the current runtime map in narrative prose, including raw query ingress, normalization, typed extraction, rule evaluation, residual default generation, Elasticsearch mapping, and result parsing.
    - [x] Step 4: Add at least one realistic example query, such as `latest SOLAS`, to anchor the overview in concrete runtime behavior.
    - [x] Step 5: Add a Mermaid diagram only if it materially improves understanding, and explain how to read it.
  - [x] Task 1.2: Expose the new query chapter through the existing wiki entry pages
    - [x] Step 1: Update `wiki/Home.md` to add a query-focused reader route and list the new query chapter pages in the supporting-page sections.
    - [x] Step 2: Update `wiki/Solution-Architecture.md` so the query subsystem overview hands readers off to the new dedicated query pages.
    - [x] Step 3: Update `wiki/Architecture-Walkthrough.md` so its shorter query section explicitly hands off to the new `wiki/Query-Pipeline.md` and later `wiki/Query-Walkthrough.md` pages.
    - [x] Step 4: Review whether any existing query-side text in those pages now needs trimming or reframing so the reading path stays clear rather than duplicative.
  - **Files**:
    - `wiki/Query-Pipeline.md`: New narrative entry page for the query subsystem.
    - `wiki/Home.md`: Query reading-route and supporting-link updates.
    - `wiki/Solution-Architecture.md`: Query chapter hand-off updates.
    - `wiki/Architecture-Walkthrough.md`: Query chapter hand-off updates.
    - `docs/095-signal-extraction-rules/plan-wiki-query-documentation-update_v0.01.md`: Execution updates once the slice is completed.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - Open `wiki/Home.md` in the repository markdown viewer.
    - Follow the query reader path into `wiki/Query-Pipeline.md`.
    - Confirm the page sequence is discoverable and the overview page explains the current query runtime in book-like narrative form.
  - **User Instructions**:
    - None.
  - **Execution Summary**:
    - Created `wiki/Query-Pipeline.md` as the query-side narrative entry page, including a chapter reading path, a GitHub-renderable Mermaid runtime map, stage-by-stage explanation of normalization through result parsing, and a worked `latest SOLAS` example grounded in the current query planner and rule-engine behavior.
    - Updated `wiki/Home.md` to expose a dedicated query reading route, add the query chapter to the main wiki flow, and surface the new chapter pages in the supporting-link sections.
    - Updated `wiki/Solution-Architecture.md` and `wiki/Architecture-Walkthrough.md` so their existing query summaries now hand readers off to the dedicated query chapter instead of acting as the deepest available query documentation.
    - Validation performed: reviewed the updated markdown reading path from `wiki/Home.md` into `wiki/Query-Pipeline.md` and checked that the new overview explains raw ingress, normalization, typed extraction, rule evaluation, residual defaults, mapping, and execution in current-state repository terms.
    - Wiki review result: created `wiki/Query-Pipeline.md`; updated `wiki/Home.md`, `wiki/Solution-Architecture.md`, and `wiki/Architecture-Walkthrough.md`; intentionally left glossary, setup, and deeper query reference pages unchanged in this work item because their dedicated coverage belongs to later work items in this plan.

## Query rules deep-dive and authoring reference slice
- [x] Work Item 2: Add the deep-dive query rules guide and the quick-reference authoring appendix - Completed
  - **Purpose**: Give contributors the same level of authoring and runtime understanding for query rules that the ingestion-side wiki already provides for ingestion rules.
  - **Acceptance Criteria**:
    - The wiki contains a dedicated `Query-Signal-Extraction-Rules.md` page that explains the rules engine in full current-state detail.
    - The page explains repository authoring shape, effective runtime shape under `rules:query:*`, supported predicates, supported action groups, consumption semantics, diagnostics, and refresh behavior.
    - The wiki contains a dedicated `Appendix-Query-Rule-Syntax-Quick-Reference.md` page that acts as a short lookup companion rather than a replacement for the deep-dive page.
    - The rules guide includes detailed worked examples for `latest SOLAS`, `latest notice`, and a year-bearing query such as `latest notice from 2024`.
  - **Definition of Done**:
    - Documentation implemented with explicit explanation of query-rule terminology, purpose, and behavior.
    - The rules page matches the ingestion-side documentation standard by teaching why the rules engine exists and how it behaves, not only by listing JSON fields.
    - The appendix is shorter and lookup-oriented but remains repository-specific and current-state.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Can execute end to end via: reading `wiki/Query-Signal-Extraction-Rules.md` and `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md` and confirming a contributor could author or debug `rules/query/*.json` without relying on source-code reading alone.
  - [x] Task 2.1: Write the deep-dive query rules page
    - [x] Step 1: Create `wiki/Query-Signal-Extraction-Rules.md` as the query-side sibling of `wiki/Ingestion-Rules.md`.
    - [x] Step 2: Explain what query rules do and do not do, including the distinction between canonical query intent, execution directives, and residual defaults.
    - [x] Step 3: Explain the repository authoring shape under `rules/query/*.json` and the effective runtime shape under `rules:query:*`.
    - [x] Step 4: Explain the supported predicates and supported action groups: `model`, `concepts`, `sortHints`, `consume`, `filters`, and `boosts`.
    - [x] Step 5: Explain refresh-aware catalog behavior, matched-rule diagnostics, and why consumption semantics matter for avoiding duplicate residual default matching.
    - [x] Step 6: Add detailed worked examples for `latest SOLAS`, `latest SOLAS msi`, `latest notice`, and `latest notice from 2024`.
  - [x] Task 2.2: Write the quick-reference appendix
    - [x] Step 1: Create `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md` as the query-side sibling of `wiki/Appendix-Rule-Syntax-Quick-Reference.md`.
    - [x] Step 2: Add concise sections for file shapes, required fields, predicate forms, supported paths, supported action groups, and fast examples.
    - [x] Step 3: Keep the page intentionally shorter than the deep-dive rules page and link back to it prominently.
  - [x] Task 2.3: Update terminology support for the rules chapter
    - [x] Step 1: Expand `wiki/Glossary.md` with any query-rule terms still missing after drafting the new rules pages, such as normalized query input, residual defaults, execution directives, exact-match field, analyzed field, and matched rule diagnostics.
    - [x] Step 2: Review glossary wording to ensure the query chapter can reuse the terms consistently without redefining them differently from page to page.
  - **Files**:
    - `wiki/Query-Signal-Extraction-Rules.md`: New deep-dive guide for the query rules engine.
    - `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`: New short lookup appendix for query rule authoring.
    - `wiki/Glossary.md`: Query-rule and query-planning terminology updates.
    - `docs/095-signal-extraction-rules/plan-wiki-query-documentation-update_v0.01.md`: Execution updates once the slice is completed.
  - **Work Item Dependencies**:
    - Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - Open `wiki/Query-Signal-Extraction-Rules.md` and confirm it reads as a deep-dive guide rather than a field list.
    - Open `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md` and confirm it acts as a shorter lookup companion.
    - Check that the glossary terms used in both pages are either defined inline or linked clearly.
  - **User Instructions**:
    - None.
  - **Execution Summary**:
    - Created `wiki/Query-Signal-Extraction-Rules.md` as the deep-dive query-rules guide, covering the current repository authoring shape under `rules/query/*.json`, the effective runtime shape under `rules:query:*`, the supported predicate surface, all supported action groups, refresh-aware catalog behavior, matched-rule diagnostics, and worked current-state examples for `latest SOLAS`, `latest SOLAS msi`, `latest notice`, and `latest notice from 2024`.
    - Created `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md` as the shorter authoring lookup companion with current file shapes, required fields, predicate forms, supported paths, supported action groups, and fast examples.
    - Expanded `wiki/Glossary.md` with query-chapter terminology needed by the new rules pages, including normalized query input, residual defaults, canonical query model, execution directives, query signal extraction rule, matched rule diagnostics, exact-match field, and analyzed field, and updated the glossary reading path to expose the new query chapter pages.
    - Validation performed: reviewed the new rules guide, appendix, and glossary entries against the current query validator, rule engine, catalog, refresh service, mapper, and query tests to keep the documentation aligned with current runtime behavior, then built the workspace successfully.
    - Wiki review result: created `wiki/Query-Signal-Extraction-Rules.md` and `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`; updated `wiki/Glossary.md`; intentionally left setup, walkthrough, and model/mapping pages unchanged in this work item because those deeper runtime-tracing and local-verification topics belong to later work items in this plan.

## Runtime tracing and mapping explanation slice
- [x] Work Item 3: Add the code-oriented query walkthrough and the query model / Elasticsearch mapping guide - Completed
  - **Purpose**: Let contributors trace one query through the running system and understand the repository-owned planning contracts and deterministic Elasticsearch mapping in the same level of detail already available on the ingestion side.
  - **Acceptance Criteria**:
    - The wiki contains a `Query-Walkthrough.md` page that follows one query from local configuration visibility through host composition, normalization, typed extraction, rule evaluation, residual defaults, Elasticsearch request generation, and result parsing.
    - The wiki contains a `Query-Model-and-Elasticsearch-Mapping.md` page that explains the repository-owned query contracts and how they map into Elasticsearch clauses.
    - Both pages use repository-owned terminology first and explain Elasticsearch terms in repository context.
    - The mapping page includes prose-backed JSON examples for model-only, filter/boost-driven, and residual-default request shapes.
  - **Definition of Done**:
    - Documentation implemented with substantial narrative depth and explicit explanation of runtime boundaries across Host, Infrastructure, Services, and Domain.
    - The walkthrough is code-oriented and practical without collapsing into a raw file list.
    - The mapping page clearly explains exact-match fields, analyzed fields, canonical query intent, execution directives, and residual defaults.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Can execute end to end via: reading `wiki/Query-Walkthrough.md` and `wiki/Query-Model-and-Elasticsearch-Mapping.md` and confirming a contributor can trace a query such as `latest SOLAS` from raw text to request body without source-code inspection.
  - [x] Task 3.1: Write the query runtime walkthrough page
    - [x] Step 1: Create `wiki/Query-Walkthrough.md` as the query-side sibling of `wiki/Ingestion-Walkthrough.md`.
    - [x] Step 2: Explain the role of AppHost services mode and the configuration seeding path for `rules/query/*.json`.
    - [x] Step 3: Explain `QueryServiceHost` as the query composition root and identify the main projects involved: `src/Hosts/QueryServiceHost`, `src/UKHO.Search.Query`, `src/UKHO.Search.Services.Query`, and `src/UKHO.Search.Infrastructure.Query`.
    - [x] Step 4: Trace a representative query through normalization, typed extraction, rules catalog access, rules evaluation, residual default construction, Elasticsearch request generation, execution, and result parsing.
    - [x] Step 5: Add practical tracing recipes for contributors debugging query behavior locally.
  - [x] Task 3.2: Write the model and Elasticsearch mapping page
    - [x] Step 1: Create `wiki/Query-Model-and-Elasticsearch-Mapping.md`.
    - [x] Step 2: Explain the purpose and structure of `QueryInputSnapshot`, `QueryExtractedSignals`, the canonical query model, execution directives, and residual default contributions.
    - [x] Step 3: Explain the difference between exact-match canonical fields and analyzed text fields, including why the query runtime uses each of them differently.
    - [x] Step 4: Explain how the mapper combines canonical-model clauses, explicit filters, explicit boosts, residual defaults, and sort directives into one deterministic request body.
    - [x] Step 5: Add JSON examples for model-only, filter/boost-driven, and residual-default requests, and explain each example in prose rather than leaving it as raw JSON.
  - [x] Task 3.3: Strengthen cross-links between the new query runtime pages
    - [x] Step 1: Ensure `wiki/Query-Pipeline.md`, `wiki/Query-Walkthrough.md`, `wiki/Query-Signal-Extraction-Rules.md`, and `wiki/Query-Model-and-Elasticsearch-Mapping.md` link to one another in both concept-first and task-first directions.
    - [x] Step 2: Review whether the shorter query section in `wiki/Architecture-Walkthrough.md` now needs a clearer hand-off to `wiki/Query-Walkthrough.md` after the new page exists.
  - **Files**:
    - `wiki/Query-Walkthrough.md`: New code-oriented walkthrough of the query runtime.
    - `wiki/Query-Model-and-Elasticsearch-Mapping.md`: New deep-dive guide to query contracts and request mapping.
    - `wiki/Architecture-Walkthrough.md`: Optional hand-off refinements after the dedicated walkthrough exists.
    - `docs/095-signal-extraction-rules/plan-wiki-query-documentation-update_v0.01.md`: Execution updates once the slice is completed.
  - **Work Item Dependencies**:
    - Depends on Work Item 2.
  - **Run / Verification Instructions**:
    - Read `wiki/Query-Walkthrough.md` top to bottom and confirm it traces one query through the running system in repository terms.
    - Read `wiki/Query-Model-and-Elasticsearch-Mapping.md` and confirm the request-body examples are explained in prose and tied back to the repository-owned query plan.
    - Check that cross-links between the query pages now support both overview-first and walkthrough-first reading paths.
  - **User Instructions**:
    - None.
  - **Execution Summary**:
    - Created `wiki/Query-Walkthrough.md` as the code-oriented query runtime walkthrough, covering AppHost services mode, rules seeding from the repository `rules` tree into `rules:query:*`, `QueryServiceHost` as the composition root, the `AddQueryServices()` registration story, representative planning flow for `latest SOLAS`, and practical tracing recipes for local debugging.
    - Created `wiki/Query-Model-and-Elasticsearch-Mapping.md` as the deep-dive contract and mapping guide, explaining `QueryInputSnapshot`, `QueryExtractedSignals`, `CanonicalQueryModel`, `QueryExecutionDirectives`, `QueryDefaultContributions`, the difference between exact-match and analyzed fields, and the deterministic request-body assembly used by `ElasticsearchQueryMapper` and `ElasticsearchQueryExecutor`.
    - Updated `wiki/Architecture-Walkthrough.md` to make the hand-off from the shorter query overview to the dedicated `wiki/Query-Walkthrough.md` page more explicit now that the dedicated walkthrough exists.
    - Validation performed: reviewed the new walkthrough and mapping pages against the current AppHost orchestration, query host composition, query planning services, rules infrastructure, typed extractor, mapper, executor, and mapper tests, then built the workspace successfully.
    - Wiki review result: created `wiki/Query-Walkthrough.md` and `wiki/Query-Model-and-Elasticsearch-Mapping.md`; updated `wiki/Architecture-Walkthrough.md`; intentionally left setup pages unchanged in this work item because the local verification guidance belongs to the next planned work item.

## Setup, verification, and chapter cohesion slice
- [x] Work Item 4: Update setup guidance and complete the full query chapter reader journey - Completed
  - **Purpose**: Close the documentation loop so contributors can discover the query chapter, understand the terminology, follow the runtime story, and then verify query-rule behavior in the local environment without gaps in setup or troubleshooting expectations.
  - **Acceptance Criteria**:
    - `wiki/Project-Setup.md` and `wiki/Setup-Walkthrough.md` explain the local verification path for query-rule work in services mode.
    - The reader can move from main wiki entry pages into the query chapter, from the query chapter into setup/verification guidance, and back again without encountering missing terminology or broken reader paths.
    - The updated chapter explicitly explains which existing wiki pages remain unchanged and why, so the ingestion-side chapter is not diluted with query-side material.
  - **Definition of Done**:
    - Documentation implemented with coherent cross-links between overview, walkthrough, reference, glossary, and setup pages.
    - Query-rule local verification guidance includes realistic example searches and explains what contributors should look for when the runtime behaves correctly.
    - Documentation updated in the work package as needed.
    - Wiki review completed; relevant wiki or repository guidance updated, or an explicit no-change review result recorded.
    - Can execute end to end via: starting at `wiki/Home.md`, following the query chapter, then opening `wiki/Project-Setup.md` and `wiki/Setup-Walkthrough.md` to confirm the local verification path for query-rule work is explicitly documented.
  - [x] Task 4.1: Update setup and verification guidance
    - [x] Step 1: Update `wiki/Project-Setup.md` to explain how `rules/query/*.json` seed into `rules:query:*` and how contributors should verify query-rule behavior after startup.
    - [x] Step 2: Update `wiki/Setup-Walkthrough.md` to add practical query-side verification steps using `QueryServiceHost`, runtime logs, and Kibana.
    - [x] Step 3: Use realistic example searches such as `latest SOLAS`, `latest SOLAS msi`, and `latest notice` to explain what healthy runtime behavior looks like.
  - [x] Task 4.2: Review chapter cohesion and intentionally unchanged pages
    - [x] Step 1: Review the final query chapter as a reading path beginning at `wiki/Home.md` and confirm the order of pages is coherent.
    - [x] Step 2: Confirm that ingestion-side pages such as `wiki/Ingestion-Pipeline.md`, `wiki/Ingestion-Rules.md`, `wiki/Ingestion-Walkthrough.md`, and `wiki/Appendix-Rule-Syntax-Quick-Reference.md` remain query-free unless a concrete contributor-facing gap has been identified.
    - [x] Step 3: Record explicitly in the execution notes which pages were intentionally left unchanged and why.
  - **Files**:
    - `wiki/Project-Setup.md`: Query-side services-mode verification updates.
    - `wiki/Setup-Walkthrough.md`: Query-side local walkthrough updates.
    - `wiki/Home.md`: Final reading-path cohesion adjustments if needed.
    - `docs/095-signal-extraction-rules/plan-wiki-query-documentation-update_v0.01.md`: Execution updates once the slice is completed.
  - **Work Item Dependencies**:
    - Depends on Work Item 3.
  - **Run / Verification Instructions**:
    - Open `wiki/Project-Setup.md` and `wiki/Setup-Walkthrough.md` and confirm they now tell a contributor how to verify query-rule behavior locally.
    - Follow the reader journey from `wiki/Home.md` through the query chapter and into setup/verification guidance.
    - Confirm the documentation clearly distinguishes updated query pages from ingestion pages intentionally left unchanged.
  - **User Instructions**:
    - None.
  - **Execution Summary**:
    - Updated `wiki/Project-Setup.md` to deepen the explanation of how repository-authored `rules/query/*.json` files seed into the flat `rules:query:*` runtime namespace during services mode, and to describe the services-mode readiness checks a query-side contributor should perform before treating the local stack as healthy.
    - Updated `wiki/Setup-Walkthrough.md` to add a dedicated services-mode query verification workflow that uses `QueryServiceHost`, query-side logs, and Kibana together, with concrete healthy-behavior explanations for `latest SOLAS`, `latest SOLAS msi`, and `latest notice`.
    - Updated `wiki/Home.md` so the query chapter now explicitly hands readers back to the setup chapter when they are ready to prove local runtime behavior rather than only understand the conceptual documentation.
    - Validation performed: reviewed the reader journey from `wiki/Home.md` through the query chapter and back into `wiki/Project-Setup.md` and `wiki/Setup-Walkthrough.md`, then built the workspace successfully.
    - Wiki review result: updated `wiki/Project-Setup.md`, `wiki/Setup-Walkthrough.md`, and `wiki/Home.md`; intentionally left `wiki/Ingestion-Pipeline.md`, `wiki/Ingestion-Rules.md`, `wiki/Ingestion-Walkthrough.md`, and `wiki/Appendix-Rule-Syntax-Quick-Reference.md` unchanged because the final cohesion review found no query-side contributor gap that required diluting the ingestion chapter with query-specific setup material.

## Final mandatory wiki review and package closure
- [x] Work Item 5: Record the final wiki review/update outcome for the full work package - Completed
  - **Purpose**: Satisfy the repository’s mandatory wiki-maintenance gate for the entire work package and explicitly record the documentation outcome.
  - **Acceptance Criteria**:
    - The relevant wiki pages, glossary entries, and repository guidance paths have been reviewed against the final query chapter design delivered by this work package.
    - Any required wiki updates are completed.
    - If any reviewed page requires no change, the no-change result is explicitly recorded with a concrete explanation of what was reviewed and why it remained sufficient.
  - **Definition of Done**:
    - Wiki review performed in accordance with `./.github/instructions/wiki.instructions.md`.
    - The final execution record states which wiki pages were updated, created, intentionally left unchanged, or why no change was needed.
    - Foundational documentation retains book-like narrative depth, defines technical terms clearly, and includes examples or walkthroughs where they materially improve comprehension.
    - Can execute end to end via: review of the final implementation record and the resulting query-side wiki chapter.
  - [x] Task 5.1: Review contributor-facing documentation paths
    - [x] Step 1: Review `wiki/Home.md`, `wiki/Solution-Architecture.md`, `wiki/Architecture-Walkthrough.md`, `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Setup-Walkthrough.md` against the final query chapter pages.
    - [x] Step 2: Confirm whether any additional cross-links, glossary definitions, or setup notes are still missing.
    - [x] Step 3: Apply any final adjustments needed to keep the reader journey coherent.
  - [x] Task 5.2: Record the explicit outcome
    - [x] Step 1: Add a final work-package execution note stating exactly which pages were created, which existing pages were updated, and which pages were intentionally left unchanged after review.
    - [x] Step 2: Ensure the wording is explicit and concrete rather than generic.
  - **Files**:
    - `wiki/*`: Final review adjustments if needed.
    - `docs/095-signal-extraction-rules/plan-wiki-query-documentation-update_v0.01.md`: Final execution record updates.
  - **Work Item Dependencies**:
    - Depends on Work Items 1 through 4.
  - **Run / Verification Instructions**:
    - Review the final query chapter pages and the updated wiki entry points.
    - Confirm the final execution record explicitly lists updated, created, and unchanged pages.
  - **User Instructions**:
    - None.
  - **Execution Summary**:
    - Reviewed the final contributor-facing reader path across `wiki/Home.md`, `wiki/Solution-Architecture.md`, `wiki/Architecture-Walkthrough.md`, `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Setup-Walkthrough.md` against the completed query chapter pages `wiki/Query-Pipeline.md`, `wiki/Query-Signal-Extraction-Rules.md`, `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`, `wiki/Query-Walkthrough.md`, and `wiki/Query-Model-and-Elasticsearch-Mapping.md`.
    - Applied one final cohesion adjustment to `wiki/Glossary.md` so the main reading-path section now links directly to `wiki/Setup-Walkthrough.md`, `wiki/Query-Walkthrough.md`, and `wiki/Query-Model-and-Elasticsearch-Mapping.md`, making the glossary usable from both the concept-first and setup-first routes.
    - Final created wiki pages for this work package: `wiki/Query-Pipeline.md`, `wiki/Query-Signal-Extraction-Rules.md`, `wiki/Appendix-Query-Rule-Syntax-Quick-Reference.md`, `wiki/Query-Walkthrough.md`, and `wiki/Query-Model-and-Elasticsearch-Mapping.md`.
    - Final updated existing wiki pages for this work package: `wiki/Home.md`, `wiki/Solution-Architecture.md`, `wiki/Architecture-Walkthrough.md`, `wiki/Glossary.md`, `wiki/Project-Setup.md`, and `wiki/Setup-Walkthrough.md`.
    - Final intentionally unchanged pages after review: `wiki/Ingestion-Pipeline.md`, `wiki/Ingestion-Rules.md`, `wiki/Ingestion-Walkthrough.md`, and `wiki/Appendix-Rule-Syntax-Quick-Reference.md`, because the completed query chapter, setup guidance, and glossary now cover the query-side contributor journey without needing to dilute the ingestion chapter with query-specific material.
    - Validation performed: reviewed the final query chapter and updated reader paths for coherence, then built the workspace successfully.
    - Wiki review result: created five new query-side chapter pages, updated six existing reader-path and setup/glossary pages, made one final glossary cross-link refinement during the closure review, and recorded the intentionally unchanged ingestion-side pages explicitly.

## Summary / key considerations

- This is a documentation-only work package, so the vertical slices are contributor-facing documentation capabilities rather than runtime code features.
- The most important requirement is not simply to add pages. It is to create a coherent query-side chapter that matches the ingestion-side documentation standard in narrative depth, clarity, terminology handling, and practical usefulness.
- The query-side wiki should mirror the ingestion-side reading model closely enough that contributors immediately recognize how to navigate it, while still using query-appropriate concepts such as normalization, typed extraction, residual defaults, execution directives, and deterministic Elasticsearch mapping.
- The rules page must remain a true deep-dive guide rather than collapsing into a JSON field list. The short appendix exists precisely so the deep-dive page can stay explanatory.
- The setup pages matter because query-rule iteration is part of the real local services-mode workflow. The query chapter will still feel incomplete if contributors cannot connect the conceptual pages back to a concrete local verification path.
- The final execution record must state explicitly which query pages were created, which existing pages were updated, and which existing pages were intentionally left unchanged after review.
