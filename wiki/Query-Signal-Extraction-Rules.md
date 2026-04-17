# Query signal extraction rules

This page is the deep-dive guide to the current query rules engine.

Use it when you need to understand how `rules/query/*.json` are authored, how those files become the flat `rules:query:*` runtime snapshot, which predicates and action groups are actually supported today, and why query-rule consumption semantics matter for the final Elasticsearch request.

## Reading path

- Start with [Query pipeline](Query-Pipeline) for the wider query-runtime mental model.
- Continue to [Query walkthrough](Query-Walkthrough) when you want to trace these rules through `QueryServiceHost`, planning, and Elasticsearch execution.
- Continue to [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping) when you want the deeper explanation of canonical intent versus execution directives.
- Keep [Appendix: query rule syntax quick reference](Appendix-Query-Rule-Syntax-Quick-Reference) nearby while authoring or reviewing JSON.

## What query rules do

Query rules are repository-owned interpretation rules for free-form search text.

They exist because the query side is trying to do more than pass raw user text straight into Elasticsearch. A search such as `latest SOLAS` contains different layers of meaning. The word `solas` can identify a known maritime concept. The word `latest` expresses execution intent more than document subject matter. A year such as `2024` may be better treated as structured canonical intent than as just another residual token. Query rules are the mechanism the repository uses to capture that meaning in a deterministic, testable way.

In the current runtime, a matched query rule can contribute six kinds of outcome:

- **canonical query intent** through `model` mutations such as keyword or taxonomy additions
- **concept signals** through `concepts`, which record recognized domain ideas and their keyword expansions
- **sort intent** through `sortHints`, which also materialize concrete execution sorts
- **residual cleanup** through `consume`, which removes already-accounted-for tokens or phrases from default matching
- **explicit filters** through `filters`, which constrain result sets without adding score
- **explicit boosts** through `boosts`, which add extra scoring weight on supported fields

Those outcomes are then carried forward into the `QueryPlan`. The rule engine does not talk to Elasticsearch directly. It shapes the repository-owned plan that the Elasticsearch mapper later translates.

## What query rules do not do

This distinction is just as important as the positive description.

Query rules do **not**:

- parse provider-specific source payloads
- mutate indexed `CanonicalDocument` values
- replace typed extraction
- execute imperative code or call external services
- load nested rule hierarchies beneath `rules:query:*`
- support the wider ingestion-rule predicate surface such as `all`, `not`, `exists`, `contains`, `startsWith`, `endsWith`, or `in`
- automatically remove typed-extraction matches from the residual query text

That last point is especially important. The current query runtime does project recognized years into canonical `majorVersion` intent before rules run, but typed extraction does not itself consume the matched year text out of the residual token stream. In current behavior, only rule-authored `consume` directives remove tokens or phrases from the residual surface.

## Canonical intent, execution directives, and residual defaults

Three related ideas show up repeatedly in this chapter, and contributors need to keep them separate.

### Canonical query intent

Canonical query intent is the part of the query plan that mirrors the indexed canonical model. When a rule adds `keywords`, `category`, `series`, or `majorVersion`, it is saying something about what the user is looking for in repository-owned search terms.

### Execution directives

Execution directives are the part of the query plan that change how Elasticsearch should execute the search without themselves becoming part of the canonical model. Sorts, explicit filters, and explicit boosts belong here. They shape execution policy rather than describing the subject matter of the query.

### Residual defaults

Residual defaults are the fallback matching contributions built after rule evaluation from whatever residual text remains. In current behavior, the planner turns residual tokens into a `keywords` terms clause and residual cleaned text into analyzed matches against `searchText` and `content`.

The reason this separation matters is duplication. If a rule already handled a phrase such as `latest`, the planner should not also let the residual default path search for the literal word `latest` again. Query rules exist in part to make that separation explicit.

## Current authoring and loading model

There are two useful views of the same ruleset.

### Repository authoring view

Developers author one rule per file under the repository query rules tree:

- `rules/query/<rule-id>.json`

