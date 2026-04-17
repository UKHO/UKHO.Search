# Appendix: query rule syntax quick reference

Use this page as the short lookup companion to [Query signal extraction rules](Query-Signal-Extraction-Rules).

If you are trying to understand why the query rules engine exists, how consumption semantics change residual defaults, or how refresh and diagnostics work, return to the deep-dive page. This appendix is intentionally shorter and optimized for authoring-time lookup.

## File shapes used in this repository

### Repository authoring shape

Query rules are authored one per file under `rules/query/*.json`.

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
      "consume": {
        "tokens": ["solas"]
      }
    }
  }
}
```

### Effective runtime shape after configuration loading

The runtime loads flat entries from `rules:query:*`, unwraps the outer `rule` object, and uses the configuration key as the effective rule id.

```json
{
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
    "consume": {
      "tokens": ["solas"]
    }
  }
}
```

## Required fields

| Field | Required | Notes |
|---|---|---|
| `schemaVersion` | Yes in file form | Must currently be `"1.0"`. |
| `rule.id` | Yes in file form | Runtime identity is still overwritten from the flat configuration key. |
| `rule.title` | Yes | Blank titles fail validation. |
| `rule.if` | Yes | The current query DSL requires `if`; there is no `match` alias. |
| `rule.then` | Yes | Must contain at least one supported action group. |
| `rule.description` | No | Optional authored description. |
| `rule.enabled` | No | Defaults to `true`; disabled rules are filtered out of the validated snapshot. |

## Predicate forms

### Equality

```json
"if": {
  "path": "input.tokens[*]",
  "eq": "solas"
}
```

### Phrase match

```json
"if": {
  "path": "input.cleanedText",
  "containsPhrase": "latest"
}
```

### Any-group

```json
"if": {
  "any": [
    { "path": "input.cleanedText", "containsPhrase": "latest" },
    { "path": "input.cleanedText", "containsPhrase": "most recent" }
  ]
}
```

Supported predicate operators are only:

- `eq`
- `containsPhrase`
- `any`

## Supported predicate paths

| Path | Supported use |
|---|---|
| `input.rawText` | `eq`, `containsPhrase` |
| `input.normalizedText` | `eq`, `containsPhrase` |
| `input.cleanedText` | `eq`, `containsPhrase` |
| `input.tokens[*]` | `eq` |
| `input.residualText` | `eq`, `containsPhrase` |
| `input.residualTokens[*]` | `eq` |
| `extracted.temporal.years[*]` | `eq` |

Not supported:

- nested arbitrary JSON paths
- `all`
- `not`
- ingestion-style operators such as `exists` or `in`

## Supported action groups

| Action group | Shape | Current support |
|---|---|---|
| `model` | object keyed by canonical field | `keywords`, `authority`, `region`, `format`, `majorVersion`, `minorVersion`, `category`, `series`, `instance`, `title` |
| `concepts` | array | Requires `id` and at least one `keywordExpansions` value |
| `sortHints` | array | Sort fields limited to `majorVersion` and `minorVersion`; `order` must be `asc` or `desc` |
| `consume` | object | Supports `tokens` and `phrases` |
| `filters` | object keyed by canonical field | Supports exact-match fields only; integer fields are `majorVersion` and `minorVersion` |
| `boosts` | array | Exact-match fields use `values`; analyzed fields (`searchText`, `content`) use `text` |

## Fast authoring examples

### Concept expansion for `solas`

```json
{
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
```

### Latest-sort intent

```json
{
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
```

### Notice filter and boost

```json
{
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
```

### Year-aware match against extracted signals

```json
{
  "id": "edition-2024",
  "title": "Recognize edition 2024",
  "if": {
    "path": "extracted.temporal.years[*]",
    "eq": "2024"
  },
  "then": {
    "model": {
      "majorVersion": {
        "add": [2024]
      }
    }
  }
}
```

## Related pages

- [Query signal extraction rules](Query-Signal-Extraction-Rules)
- [Query pipeline](Query-Pipeline)
- [Query walkthrough](Query-Walkthrough)
- [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping)
- [Glossary](Glossary)
