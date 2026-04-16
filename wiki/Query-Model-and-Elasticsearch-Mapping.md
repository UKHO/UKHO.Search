# Query model and Elasticsearch mapping

Use this page when you want the contract-level explanation of how the repository-owned query plan is structured and how that plan becomes a deterministic Elasticsearch request body.

## Reading path

- Start with [Query pipeline](Query-Pipeline) for the wider mental model of the query-side runtime.
- Read [Query walkthrough](Query-Walkthrough) when you want the host-to-executor tracing story before diving into field-level mapping detail.
- Keep [Query signal extraction rules](Query-Signal-Extraction-Rules) nearby when you need the rule-authoring explanation of where canonical intent, execution directives, and consumption behavior come from.
- Keep [Appendix: query rule syntax quick reference](Appendix-Query-Rule-Syntax-Quick-Reference) nearby when you need the shorter rule JSON lookup companion while reading this page.

## Why this page exists

The query runtime owns more than one contract, and the distinction between those contracts matters for contributors.

A search request does not move directly from UI text to Elasticsearch JSON. The runtime first shapes repository-owned models that explain what the query means in repository terms. Only after that does the infrastructure layer decide how Elasticsearch should express that meaning. If those two steps are blurred together, contributors are forced to debug search behavior from the raw request body outward. This page exists to preserve the opposite reading order: repository-owned meaning first, search-engine clause shape second.

## The main repository-owned contracts in order

The current query plan is made of five closely related contract areas:

1. `QueryInputSnapshot`
2. `QueryExtractedSignals`
3. `CanonicalQueryModel`
4. `QueryExecutionDirectives`
5. `QueryDefaultContributions`

Together they become `QueryPlan`, which is the boundary between query interpretation and query execution.

The current diagnostics-first Query UI makes that boundary visible in a very direct way. The left insight column derives extracted signals and a staged transformation trace from the repository-owned `QueryPlan`, while the right diagnostics column shows the final Elasticsearch request JSON and execution timings returned after the executor has mapped and run that plan. Contributors therefore need to understand not only how the plan is shaped, but also how the executor preserves the mapped request as a developer-facing runtime artifact.

## 1. `QueryInputSnapshot`: what the planner actually saw

`QueryInputSnapshot` is the immutable normalized view of the user query.

It keeps six related surfaces:

- `RawText`
- `NormalizedText`
- `CleanedText`
- `Tokens`
- `ResidualTokens`
- `ResidualText`

Those surfaces are not redundant. They let the runtime separate different kinds of reasoning.

`RawText` preserves exactly what the caller supplied. `NormalizedText` preserves the lowercased form before whitespace collapse. `CleanedText` is the version later stages usually reason over. `Tokens` provide deterministic token order. `ResidualTokens` and `ResidualText` are the mutable idea surfaces that rules and defaults share over time.

The important conceptual point is that the repository does not treat normalization as a disposable preprocessing detail. It treats it as a first-class contract that later stages can explain and diagnose.

## 2. `QueryExtractedSignals`: typed meaning that does not belong to raw text anymore

`QueryExtractedSignals` holds the structured values derived from the query before rule evaluation finishes.

The current contract contains:

- `Temporal`, which includes recognized `Years` and richer `Dates`
- `Numbers`, which carry normalized numeric matches
- `Concepts`, which are later augmented by the rule engine
- `SortHints`, which are later augmented by the rule engine

A useful way to think about this contract is that it holds meaning the planner does not want to rediscover from text repeatedly. A recognized year such as `2024` can later seed canonical `majorVersion` intent. A rule-derived concept signal can later explain why the planner added a specific keyword expansion. A rule-derived sort hint can later explain why the executor sorted by version fields.

This contract is therefore partly typed-extraction output and partly rule-engine output. That is intentional. It gives contributors one place to inspect the signals the planner retained while shaping the final plan.

## 3. `CanonicalQueryModel`: the repository-owned subject matter of the query