The current file shape is a wrapped document rather than a bare rule object:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "concept-solas",
    "title": "Recognize SOLAS concept",
    "if": {
      "path": "input.tokens[*]",
      "eq": "solas"
    },
    "then": {
      "model": {
        "keywords": {
          "add": ["solas", "maritime", "safety", "msi"]
        }
      },
      "concepts": [
        {
          "id": "solas",
          "matchedText": "$val",
          "keywordExpansions": ["solas", "maritime", "safety", "msi"]
        }
      ],
      "consume": {
        "tokens": ["solas"]
      }
    }
  }
}
```

The wrapper matters because validation starts at the document level. `schemaVersion` belongs to the outer document, while the authored rule itself lives beneath `rule`.

### Effective runtime view under `rules:query:*`

At runtime, the query rules source enumerates configuration keys beneath `rules:query`.

That means the logical App Configuration keys look like this:

- `rules:query:concept-solas`
- `rules:query:sort-latest`
- `rules:query:filter-notice-latest`

`AppConfigQueryRulesSource` treats this namespace as deliberately flat. If a nested configuration section appears beneath `rules:query:*`, it is ignored rather than interpreted as another rule family. That is a current design rule, not an accident.

`QueryRulesLoader` then:

- enumerates the flat entries from `rules:query:*`
- deserializes the wrapped JSON document
- rejects empty or invalid JSON entries
- overwrites the inner `rule.id` with the rule id derived from the configuration key
- passes the resulting documents into `QueryRulesValidator`

That overwrite behavior is worth remembering. In practice, the effective runtime identity of a rule is the configuration key or flat file name, not whatever mismatched `id` text may have been left inside the inner payload.

### Runtime snapshot and stable ordering

`QueryRulesValidator` sorts enabled rules by id before they are retained on the validated snapshot. `ConfigurationQueryRuleEngine` then evaluates that stable snapshot once per planning request.

This gives the runtime two useful guarantees:

- one request sees one stable rule snapshot even if refresh happens concurrently
- rule order is deterministic for diagnostics, duplication suppression, and test assertions

## Required fields and authored metadata

The current query-rule contract is smaller than the ingestion-rule contract.

### Required document-level fields

The wrapped repository file form requires:

- `schemaVersion`
- `rule`

`schemaVersion` must currently be `"1.0"`.

### Required rule-level fields

Inside `rule`, the live validator requires:

- `id`
- `title`
- `if`
- `then`

### Optional fields

The current authored rule model also supports:

- `description`
- `enabled`

`enabled` defaults to `true`. Disabled rules are filtered out of the validated snapshot rather than being retained and skipped later.

### A deliberate difference from ingestion rules

Unlike the ingestion-rule DSL, the current query-rule DSL does **not** support a `match` alias. The validator expects `if`. That is an important current-state detail because contributors who copy ingestion examples mechanically may otherwise assume the alias exists on the query side too.

## Predicate model

The current query DSL keeps predicate logic intentionally small.

### Supported predicate kinds

The validator currently supports exactly three predicate shapes:

- `eq`
- `containsPhrase`
- `any`

Every predicate node must define exactly one of those shapes.

### Equality predicate

Use `eq` when you want a resolved scalar value to equal an authored value after normalization.

```json
"if": {
  "path": "input.tokens[*]",
  "eq": "solas"
}
```

This is the main way to match exact cleaned tokens or exact extracted year values.

### Phrase predicate

Use `containsPhrase` when you want phrase-level matching against one of the supported text surfaces.

```json
"if": {
  "path": "input.cleanedText",
  "containsPhrase": "most recent"
}
```

The rule engine lowercases both sides and checks token-boundary-aware phrase occurrence inside the resolved text surface.

### Any-group predicate

Use `any` when more than one trigger phrase or value should produce the same rule outcome.

```json
"if": {
  "any": [
    { "path": "input.cleanedText", "containsPhrase": "latest" },
    { "path": "input.cleanedText", "containsPhrase": "most recent" }
  ]
}
```

The first matching child contributes the matched value used later by `$val`-style matched-text expansion.

### Supported predicate paths

The current validator only accepts a small path surface:

- `input.rawText`
- `input.normalizedText`
- `input.cleanedText`
- `input.tokens[*]`
- `input.residualText`
- `input.residualTokens[*]`
- `extracted.temporal.years[*]`

That path list is intentionally repository-owned. Query rules do not resolve arbitrary JSON paths over external documents. They only inspect the normalized input snapshot and extracted query signals.

### Text-path restriction for `containsPhrase`

`containsPhrase` is only valid on these text surfaces:

- `input.rawText`
- `input.normalizedText`
- `input.cleanedText`
- `input.residualText`

You cannot use `containsPhrase` on token collections or extracted year collections.

### Unsupported predicate features

The current query DSL does **not** support:

- `all`
- `not`
- `exists`
- arbitrary nested path navigation beyond the supported query-owned paths
- ingestion-style comparison operators

That narrower surface is deliberate. Query rules are meant to inspect the repository-owned query snapshot, not to reproduce a general-purpose rule language.

## Supported action groups

Every rule must emit at least one supported action group. The current validator recognizes six.

### 1. `model`

`model` mutations add values into the canonical query model.

```json
"then": {
  "model": {
    "keywords": {
      "add": ["solas", "maritime", "safety", "msi"]
    }
  }
}
```

Supported `model` fields are currently:

- `keywords`
- `authority`
- `region`
- `format`
- `majorVersion`
- `minorVersion`
- `category`
- `series`
- `instance`
- `title`

A few details matter here.

First, `searchText` and `content` are part of `CanonicalQueryModel`, but they are **not** currently supported as rule-authored `model` fields. The live validator rejects them. On the query side, those analyzed fields are currently reached through residual defaults or future runtime shaping, not by direct rule-authored model mutation.

Second, integer-backed model fields such as `majorVersion` and `minorVersion` are validated separately from string-backed fields. The validator accepts authored primitive values and the rule engine later parses and deduplicates numeric values before storing them on the final model.

### 2. `concepts`

`concepts` emit repository-owned concept signals into `QueryExtractedSignals`.

```json
"then": {
  "concepts": [
    {
      "id": "solas",
      "matchedText": "$val",
      "keywordExpansions": ["solas", "maritime", "safety", "msi"]
    }
  ]
}
```

A concept signal is not only a label. It records:

- the concept id
- the matched display text derived from the authored template
- the keyword expansion set associated with that recognized concept

Concepts therefore help the planner remember why a rule matched, not only what canonical values were added.

### 3. `sortHints`

`sortHints` emit both extracted sort-hint signals and concrete execution sorts.

```json
"then": {
  "sortHints": [
    {
      "id": "latest",
      "matchedText": "$val",
      "fields": ["majorVersion", "minorVersion"],
      "order": "desc"
    }
  ]
}
```

Current sort support is intentionally narrow. Supported sort fields are:

- `majorVersion`
- `minorVersion`

The query engine records the hint in extracted signals and also materializes ordered `QueryExecutionSortDirective` entries so the executor can apply them later.

### 4. `consume`

`consume` removes already-accounted-for text from the residual token stream.

```json
"then": {
  "consume": {
    "phrases": ["most recent"],
    "tokens": ["solas"]
  }
}
```

Current behavior is precise:

- phrases are removed first
- tokens are removed second
- phrase removal works on non-overlapping token windows
- comparison is case-insensitive after normalization

This ordering matters. Removing phrases first stops a multi-token intent marker such as `most recent` from being partially broken up before phrase matching gets a chance to run.

### 5. `filters`

`filters` emit non-scoring execution constraints.

```json
"then": {
  "filters": {
    "category": { "add": ["notice"] },
    "majorVersion": { "add": [2024] }
  }
}
```

Supported filter fields are currently:

- `keywords`
- `authority`
- `region`
- `format`
- `majorVersion`
- `minorVersion`
- `category`
- `series`
- `instance`
- `title`

These become `filter` clauses in the final Elasticsearch request, not scoring `should` clauses. That means they narrow the result set without increasing relevance score.

### 6. `boosts`

`boosts` emit explicit scoring clauses.

```json
"then": {
  "boosts": [
    {
      "field": "searchText",
      "text": "notice",
      "matchingMode": "analyzedText",
      "weight": 4.0
    },
    {
      "field": "keywords",
      "values": ["notice"],
      "weight": 2.0
    }
  ]
}
```

Supported boost fields are currently:

- exact-match fields: `keywords`, `authority`, `region`, `format`, `majorVersion`, `minorVersion`, `category`, `series`, `instance`, `title`
- analyzed fields: `searchText`, `content`

The matching-mode rules are strict:

- analyzed fields must use `analyzedText`
- exact-match fields must use `exactTerms`
- omitted `matchingMode` falls back to the field-appropriate default
- boost weights must be positive

## Why consumption semantics matter

The query planner always starts from a normalized input snapshot whose residual tokens initially mirror the full cleaned token stream. Rule consumption is the thing that stops earlier interpretation from being repeated later.

Without `consume`, a rule could do all of the following at once:

- add canonical keywords
- emit a concept signal
- emit explicit filters or boosts
- still leave the same trigger word available to default keyword and analyzed-text matching

Sometimes that duplication is useful, but often it is not. `latest` is the clearest example. The word usually expresses recency intent, not subject matter. If a `sort-latest` rule matched and the residual default path still insisted on searching for the literal word `latest`, the query could drift away from the user’s real intent.

The current implementation therefore treats consumption as a first-class authoring decision rather than an afterthought.

## Refresh-aware catalog behavior

The rules engine does not deserialize and validate rule JSON on every query.

Instead, `QueryRulesCatalog` caches the latest validated snapshot and exposes two things to the services layer:

- the current rule snapshot
- diagnostics about when that snapshot was loaded and how many validated rules it contains

`AppConfigQueryRulesRefreshService` then monitors configuration refresh in the background. Its current behavior is:

- if Azure App Configuration refreshers are available, poll them on the configured interval
- otherwise, fall back to configuration reload-token monitoring
- when a refresh succeeds, reload the query rules catalog
- when refresh fails, log the error and keep using the previous validated snapshot

The refresh cadence comes from `configuration:refreshIntervalSeconds` and defaults to 30 seconds when the setting is absent or invalid.

This matters operationally because it means the live query runtime is refresh-aware without becoming fragile. A bad refresh attempt should not throw away the last known-good validated ruleset.

## Matched-rule diagnostics

The rule engine records several developer-facing diagnostics on `QueryPlanDiagnostics`.

Current diagnostics include:

- `MatchedRuleIds`
- `AppliedFilters`
- `AppliedBoosts`
- `AppliedSorts`
- `RuleCatalogLoadedAtUtc`

These diagnostics help answer questions such as:

- which rules actually matched this query
- whether a filter or boost was produced at all
- which sort directives were materialized
- which snapshot timestamp was in effect when the plan was built

The engine also logs each matched rule with its matched value, which is useful when tracing why a phrase or token produced a certain plan shape.

## Worked examples

The examples below are deliberately narrative. They are meant to help contributors reason about runtime behavior, not just memorize field names.

### `latest SOLAS`

This is the clearest current example because it combines concept expansion, sort intent, and residual consumption.

A representative rule pair looks like this:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "concept-solas",
    "title": "Recognize SOLAS concept",
    "if": {
      "path": "input.tokens[*]",
      "eq": "solas"
    },
    "then": {
      "model": {
        "keywords": {
          "add": ["solas", "maritime", "safety", "msi"]
        }
      },
      "concepts": [
        {
          "id": "solas",
          "matchedText": "$val",
          "keywordExpansions": ["solas", "maritime", "safety", "msi"]
        }
      ],
      "consume": {
        "tokens": ["solas"]
      }
    }
  }
}
```

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "sort-latest",
    "title": "Recognize latest intent",
    "if": {
      "any": [
        { "path": "input.cleanedText", "containsPhrase": "latest" },
        { "path": "input.cleanedText", "containsPhrase": "most recent" }
      ]
    },
    "then": {
      "sortHints": [
        {
          "id": "latest",
          "matchedText": "$val",
          "fields": ["majorVersion", "minorVersion"],
          "order": "desc"
        }
      ],
      "consume": {
        "phrases": ["latest", "most recent"]
      }
    }
  }
}
```

When the user searches for `latest SOLAS`, the current rule engine behavior is:

1. normalization produces `latest solas`
2. `concept-solas` matches `solas` on `input.tokens[*]`
3. `sort-latest` matches `latest` on `input.cleanedText`
4. the model gains the canonical keywords `solas`, `maritime`, `safety`, and `msi`
5. extracted signals gain a concept signal and a sort-hint signal
6. execution directives gain descending sorts on `majorVersion` and `minorVersion`
7. `solas` and `latest` are consumed from the residual path
8. the residual token stream becomes empty

That final empty residual state is the key outcome. The structured rule outputs already captured the useful meaning, so the default path has nothing left to search.

### `latest SOLAS msi`

This example is useful because it shows that rule consumption is selective rather than magical.

The same two representative rules above still match. `latest` is consumed by the sort rule and `solas` is consumed by the concept rule. But the extra token `msi` was entered explicitly by the user and is not removed by either rule.

The result is a mixed plan:

- canonical keyword intent still includes the rule-added SOLAS expansions
- sort intent still applies
- the residual path still contains `msi`

That means the final request body can contain both rule-shaped clauses and residual default clauses at the same time. This is often exactly what you want. The repository recognizes the broader SOLAS concept, but it still honors the caller’s extra explicit text rather than pretending the concept expansion already said everything.

### `latest notice`

A representative current-state notice rule can use explicit execution directives rather than only canonical keyword mutations.

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "notice-shaping",
    "title": "Shape notice searches",
    "if": {
      "path": "input.cleanedText",
      "containsPhrase": "notice"
    },
    "then": {
      "filters": {
        "category": { "add": ["notice"] }
      },
      "boosts": [
        {
          "field": "searchText",
          "text": "notice",
          "matchingMode": "analyzedText",
          "weight": 4.0
        },
        {
          "field": "keywords",
          "values": ["notice"],
          "weight": 2.0
        }
      ],
      "consume": {
        "tokens": ["notice"]
      }
    }
  }
}
```

