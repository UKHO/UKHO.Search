# Ingestion rules

This page is the deep-dive guide to the current ingestion rules engine.

Use it when you need to understand how rules are loaded, how predicates and templates behave, which canonical fields are actively materialized today, and where the live implementation boundaries sit.

## Reading path

- Start with [Ingestion pipeline](Ingestion-Pipeline) for the wider runtime context.
- Continue to [Ingestion walkthrough](Ingestion-Walkthrough) if you want to trace rules through the host and provider graph.
- Keep [Appendix: rule syntax quick reference](Appendix-Rule-Syntax-Quick-Reference) nearby while writing JSON.
- Use [Ingestion troubleshooting](Ingestion-Troubleshooting) when the runtime result does not match your expectation.

## What the rules engine does

The rules engine enriches a `CanonicalDocument` from values found on the active ingestion payload.

In the current runtime that means:

1. the provider name is resolved, typically `file-share`
2. the rules catalog loads only rules scoped to that provider
3. the engine evaluates predicates against the active `IndexItem` payload
4. every matching rule adds canonical values through monotonic mutations

Delete and ACL-update requests do not receive canonical rule mutations because the live engine only evaluates `request.IndexItem` payloads.

## Current authoring and loading model

There are two useful views of the same ruleset.

### Repository authoring view

Developers currently author one rule per file under the repository root:

- `rules/file-share/...`

Each file usually uses this wrapper shape:

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

### Effective runtime view

During local services-mode development, `AppHost` loads the repository `rules/` directory into the configuration emulator under the `rules` prefix.

`IngestionRulesSource` then:

- enumerates keys such as `rules:file-share:mime-app-s63`
- unwraps file-style `{ "rule": { ... } }` JSON when present
- assigns the runtime rule id from the configuration key
- canonicalizes the provider name through `IProviderCatalog`

That means the current local runtime is configuration-backed, but it still keeps the same per-rule JSON authoring shape that developers edit in the repository.

### File-based contract still matters

The repository also retains a file-based loader contract around `Rules/<provider>/...`.

That contract remains important for isolated tests and bootstrap scenarios, and it is still the clearest mental model for understanding one-rule-per-file authoring. The important part is that both paths converge on the same inner rule JSON shape.

## Provider scoping and identity

Rules are provider-scoped.

Current behavior:

- only the rules for the current provider are evaluated
- provider lookup is case-insensitive at load time
- provider names are canonicalized through `UKHO.Search.ProviderModel`
- unknown providers fail rule loading with a diagnosable validation error

This is one of the reasons provider metadata matters outside the host bootstrap path: rules are not allowed to invent providers that runtime composition does not know about.

## Required rule fields

The live rule contract requires:

- `id`
- `title`
- exactly one predicate block: `if` or `match`
- `then`

### `title` is part of the runtime contract

`title` is not optional decorative metadata.

It is:

- required at validation time
- templated through the same variable-expansion machinery as other rule outputs
- written into `CanonicalDocument.Title`
- preserved with display casing rather than normalized to lowercase

Separately, after enrichment completes, the pipeline validates that the final canonical document still contains at least one title. If not, the operation goes to dead letter rather than being indexed.

### `context` is a tooling and classification aid

`context` is not a predicate. It is metadata used for rule grouping and tools such as `RulesWorkbench` candidate detection.

Current `file-share` behavior is transitional but explicit:

- if no `file-share` rules declare `context`, the legacy all-missing state still validates
- once any `file-share` rule declares `context`, all `file-share` rules must declare it

When present, `context` is normalized to trimmed lowercase.

## Predicate model

The engine supports two predicate styles.

### 1. Shorthand equality form

```json
"if": {
  "id": "doc-1",
  "properties[\"product\"]": "AVCS"
}
```

Semantics:

- each property name is treated as a path
- each value is treated as an `eq` comparator
- all entries must match
- `$val` becomes the concatenated matched values in JSON property order