`CanonicalQueryModel` is the query-side model that mirrors the discovery-facing half of the canonical index.

The current model includes these exact-match field sets:

- `Keywords`
- `Authority`
- `Region`
- `Format`
- `MajorVersion`
- `MinorVersion`
- `Category`
- `Series`
- `Instance`
- `Title`

It also includes these analyzed text fields:

- `SearchText`
- `Content`

The most important thing to understand is that this model is about **what** the query is about in repository-owned search terms. It is not yet about how the search engine should score or sort the query.

For example:

- if typed extraction recognizes `2024` and the planner projects it into `MajorVersion`, that is canonical intent
- if a rule expands `solas` into repository-owned keywords, that is canonical intent
- if a rule decides that the query should sort descending by version, that is **not** canonical intent; it belongs somewhere else

## 4. `QueryExecutionDirectives`: how the query should run, not what it is about

`QueryExecutionDirectives` carries execution-time behavior that accompanies the canonical model without becoming part of it.

The current contract contains:

- `Filters`
- `Boosts`
- `Sorts`

This separation matters because the repository wants to keep subject matter and execution policy distinct.

A filter such as `category = notice` changes which results are allowed to survive, but it does not mean the canonical subject matter of the query is only the filter itself. A boost changes scoring emphasis, but it does not mean the boosted text has become the whole query model. A sort changes ordering, but it does not describe what the user is searching for.

That distinction makes the query plan more teachable. A contributor can inspect canonical intent first and execution policy second instead of trying to infer both from one large Elasticsearch request body.

## 5. `QueryDefaultContributions`: the fallback layer built from surviving residual text

`QueryDefaultContributions` is the repository-owned contract that captures the default matching layer derived from the final residual surface.

Each `QueryDefaultFieldContribution` carries:

- `FieldName`
- `MatchingMode`
- `Text` or `Terms`
- `Boost`

In the current implementation, `QueryPlanService.CreateDefaultContributions()` produces at most three default contributions:

- a `keywords` exact-terms contribution with boost `1.0`
- a `searchText` analyzed-text contribution with boost `2.0`
- a `content` analyzed-text contribution with boost `1.0`

This contract exists because the planner wants a deterministic, explainable fallback rather than an ad hoc "search whatever text is left" behavior.

## Exact-match fields versus analyzed fields

This is one of the most important distinctions on the query side.

### Exact-match fields

The current exact-match canonical fields are the ones the mapper translates into `terms` clauses. These include fields such as:

- `keywords`
- `authority`
- `region`
- `format`
- `category`
- `series`
- `instance`
- `title`
- `majorVersion`
- `minorVersion`

Use repository language first here. These are the fields where the planner expects discrete values, taxonomy values, or numeric version values rather than free-form prose matching.

### Analyzed fields

The current analyzed fields are:

- `searchText`
- `content`

These are the fields where the planner expects free-text matching. The mapper translates those into Elasticsearch `match` clauses rather than `terms` clauses.

### Why the runtime uses them differently

The reason for this split is not an Elasticsearch preference alone. It is a repository meaning distinction.

A canonical keyword or category value should be matched as a discrete repository-owned value. A free-text narrative surface such as `content` should be matched as analyzed text. If those two categories were handled identically, the runtime would lose the difference between exact search intent and broad textual recall.

That is why residual defaults split the way they do:

- residual tokens are sent to `keywords`
- residual cleaned text is sent to `searchText` and `content`

The same principle explains why rules can emit exact-term boosts on canonical fields and analyzed boosts on text fields, but the validator requires the matching mode to line up with the field type.

## How the mapper builds one deterministic request body

`ElasticsearchQueryMapper` is deliberately small but very strict about order.

At a high level, it asks two questions.

### First question: is the plan executable at all?

The mapper and the executor both treat a plan as executable only if it contains at least one of the following:

- canonical model clauses
- execution filters
- execution boosts
- residual default contributions

