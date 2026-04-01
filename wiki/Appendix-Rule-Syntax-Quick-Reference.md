# Appendix: rule syntax quick reference

Use this page as the short lookup companion to [Ingestion rules](Ingestion-Rules).

## File shapes used in this repository

### Repository authoring shape

Local rule files are authored one per file under `rules/<provider>/...`.

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "mime-app-s63",
    "context": "adds-s57",
    "title": "Exchange set",
    "if": {
      "files[*].mimeType": "app/s63"
    },
    "then": {
      "keywords": { "add": ["exchange-set"] }
    }
  }
}
```

### Effective runtime shape after configuration loading

`IngestionRulesSource` unwraps the outer `rule` object and assigns the rule id from the configuration key.

```json
{
  "id": "mime-app-s63",
  "context": "adds-s57",
  "title": "Exchange set",
  "if": {
    "files[*].mimeType": "app/s63"
  },
  "then": {
    "keywords": { "add": ["exchange-set"] }
  }
}
```

## Required fields

| Field | Required | Notes |
|---|---|---|
| `schemaVersion` | Yes in file form | Must be `"1.0"`. |
| `rule.id` | Yes in file form | Must be unique within a provider scope. In App Configuration-backed loading, the runtime uses the configuration key as the final id. |
| `rule.title` | Yes | Missing or blank titles fail validation. |
| `rule.if` or `rule.match` | Exactly one | `if` is preferred. `match` remains an alias. |
| `rule.then` | Yes | The mutation block. |
| `rule.context` | Conditionally required | For `file-share`, once any rule declares `context`, all `file-share` rules must declare it. |

## Predicate forms

### Shorthand equality

```json
"if": {
  "id": "doc-1",
  "properties[\"product\"]": "AVCS"
}
```

- each entry behaves like `eq`
- all entries must match
- `$val` concatenates the matched values in JSON property order

### Boolean form

```json
"if": {
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
    { "path": "properties[\"product code\"]", "exists": false }
  ]
}
```

Supported boolean keys:

- `all`
- `any`
- `not`

## Path rules

| Pattern | Supported | Notes |
|---|---|---|
| `id` | Yes | Property names are case-insensitive. |
| `files[*].mimeType` | Yes | Explicit wildcard traversal is required. |
| `properties.abcdef` | Yes | For identifier-like property names. |
| `properties["abcdef"]` | Yes | Preferred when the property name contains spaces or punctuation. |
| `files.mimeType` | No | Collection traversal without `[*]` fails validation. |
| `files[0].mimeType` | No | Numeric indexes are not supported. |
| `files[name="x"].mimeType` | No | Selector/filter syntax is not supported. |

## Operators

| Operator | Comparator type | Match rule |
|---|---|---|
| `exists` | Boolean | `true` matches any non-empty resolved value; `false` matches when no non-empty value remains. |
| `eq` | String | Case-insensitive, trimmed equality. |
| `contains` | String | Case-insensitive substring match. |
| `startsWith` | String | Case-insensitive prefix match. |
| `endsWith` | String | Case-insensitive suffix match. |
| `in` | Array of strings | Matches when any resolved value equals any normalized member of the array. |

If a runtime path resolves to no values:

- `exists: false` can still match
- all other operators evaluate as non-match
- no exception is thrown

## Variables and parsing helpers

### `$val`

Comes from predicate evaluation.

- shorthand: matched values in property order
- `all`: concatenated matched values from each child in order
- `any`: matched values from the first matching branch
- `exists: false`: no matched values

### `$path:<path>`

Resolves values from the active payload using the same path rules as predicates.

Examples:

```json
"instance": { "add": ["$path:properties[\"product name\"]"] }
```

```json
"keywords": { "add": ["mime-$path:files[*].mimeType"] }
```

### `toInt(...)`

Converts templates or literals into integer values for numeric taxonomy fields.

```json
"majorVersion": {
  "add": ["toInt($path:properties[\"edition number\"])"]
}
```

- trims whitespace
- uses invariant culture
- ignores invalid or overflowing inputs instead of failing the whole rule run

## Currently materialized `then` actions

These actions are actively applied to `CanonicalDocument` by the current action applier.

| Action | Target canonical field |
|---|---|
| `keywords.add` | `Keywords` |
| `searchText.add` | `SearchText` |
| `content.add` | `Content` |
| `authority.add` | `Authority` |
| `region.add` | `Region` |
| `format.add` | `Format` |
| `category.add` | `Category` |
| `series.add` | `Series` |
| `instance.add` | `Instance` |
| `majorVersion.add` | `MajorVersion` |
| `minorVersion.add` | `MinorVersion` |
| `title` | `Title` |

Notes:

- string outputs are trimmed and lowercased before being added to normalized canonical fields
- `title` is trimmed but preserves display casing
- duplicate additions are ignored by the canonical mutators

## Modeled but not currently materialized actions

The rule JSON DTO shape still includes these sections:

- `facets.add`
- `documentType.set`

They remain part of the accepted model and reporting surface, but the current `IngestionRulesActionApplier` does not mutate `CanonicalDocument` from those sections. Treat them as reserved authoring surface rather than as active canonical outputs until the runtime applier grows to handle them.

## Fast examples

### Match when a property is missing

```json
{
  "id": "missing-product-code",
  "title": "Fallback product",
  "if": {
    "path": "properties[\"product code\"]",
    "exists": false
  },
  "then": {
    "keywords": { "add": ["missing-product-code"] }
  }
}
```

### Match against file MIME types

```json
{
  "id": "mime-app-s63",
  "title": "Exchange set",
  "if": {
    "files[*].mimeType": "app/s63"
  },
  "then": {
    "keywords": { "add": ["exchange-set"] },
    "searchText": { "add": ["exchange set"] }
  }
}
```

### Project matched data into taxonomy fields

```json
{
  "id": "s100-product",
  "title": "ADDS-S100 data product $path:properties[\"product name\"]",
  "if": {
    "all": [
      { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
      { "path": "properties[\"product name\"]", "exists": true }
    ]
  },
  "then": {
    "category": { "add": ["data product"] },
    "series": { "add": ["s-100", "s100"] },
    "instance": { "add": ["$path:properties[\"product name\"]"] }
  }
}
```

## Related pages

- [Ingestion rules](Ingestion-Rules)
- [Ingestion walkthrough](Ingestion-Walkthrough)
- [Ingestion troubleshooting](Ingestion-Troubleshooting)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