Combined with `sort-latest`, this does something different from the SOLAS concept example.

Here the rule is not mainly expanding a domain concept. Instead, it is shaping execution:

- the category is explicitly filtered to `notice`
- `notice` gets additional scoring weight on both analyzed and exact-match surfaces
- the literal token `notice` is consumed so default matching does not redundantly search for it again
- `latest` still contributes descending sort intent through the separate latest rule

This is a good example of why query rules are not just synonym tables. They can decide that a word should narrow the result set, bias scoring, and reduce residual noise all at once.

### `latest notice from 2024`

This example is the best place to see the relationship between typed extraction, rules, and residual defaults.

A likely current runtime story is:

1. normalization produces the cleaned text `latest notice from 2024`
2. typed extraction recognizes the year `2024`
3. the planner seeds canonical `majorVersion` intent with `2024` before rules run
4. the latest rule contributes descending sort intent and consumes `latest`
5. the notice-shaping rule contributes the notice filter and boosts and consumes `notice`

At that point, two things are true at once.

First, the plan already carries a typed year as canonical intent through `majorVersion`. That is the structured part of the result.

Second, the residual surface is not automatically empty. In the current implementation, typed extraction does not itself remove the recognized year text, and neither of the example rules consumes the connecting word `from`. So unless another rule removes those tokens, residual defaults can still see `from` and `2024`.

That is not a documentation bug. It is a real current-state behavior that contributors should understand when authoring or debugging rules. If a year-bearing query seems to receive both canonical year intent and residual default matching for the same year text, the first thing to check is whether a rule authored any matching `consume` behavior for that case.

## Common mistakes when authoring query rules

- Assuming the query DSL supports the same predicate surface as ingestion rules.
- Assuming nested sections beneath `rules:query:*` will be loaded. They are ignored.
- Forgetting that the runtime rule id comes from the flat configuration key or file name.
- Treating filters as if they were boosts. Filters constrain results but do not add score.
- Forgetting to consume intent words such as `latest` after a rule has already translated them into sort behavior.
- Assuming typed extraction automatically removes matched years from the residual text. It does not in the current implementation.
- Trying to author `searchText` or `content` inside `model`, even though those fields are not currently supported as rule-authored model mutations.

## Related pages

- [Query pipeline](Query-Pipeline)
- [Query walkthrough](Query-Walkthrough)
- [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping)
- [Appendix: query rule syntax quick reference](Appendix-Query-Rule-Syntax-Quick-Reference)
- [Glossary](Glossary)