If none of those exist, the mapper emits `match_none` instead of allowing a broad accidental search.

### Second question: which clauses belong in which part of the bool query?

For executable plans, the mapper builds a bool query with these parts:

- `filter` for explicit non-scoring constraints
- `should` for canonical model clauses
- `should` for explicit boosts
- `should` for residual defaults
- `minimum_should_match: 1`
- `sort` for explicit execution sorts when present

That layout is more important than it may first appear.

The current Query UI depends on that determinism. Because the host now shows the final Elasticsearch request JSON beside the generated plan, contributors can compare the repository-owned meaning in `QueryPlan` with the exact mapped request body without guessing whether the UI rebuilt anything itself. The host only reformats the returned JSON for readability; it does not invent or reorder the request.

## How execution diagnostics return to the host

`QuerySearchResult` is now more than a hit container. It is the repository-owned execution result contract that carries the artifacts the developer-facing host needs in order to explain what happened.

In current behavior, the result can retain:

- the executed `QueryPlan`
- the final Elasticsearch request JSON produced by `ElasticsearchQueryMapper`
- the total hit count and projected hits
- the wall-clock duration measured around execution
- the search-engine-reported duration when Elasticsearch returns the `took` field
- non-blocking warnings, such as the explanation that execution was skipped because the plan contained no executable clauses

This matters because request visibility is now part of the contributor workflow. A developer can run a raw query, inspect the generated plan in Monaco, inspect the mapped request JSON in the diagnostics column, then edit the plan and run it again to see how the request changes. The infrastructure layer remains the source of truth for both mapping and execution diagnostics, while the host remains only a reader and presenter of those inward artifacts.

## Repository-owned meaning of Elasticsearch bool terms

This page should explain the Elasticsearch terms in repository language rather than assuming the raw search-engine vocabulary is enough.

### `filter`

In repository terms, `filter` is where execution directives narrow the allowed result set without changing score.

A category filter emitted by a query rule belongs here because the rule is expressing a hard execution constraint, not a scoring preference.

### `should`

In repository terms, `should` is the shared scoring surface where canonical intent, explicit boosts, and residual defaults can all contribute.

The important design choice is that the mapper keeps those three sources distinct in origin while still expressing them in one score-bearing clause array.

### `minimum_should_match`

In repository terms, `minimum_should_match: 1` means the runtime is not satisfied unless at least one scoring clause actually matches.

That is how the mapper avoids turning the query into an unconstrained broad search when filters alone are present.

### `sort`

In repository terms, `sort` is where explicit execution order from the plan becomes search-engine ordering behavior.

The current query runtime only emits sorts when rules or future planning stages have asked for them. Sorting is therefore part of execution policy, not an automatic fallback.

## Example 1: model-only request shape

A model-only plan is the cleanest way to see canonical intent on its own.

Imagine a plan shaped by a rule set for `latest SOLAS` after consumption has removed the residual text. The canonical model contains the expanded keywords, and execution directives contain the version sorts.

The request body shape looks like this:

```json
{
  "size": 25,
  "query": {
    "bool": {
      "should": [
        {
          "terms": {
            "keywords": ["solas", "maritime", "safety", "msi"],
            "_name": "keywords"
          }
        }
      ],
      "minimum_should_match": 1
    }
  },
  "sort": [
    { "majorVersion": { "order": "desc" } },
    { "minorVersion": { "order": "desc" } }
  ]
}
```

The important thing to notice is what is **not** present. There are no residual defaults because the rule engine already consumed the meaningful tokens. The request is almost entirely driven by repository-owned canonical intent and explicit sort policy.

## Example 2: filter-and-boost-driven request shape

Now imagine a rules-shaped notice query where execution directives matter more than canonical keyword expansion.

A representative plan can carry:

- a filter `category = notice`
- a filter `majorVersion = 2024`
- an exact-term boost on `keywords = notice`
- an analyzed boost on `searchText = notice`

The request body shape looks like this:

```json
{
  "size": 25,
  "query": {
    "bool": {
      "filter": [
        {
          "terms": {
            "category": ["notice"],
            "_name": "filter:category"
          }
        },
        {
          "terms": {
            "majorVersion": [2024],
            "_name": "filter:majorVersion"
          }
        }
      ],
      "should": [
        {
          "terms": {
            "keywords": ["notice"],
            "boost": 3.0,
            "_name": "boost:keywords"
          }
        },
        {
          "match": {
            "searchText": {
              "query": "notice",
              "boost": 5.0,
              "_name": "boost:searchText"
            }
          }
        }
      ],
      "minimum_should_match": 1
    }
  }
}
```

This is a good example of repository-owned policy expressed through Elasticsearch vocabulary.

- the `filter` array says which documents are allowed to survive
- the `should` array says which surviving documents should score better
- `minimum_should_match: 1` says at least one scoring clause still has to match

The plan therefore preserves a clean distinction between narrowing the result set and shaping score.

## Example 3: residual-default request shape

Finally, imagine a plan with no rule-driven canonical model and no explicit execution directives, only surviving residual text.

The current default contribution logic turns that residual surface into three contributions:

- exact residual tokens on `keywords`
- analyzed residual text on `searchText`
- analyzed residual text on `content`

The request body shape looks like this:

```json
{
  "size": 25,
  "query": {
    "bool": {
      "should": [
        {
          "terms": {
            "keywords": ["latest", "solas"],
            "boost": 1.0,
            "_name": "keywords"
          }
        },
        {
          "match": {
            "searchText": {
              "query": "latest solas",
              "boost": 2.0,
              "_name": "searchText"
            }
          }
        },
        {
          "match": {
            "content": {
              "query": "latest solas",
              "boost": 1.0,
              "_name": "content"
            }
          }
        }
      ],
      "minimum_should_match": 1
    }
  }
}
```

This request shape is the fallback story of the query runtime. No rule had already captured the meaning in a more structured way, so the residual text still has to do the work.

## How named clauses help later diagnostics

The mapper writes `_name` values onto the generated clauses.

That detail matters because Elasticsearch can later report which named clauses matched each hit. The executor retains those matched query names on `QuerySearchHit.MatchedFields`. In other words, the request body contains some of the later diagnostic breadcrumbs that the host UI can surface.

This is another reason the repository-owned plan and the deterministic mapper belong in the architecture story. Diagnostics are easier when contributors can connect clause names back to canonical fields, boosts, or filters they already understand in repository terms.

## Result parsing after execution

After the request has been executed, the infrastructure layer parses the Elasticsearch response back into repository-owned shapes.

`ElasticsearchQueryExecutor` deserializes the raw body into a focused internal envelope and then projects each hit into `QuerySearchHit`. The current projection keeps:

- the first available title or a fallback `(untitled)`
- the first available category, or format as a fallback type-like value
- the first available region
- the matched clause names returned by Elasticsearch
- an optional raw source copy for developer inspection

That means the query UI never has to reason about raw Elasticsearch client result types directly.

## Common mistakes when reasoning about mapping

- Treating filters as if they were score-bearing clauses. They are not.
- Assuming all canonical fields are analyzed text fields. Most of the canonical query model is exact-match intent.
- Forgetting that `searchText` and `content` are analyzed fields even though they live on the canonical query model.
- Assuming residual defaults run before rules. They run after rule evaluation finishes.
- Forgetting that an empty plan becomes `match_none` rather than a broad match-all search.
- Reading the raw request body before checking the repository-owned `QueryPlan` that produced it.

## Related pages

- [Query pipeline](Query-Pipeline)
- [Query walkthrough](Query-Walkthrough)
- [Query signal extraction rules](Query-Signal-Extraction-Rules)
- [Appendix: query rule syntax quick reference](Appendix-Query-Rule-Syntax-Quick-Reference)
- [Glossary](Glossary)