Use this form for small, readable equality-only checks.

### 2. Explicit boolean form

```json
"if": {
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
    { "path": "properties[\"product code\"]", "exists": false },
    { "not": { "path": "id", "eq": "blocked" } }
  ]
}
```

Supported boolean nodes are:

- `all`
- `any`
- `not`

Current validation requires:

- exactly one boolean key per boolean node
- non-empty arrays for `all` and `any`
- an object, not an array, for `not`

## Path model

Paths resolve values from the active payload using case-insensitive property lookup.

Important current rules:

- collection traversal must use `[*]`
- numeric indexes such as `[0]` are not supported
- selector/filter syntax is not supported
- ingestion property lookup supports both `properties.name` and `properties["name"]`

Recommended common forms:

- `id`
- `files[*].mimeType`
- `files[*].filename`
- `properties["businessunitname"]`
- `properties["product name"]`

If a path does not resolve at runtime because optional data is missing, the engine treats that as ordinary non-match behavior rather than as an exception.

## Operator semantics

The live implementation supports these leaf operators:

- `exists`
- `eq`
- `contains`
- `startsWith`
- `endsWith`
- `in`

All string comparison operators normalize by trimming and lowercasing both sides.

### `exists`

`exists: true` matches when any resolved value is non-empty after trimming.

`exists: false` matches when no resolved value remains after trimming. That includes:

- a missing path
- null values
- empty strings
- whitespace-only strings

This is one of the most useful current behaviors for rule fallbacks and classification rules.

### Multi-value semantics

When a path resolves to multiple values such as `files[*].mimeType`, the comparison succeeds if any resolved value matches.

Matched values then feed `$val` according to the predicate form that produced them.

## Variables and templating

The template expander supports two variable forms:

- `$val`
- `$path:<path>`

### `$val`

`$val` comes from predicate evaluation:

- `exists` contributes resolved non-empty values for `exists: true`
- comparison operators contribute only the values that actually matched
- `all` concatenates matched child values in order
- shorthand concatenates values in JSON property order
- `any` takes values from the first matching branch
- `exists: false` contributes no values

### `$path:<path>`

`$path:` resolves a fresh value from the active payload instead of reusing the matched comparison value.

Use it when the thing you want to write into the canonical document is not identical to the thing that made the rule match.

### Unknown or empty variables

Unknown variables or variables that resolve to no values simply produce no output. They do not fail the whole rule run.

## `toInt(...)` parsing

The current template expander also supports `toInt(...)` for numeric taxonomy fields.

It:

- resolves variables first
- trims the input
- parses using invariant culture
- accepts base-10 integers with optional `+` or `-`
- skips invalid or overflowing values rather than failing ingestion

Typical usage:

```json
"majorVersion": {
  "add": ["toInt($path:properties[\"edition number\"])"]
}
```

## Which `then` actions actively mutate the live canonical document

The current `IngestionRulesActionApplier` actively materializes these canonical fields:

- `title`
- `keywords.add`
- `searchText.add`
- `content.add`
- `authority.add`
- `region.add`
- `format.add`
- `category.add`
- `series.add`
- `instance.add`
- `majorVersion.add`
- `minorVersion.add`

Current normalization behavior is important:

- `title` is trimmed but keeps display casing
- string values written into normalized canonical sets are trimmed and lowercased
- keyword additions are token-normalized
- duplicate values are skipped

## Modeled actions that are not currently applied to `CanonicalDocument`

The DTO shape for rules still includes:

- `facets.add`
- `documentType.set`

Those sections remain part of the accepted model and logging/reporting surface, but the current live action applier does not mutate `CanonicalDocument` from them.

That distinction matters for documentation accuracy:

- they are visible in the model
- they are not the right place to expect live canonical changes today

If a rule only edits those sections, the rule can still load and match without producing visible canonical output.

## Fail-fast validation vs runtime tolerance

The current engine is intentionally strict about some things and intentionally tolerant about others.

### Fail-fast at load time

The current validation path fails startup or reload for:

- invalid JSON
- missing or unsupported `schemaVersion`
- unknown providers
- duplicate rule ids
- missing `title`
- missing required `context` in a partially uplifted `file-share` ruleset
- invalid predicate shape
- unsupported operators
- invalid path syntax

### Tolerant at runtime

The engine does not fail ingestion just because one payload is missing optional data.

Typical tolerant cases are:

- a runtime path resolves to no values
- a comparison does not match
- a template variable resolves to nothing
- `toInt(...)` cannot parse a value

In those cases the rule simply does not add the derived output.

## Worked examples

### Simple MIME-type rule

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
      "keywords": { "add": ["exchange-set"] },
      "searchText": { "add": ["exchange set"] }
    }
  }
}
```

### Missing-property fallback rule

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "bu-adds-s100-2-data-product-product-identifier",
    "context": "adds-s100",
    "title": "ADDS-S100 data product $path:properties[\"product name\"]",
    "description": "ADDS-S100 data product using product identifier.",
    "if": {
      "all": [
        { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
        { "path": "properties[\"product type\"]", "exists": true },
        { "path": "properties[\"product code\"]", "exists": false },
        { "path": "properties[\"product name\"]", "exists": true }
      ]
    },
    "then": {
      "category": { "add": ["data product"] },
      "series": { "add": ["s-100", "s100", "s-101", "s101"] },
      "instance": { "add": ["$path:properties[\"product name\"]"] }
    }
  }
}
```

### Numeric taxonomy rule

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "edition-and-update",
    "title": "Edition $path:properties[\"edition number\"]",
    "if": {
      "all": [
        { "path": "properties[\"edition number\"]", "exists": true },
        { "path": "properties[\"update number\"]", "exists": true }
      ]
    },
    "then": {
      "majorVersion": { "add": ["toInt($path:properties[\"edition number\"])"] },
      "minorVersion": { "add": ["toInt($path:properties[\"update number\"])"] }
    }
  }
}
```

## Practical local workflow

1. Edit the relevant file under `rules/file-share/...`.
2. Restart the services-mode AppHost stack so the configuration emulator reloads the repository rules.
3. Open [Tools: `RulesWorkbench`](Tools-RulesWorkbench) to validate and inspect the effective rule behavior.
4. Use `FileShareEmulator` or another local batch source to push a real message through the pipeline when ZIP-dependent enrichment also matters.
5. Inspect the resulting indexed document or dead-letter payload.

## Practical commands

Start the full local stack:

```powershell
dotnet run --project src/Hosts/AppHost/AppHost.csproj
```

Run ingestion-rules tests:

```powershell
dotnet test test/UKHO.Search.Infrastructure.Ingestion.Tests/UKHO.Search.Infrastructure.Ingestion.Tests.csproj
```

Run RulesWorkbench tests:

```powershell
dotnet test test/RulesWorkbench.Tests/RulesWorkbench.Tests.csproj
```

## Authoring checklist

- unique rule id within the provider scope
- non-empty `title`
- correct provider placement under `rules/<provider>/...`
- consistent `context` usage for `file-share`
- paths use `[*]` correctly for collections
- rule outputs target canonical fields the live applier actually materializes
- numeric fields use `toInt(...)` when needed

## Related pages

- [Ingestion pipeline](Ingestion-Pipeline)
- [Ingestion walkthrough](Ingestion-Walkthrough)
- [Appendix: rule syntax quick reference](Appendix-Rule-Syntax-Quick-Reference)
- [Ingestion troubleshooting](Ingestion-Troubleshooting)
- [CanonicalDocument and discovery taxonomy](CanonicalDocument-and-Discovery-Taxonomy)
- [File Share provider](FileShare-Provider)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
